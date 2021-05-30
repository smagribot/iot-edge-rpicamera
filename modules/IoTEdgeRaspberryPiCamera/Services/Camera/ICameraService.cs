using System;
using System.IO;
using System.Threading.Tasks;
using IoTEdgeRaspberryPiCamera.Models.DeviceProperties;

namespace IoTEdgeRaspberryPiCamera.Services.Camera
{
    public interface ICameraService : IDisposable
    {
        public string SensorName { get; }
        public Task<Stream> TakePicture(ImageConfig imageConfig);
    }
}