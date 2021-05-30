using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace IoTEdgeRaspberryPiCamera.Models.DeviceProperties
{
    public class ImageConfig
    {
        //Default is 1280 x 720.
        public int Width { get; set; }
        public int Height { get; set; }

        // Default is 0 (auto)
        public int ShutterSpeed { get; set; }
        // Default is 0 (auto)
        public int ISO { get; set; }
            
        [JsonConverter(typeof(StringEnumConverter))]
        public EncodingFormat EncodingFormat { get; set; }
        
        [JsonConverter(typeof(StringEnumConverter))]
        public PixelFormat PixelFormat { get; set; }

        public int Quality { get; set; }
    }

    
    public enum PixelFormat
    {
        RGBA,
        I420
    }
    
    public enum EncodingFormat
    {
        BMP,
        JPEG
    }

    public class TimelapseConfig
    {
        public double Period { get; set; }
    }
    
    public class DesiredDeviceProperties
    {
        public TimelapseConfig TimelapseConfig { get; set; }
        public ImageConfig ImageConfig { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}