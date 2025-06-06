using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading.Tasks;

namespace Healy.Services
{
    public class BlobService(IConfiguration config)
    {
        private readonly string _connectionString = config["AzureBlobStorage:ConnectionString"] ?? "";
        private readonly string _containerName = config["AzureBlobStorage:ContainerName"] ?? "";

        public async Task<Stream> GetBlobStreamAsync(string blobName)
        {
            var containerClient = new BlobContainerClient(_connectionString, _containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var stream = new MemoryStream();
            await blobClient.DownloadToAsync(stream);
            stream.Position = 0;
            return stream;
        }
    }
}
