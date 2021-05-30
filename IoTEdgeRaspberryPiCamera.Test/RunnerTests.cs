using System;
using System.IO;
using System.Reactive.Subjects;
using IoTEdgeRaspberryPiCamera.Models.DeviceProperties;
using IoTEdgeRaspberryPiCamera.Services.Camera;
using IoTEdgeRaspberryPiCamera.Services.Cloud;
using IoTEdgeRaspberryPiCamera.Test.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IoTEdgeRaspberryPiCamera.Test
{
    public class RunnerTests
    {
        private readonly Runner _sut;

        private readonly TestSchedulerProvider _schedulerProvider = new TestSchedulerProvider();
        private readonly Mock<ILogger> _loggerMock = new Mock<ILogger>();
        private readonly Mock<ICloudService> _cloudServiceMock = new Mock<ICloudService>();
        private readonly Mock<ICameraService> _cameraServiceMock = new Mock<ICameraService>();
        private readonly Mock<IBlobService> _blobServiceMock = new Mock<IBlobService>();

        private readonly Subject<DesiredDeviceProperties> _devicePropertiesSubject =
            new Subject<DesiredDeviceProperties>();


        private readonly Stream _testStream = new MemoryStream();


        protected RunnerTests()
        {
            _cloudServiceMock.Setup(m => m.GetDesiredProperties())
                .Returns(_devicePropertiesSubject);


            _cameraServiceMock
                .Setup(m => m.TakePicture(It.IsAny<ImageConfig>()))
                .ReturnsAsync(_testStream);

            _sut = new Runner(
                _loggerMock.Object,
                _schedulerProvider,
                _cloudServiceMock.Object,
                _cameraServiceMock.Object,
                _blobServiceMock.Object
            );
        }

        public class Run : RunnerTests
        {
            [Fact]
            public void Should_connect_to_cloud()
            {
                _sut.Run();

                _schedulerProvider.AdvanceTo(1);
                _cloudServiceMock.Verify(m => m.Connect());
            }

            [Fact]
            public void Should_take_every_15_minutes_a_picture_at_0_by_default()
            {
                _sut.Run();

                _schedulerProvider.AdvanceTo(2);
                _cameraServiceMock.Verify(m =>
                        m.TakePicture(It.IsAny<ImageConfig>()),
                    Times.Exactly(1));

                _schedulerProvider.AdvanceBy(TimeSpan.FromMinutes(15).Ticks);
                _cameraServiceMock.Verify(m =>
                        m.TakePicture(It.IsAny<ImageConfig>()),
                    Times.Exactly(2));

                _schedulerProvider.AdvanceBy(TimeSpan.FromMinutes(15).Ticks);
                _cameraServiceMock.Verify(m =>
                        m.TakePicture(It.IsAny<ImageConfig>()),
                    Times.Exactly(3));
            }

            [Fact]
            public void Should_send_image_to_cloud()
            {
                var testStream = new MemoryStream();
                _cameraServiceMock
                    .Setup(m => m.TakePicture(It.IsAny<ImageConfig>()))
                    .ReturnsAsync(testStream);

                _sut.Run();

                _schedulerProvider.AdvanceTo(2);
            }

            [Fact]
            public void Should_upload_image_to_blob()
            {
                _sut.Run();

                _schedulerProvider.AdvanceTo(2);
                _blobServiceMock.Verify(m => m.Upload("timelapse", It.IsAny<string>(), _testStream), Times.Exactly(1));
            }

            [Fact]
            public void Should_not_send_status_to_cloud_when_status_cant_be_retrieved()
            {
                _cameraServiceMock.Setup(m => m.TakePicture(It.IsAny<ImageConfig>()))
                    .ThrowsAsync(new Exception());

                _sut.Run();

                _schedulerProvider.AdvanceTo(2);
                _cloudServiceMock.Verify(m => m.SendStatusMessage(It.IsAny<Stream>()), Times.Never);
            }

            [Fact]
            public void Should_not_upload_image_to_blob_when_status_cant_be_retrieved()
            {
                _cameraServiceMock.Setup(m => m.TakePicture(It.IsAny<ImageConfig>()))
                    .ThrowsAsync(new Exception());

                _sut.Run();

                _schedulerProvider.AdvanceTo(2);
                _blobServiceMock.Verify(m => m.Upload(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>()),
                    Times.Never);
            }

            [Fact]
            public void Should_listen_for_reported_properties()
            {
                _sut.Run();

                _schedulerProvider.AdvanceTo(2);
                _cloudServiceMock.Verify(m => m.GetDesiredProperties());
            }

            [Fact]
            public void Should_update_device_status_timer_when_new_period_is_set()
            {
                var devPropertiesWithNewPeriod = new DesiredDeviceProperties
                {
                    TimelapseConfig = new TimelapseConfig()
                    {
                        Period = TimeSpan.FromMinutes(5).TotalSeconds
                    }
                };

                _sut.Run();

                _schedulerProvider.AdvanceTo(2);
                _blobServiceMock.Verify(m => m.Upload("timelapse", It.IsAny<string>(), It.IsAny<Stream>()),
                    Times.Exactly(1));

                _schedulerProvider.AdvanceBy(TimeSpan.FromMinutes(15).Ticks);
                _cameraServiceMock.Verify(m =>
                        m.TakePicture(It.IsAny<ImageConfig>()),
                    Times.Exactly(2));

                //Update to new time
                _devicePropertiesSubject.OnNext(devPropertiesWithNewPeriod);

                _schedulerProvider.AdvanceBy(1);
                _cameraServiceMock.Verify(m =>
                        m.TakePicture(It.IsAny<ImageConfig>()),
                    Times.Exactly(3));

                _schedulerProvider.AdvanceBy(TimeSpan.FromMinutes(5).Ticks);
                _cameraServiceMock.Verify(m =>
                        m.TakePicture(It.IsAny<ImageConfig>()),
                    Times.Exactly(4));

                _schedulerProvider.AdvanceBy(TimeSpan.FromMinutes(5).Ticks);
                _cameraServiceMock.Verify(m =>
                        m.TakePicture(It.IsAny<ImageConfig>()),
                    Times.Exactly(5));

                _schedulerProvider.AdvanceBy(TimeSpan.FromMinutes(5).Ticks);
                _cameraServiceMock.Verify(m =>
                        m.TakePicture(It.IsAny<ImageConfig>()),
                    Times.Exactly(6));
            }

            [Fact]
            public void Should_not_update_device_status_timer_when_it_doesnt_changed()
            {
                var devPropertiesWithNewPeriod = new DesiredDeviceProperties
                {
                    TimelapseConfig = new TimelapseConfig
                    {
                        Period = TimeSpan.FromMinutes(15).TotalSeconds
                    }
                };

                _sut.Run();

                _schedulerProvider.AdvanceTo(2);
                _blobServiceMock.Verify(m => m.Upload("timelapse", It.IsAny<string>(), It.IsAny<Stream>()),
                    Times.Exactly(1));

                _schedulerProvider.AdvanceBy(TimeSpan.FromMinutes(15).Ticks);
                _cameraServiceMock.Verify(m =>
                        m.TakePicture(It.IsAny<ImageConfig>()),
                    Times.Exactly(2));

                //Update to new time
                _devicePropertiesSubject.OnNext(devPropertiesWithNewPeriod);

                _schedulerProvider.AdvanceBy(1);
                _cameraServiceMock.Verify(m =>
                        m.TakePicture(It.IsAny<ImageConfig>()),
                    Times.Exactly(2));

                _schedulerProvider.AdvanceBy(TimeSpan.FromMinutes(15).Ticks);
                _cameraServiceMock.Verify(m =>
                        m.TakePicture(It.IsAny<ImageConfig>()),
                    Times.Exactly(3));
            }

            [Fact]
            public void Should_only_listen_to_device_properties_with_TelemetryConfig()
            {
                var devPropertiesWithNewPeriod = new DesiredDeviceProperties
                {
                    TimelapseConfig = new TimelapseConfig
                    {
                        Period = TimeSpan.FromMinutes(5).TotalSeconds
                    }
                };

                _sut.Run();

                _schedulerProvider.AdvanceTo(2);
                _blobServiceMock.Verify(m => m.Upload("timelapse", It.IsAny<string>(), It.IsAny<Stream>()),
                    Times.Exactly(1));

                _schedulerProvider.AdvanceBy(TimeSpan.FromMinutes(15).Ticks);
                _cameraServiceMock.Verify(m =>
                        m.TakePicture(It.IsAny<ImageConfig>()),
                    Times.Exactly(2));

                //Update without TelemetryConfig
                _devicePropertiesSubject.OnNext(new DesiredDeviceProperties());

                _schedulerProvider.AdvanceBy(1);
                _cameraServiceMock.Verify(m =>
                        m.TakePicture(It.IsAny<ImageConfig>()),
                    Times.Exactly(2));

                _schedulerProvider.AdvanceBy(TimeSpan.FromMinutes(15).Ticks);
                _cameraServiceMock.Verify(m =>
                        m.TakePicture(It.IsAny<ImageConfig>()),
                    Times.Exactly(3));

                //Update with TelemetryConfig
                _devicePropertiesSubject.OnNext(devPropertiesWithNewPeriod);

                _schedulerProvider.AdvanceBy(1);
                _cameraServiceMock.Verify(m =>
                        m.TakePicture(It.IsAny<ImageConfig>()),
                    Times.Exactly(4));

                _schedulerProvider.AdvanceBy(TimeSpan.FromMinutes(5).Ticks);
                _cameraServiceMock.Verify(m =>
                        m.TakePicture(It.IsAny<ImageConfig>()),
                    Times.Exactly(5));
            }

            [Fact]
            public void Should_listen_for_ImageConfig_changes()
            {
                var devPropertiesWithNewImageConfig = new DesiredDeviceProperties
                {
                    ImageConfig = new ImageConfig
                    {
                        Width = 1280,
                        Height = 720,
                        Quality = 80,
                        EncodingFormat = EncodingFormat.JPEG,
                        PixelFormat = PixelFormat.I420,
                        ShutterSpeed = 2000000,
                        ISO = 400
                    }
                };

                _sut.Run();

                _schedulerProvider.AdvanceTo(2);
                //Called with default props
                _cameraServiceMock.Verify(m =>
                        m.TakePicture(It.Is<ImageConfig>(config =>

                            config.Width == 640 &&
                            config.Height == 480 &&
                            config.Quality == 100 &&
                            config.EncodingFormat == EncodingFormat.BMP &&
                            config.PixelFormat == PixelFormat.RGBA &&
                            config.ShutterSpeed == 0 &&
                            config.ISO == 0
                        )),
                    Times.Exactly(1));

                _devicePropertiesSubject.OnNext(devPropertiesWithNewImageConfig);
                _schedulerProvider.AdvanceBy(1);
                //Should only take pictures on timer, not ImageConfig changes
                _cameraServiceMock.Verify(m =>
                        m.TakePicture(It.IsAny<ImageConfig>()),
                    Times.Exactly(1));

                _schedulerProvider.AdvanceBy(TimeSpan.FromMinutes(15).Ticks);
                _cameraServiceMock.Verify(m =>
                        m.TakePicture(It.Is<ImageConfig>(config =>

                            config == devPropertiesWithNewImageConfig.ImageConfig
                        )),
                    Times.Exactly(1));
            }
        }
    }
}