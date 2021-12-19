using System;
using System.IO;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using IoTEdgeRaspberryPiCamera.Models.DeviceProperties;
using IoTEdgeRaspberryPiCamera.Services.Camera;
using IoTEdgeRaspberryPiCamera.Services.Cloud;
using IoTEdgeRaspberryPiCamera.Services.Scheduler;
using Microsoft.Extensions.Logging;

namespace IoTEdgeRaspberryPiCamera
{
    public class Runner : IDisposable
    {
        private readonly ILogger _logger;
        private readonly ISchedulerProvider _schedulerProvider;
        private readonly ICloudService _cloudService;
        private readonly ICameraService _cameraService;
        private readonly IBlobService _blobService;

        private TimeSpan _currentTimerInterval;

        private ImageConfig _currentImageConfig = new ImageConfig
        {
            Height = 480,
            Width = 640,
            EncodingFormat = EncodingFormat.BMP,
            PixelFormat = PixelFormat.RGBA,
            ShutterSpeed = 0, //Auto
            ISO = 0, // Auto
            Quality = 100
        };

        private readonly CompositeDisposable _compositeDisposable = new CompositeDisposable();

        public Runner(ILogger logger,
            ISchedulerProvider schedulerProvider,
            ICloudService cloudService,
            ICameraService cameraService,
            IBlobService blobService)
        {
            _logger = logger;
            _schedulerProvider = schedulerProvider;
            _cloudService = cloudService;
            _cameraService = cameraService;
            _blobService = blobService;
        }

        public void Run()
        {
            var connectedObservable = Observable.FromAsync(async () => await _cloudService.Connect());

            SubscribeToImageConfigChanges();
            SendStatusAndHearForPeriodUpdates(connectedObservable);
        }

        private void SubscribeToImageConfigChanges()
        {
            var imageConfigObservable = _cloudService.GetDesiredProperties()
                .Select(properties => properties?.ImageConfig)
                .Where(imgConfig => imgConfig != null)
                .Subscribe(imgConfig =>
                    {
                        _logger.LogInformation(
                            "Got new image config");
                        _currentImageConfig = imgConfig;
                    },
                    err => { _logger.LogError("Got error while listining for ImageConfig: {Err}", err); },
                    () => { _logger.LogInformation("Listing for ImageConfig finished"); });
            _compositeDisposable.Add(imageConfigObservable);
        }

        private void SendStatusAndHearForPeriodUpdates(IObservable<Unit> connectedObservable)
        {
            // Get TimelapseConfig.Period from reported properties to update StartIntervalledStatusUpdate with new TimeSpan
            var newIntervalObservable = _cloudService.GetDesiredProperties()
                .Where(properties => properties?.TimelapseConfig != null)
                .Select(properties => properties.TimelapseConfig.Period)
                .Where(period => period > 0)
                .Select(TimeSpan.FromSeconds)
                // When device and cloud are connected, start once with default interval, in case reported properties
                // doesn't contain TimelapseConfig.Period or they aren't reported yet
                .Merge(connectedObservable.Select(_ => TimeSpan.FromMinutes(15)))
                .Where(timespan => timespan != _currentTimerInterval)
                .Do(timespan => _currentTimerInterval = timespan)
                .Do(timespan =>
                    _logger.LogInformation(
                        "Setup new interval for timelapse interval. Interval is {timespan.TotalMinutes} minutes",
                        timespan.TotalMinutes))
                .Select(StartIntervalledStatusUpdate)
                .Switch()
                .SubscribeOn(_schedulerProvider.NewThread)
                .Subscribe(_ => { _logger.LogInformation("Uploaded picture"); },
                    err => { _logger.LogError("Uploaded picture interval got error: {Err}", err); },
                    () => { _logger.LogInformation("Uploaded pictures interval finished"); });

            _compositeDisposable.Add(newIntervalObservable);
        }

        private IObservable<Unit> StartIntervalledStatusUpdate(TimeSpan timespan)
        {
            return Observable.Interval(timespan, _schedulerProvider.NewThread)
                .Merge(Observable.Return(0L))
                .SelectMany(_ => TakePicture())
                .Do(data => data.Seek(0, SeekOrigin.Begin))
                .SelectMany(async data =>
                {
                    var fileName = CreatePictueName();
                    await using (data)
                    {
                        await _blobService.Upload("timelapse", fileName.ToString(), data);
                    }

                    return Unit.Default;
                });
        }

        private IObservable<Stream> TakePicture()
        {
            return Observable.FromAsync(() => _cameraService.TakePicture(_currentImageConfig))
                .RetryWhen(observable =>
                    observable
                        .Do(ex => _logger.LogWarning("TakePicture throw {Ex} trying again", ex))
                        .Zip(Observable.Return(0).Delay(TimeSpan.FromSeconds(30), _schedulerProvider.NewThread),
                            (exception, i) => i)
                        .Do(_ => _logger.LogInformation("TakePicture trying again"))
                );
        }

        private StringBuilder CreatePictueName()
        {
            var fileName = new StringBuilder();
            fileName.Append($"{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}");
            fileName.Append(_currentImageConfig.EncodingFormat == EncodingFormat.JPEG ? ".jpg" : ".bmp");
            return fileName;
        }

        public void Dispose()
        {
            _compositeDisposable?.Dispose();
        }
    }
}