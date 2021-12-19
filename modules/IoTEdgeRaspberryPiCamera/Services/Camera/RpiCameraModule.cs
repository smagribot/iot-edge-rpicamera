using System.IO;
using System.Threading.Tasks;
using IoTEdgeRaspberryPiCamera.Extensions;
using IoTEdgeRaspberryPiCamera.Models.DeviceProperties;
using MMALSharp;
using MMALSharp.Common.Utility;
using MMALSharp.Handlers;

namespace IoTEdgeRaspberryPiCamera.Services.Camera
{
    public class RpiCameraModule : ICameraService
    {
        public async Task<Stream> TakePicture(ImageConfig imageConfig)
        {
            MMALCameraConfig.StillResolution = new Resolution(imageConfig.Width, imageConfig.Height); // Default is 1280 x 720.
            MMALCameraConfig.ShutterSpeed = imageConfig.ShutterSpeed; // Default is 0 (auto).
            MMALCameraConfig.ISO = imageConfig.ISO; //  Default is 0 (auto).

            var cam = MMALCamera.Instance;
            try
            {
                cam.ConfigureCameraSettings();

                var encodingType = imageConfig.EncodingFormat.CreateMMALEncoding();
                var pixelFormat = imageConfig.PixelFormat.CreateMMALEncoding();

                var captureHandler = new MemoryStreamCaptureHandler();
                await cam.TakePicture(captureHandler, encodingType, pixelFormat);
                return captureHandler.CurrentStream;
            }
            finally
            {
                cam.Cleanup();
            }
        }

        public string SensorName =>  MMALCamera.Instance?.Camera.CameraInfo.SensorName;
        
        public void Dispose()
        {
            MMALCamera.Instance.Cleanup();
        }
    }
}