using System;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using IoTEdgeRaspberryPiCamera.Models.DeviceProperties;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace IoTEdgeRaspberryPiCamera.Services.Cloud
{
    public abstract class AzureIoTHubServiceBase : ICloudService
    {
        protected readonly ILogger Logger;
        private readonly BehaviorSubject<DesiredDeviceProperties> _desiredDevicePropertiesSubject = new BehaviorSubject<DesiredDeviceProperties>(null);
        private readonly Subject<ImageConfig> _takePictureSubject = new Subject<ImageConfig>();

        protected AzureIoTHubServiceBase(ILogger logger)
        {
            Logger = logger;
        }
        
        public abstract Task Connect();
        public abstract Task Disconnect();
        
        protected abstract Task SendMessage(Message message);

        protected abstract Task UpdateTwin(TwinCollection newTwinData);

        public Task SendStatusMessage(Stream imageData)
        {
            Logger.LogDebug($"Sending status to IoT Hub:\nStream length:{imageData.Length}");
            var message = new Message(imageData)
            {
                ContentType = "image/jpeg", //image/bmp
                ContentEncoding = "utf-8",
            };
            // message.Properties.Add("content-type", "image/jpeg");
            return SendMessage(message);
        }
        
        public Task UpdateProperties(ReportedDeviceProperties updatedProperties)
        {
            var propertiesAsJson = JsonConvert.SerializeObject(updatedProperties, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
            Logger.LogDebug($"Sending updated properties to IoT Hub:\n{propertiesAsJson}");
            var newTwinData = new TwinCollection(propertiesAsJson);
            return UpdateTwin(newTwinData);
        }

        protected Task<MethodResponse> TakePicture(MethodRequest methodRequest, object userContext)
        {
            Logger.LogDebug($"{nameof(TakePicture)} was called");

            var imgConfig = JsonConvert.DeserializeObject<ImageConfig>(methodRequest.DataAsJson);
            _takePictureSubject.OnNext(imgConfig);
            
            //TODO: Needs proper result!
            return Task.FromResult(new MethodResponse(new byte[0], 200));
        }

        protected Task OnDesiredPropertyChanged(TwinCollection desiredProperties, object userContext)
        {
            Logger.LogDebug($"Desired property changed:\n{desiredProperties.ToJson()}");

            var desiredDeviceProperties =
                JsonConvert.DeserializeObject<DesiredDeviceProperties>(desiredProperties.ToJson());
            _desiredDevicePropertiesSubject.OnNext(desiredDeviceProperties);

            return Task.CompletedTask;
        }

        public IObservable<ImageConfig> TakePicture()
        {
            return _takePictureSubject.Publish().RefCount();
        }

        public IObservable<string> CloudMessage()
        {
            return Observable.Empty<string>();
        }

        public IObservable<DesiredDeviceProperties> GetDesiredProperties()
        {
            return _desiredDevicePropertiesSubject.Publish().RefCount();
        }
    }
}