using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;

namespace IoTEdgeRaspberryPiCamera.Services.Cloud
{
    public interface IBlobService
    {
        Task Upload(string containerName, string fileName, Stream dataStream);
    }
    
    public class BlobService : IBlobService
    {
        private readonly BlobServiceClient _blobServiceClient;

        public BlobService(string connectionString)
        {
            _blobServiceClient = new BlobServiceClient(connectionString);
        }

        public async Task Upload(string containerName, string fileName, Stream dataStream)
        {

            var containerClient = await GetBlobContainerClient(containerName);
            

            var blobClient = containerClient.GetBlobClient(fileName);
            await blobClient.UploadAsync(dataStream, true);
        }

        private async Task<BlobContainerClient> GetBlobContainerClient(string containerName)
        {
            try
            {
                return await _blobServiceClient.CreateBlobContainerAsync(containerName);
            }
            catch (Azure.RequestFailedException)
            {
                return _blobServiceClient.GetBlobContainerClient(containerName);
            }
        }
    }
}