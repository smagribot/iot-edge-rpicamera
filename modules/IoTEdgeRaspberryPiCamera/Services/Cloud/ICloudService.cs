using System;
using System.IO;
using System.Threading.Tasks;
using IoTEdgeRaspberryPiCamera.Models.DeviceProperties;

namespace IoTEdgeRaspberryPiCamera.Services.Cloud
{
    public interface ICloudService
    {
        Task Connect();
        Task Disconnect();

        Task SendStatusMessage(Stream imageData);
        Task UpdateProperties(ReportedDeviceProperties updatedProperties);

        IObservable<ImageConfig> TakePicture();

        IObservable<string> CloudMessage();
        IObservable<DesiredDeviceProperties> GetDesiredProperties();
    }
}