using IoTEdgeRaspberryPiCamera.Extensions;
using IoTEdgeRaspberryPiCamera.Models.DeviceProperties;
using MMALSharp.Common;
using Xunit;

namespace IoTEdgeRaspberryPiCamera.Test.Extensions
{
    public class MMALEncodingExtensionsTests
    {
        [Fact]
        public void Should_return_MMALEncoding_BMP_when_input_is_EncodingFormat_BMP()
        {
            Assert.Equal(MMALEncoding.BMP, EncodingFormat.BMP.CreateMMALEncoding());
        }
        
        [Fact]
        public void Should_return_MMALEncoding_JPEG_when_input_is_EncodingFormat_JPEG()
        {
            Assert.Equal(MMALEncoding.JPEG, EncodingFormat.JPEG.CreateMMALEncoding());
        }
    }
}