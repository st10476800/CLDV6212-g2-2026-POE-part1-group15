using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CoffeeNChillGateway.Models;

namespace CoffeeNChillGateway.Services
{
    /// <summary>
    /// Saves, lists, downloads and deletes staff documents in the staff-docs blob container.
    /// </summary>
    public class BlobStorageService
    {
        private const string ContainerName = "staff-docs";

        private BlobContainerClient _containerClient;
        private bool _containerChecked = false;

        public BlobStorageService(string connectionString)
        {
            BlobServiceClient serviceClient = new BlobServiceClient(connectionString);
            _containerClient = serviceClient.GetBlobContainerClient(ContainerName);
        }

        private async Task EnsureContainerExistsAsync()
        {
            if (_containerChecked == false)
            {
                await _containerClient.CreateIfNotExistsAsync();
                _containerChecked = true;
            }
        }

        public async Task UploadFileAsync(string fileName, Stream fileStream)
        {
            await EnsureContainerExistsAsync();

            BlobClient blobClient = _containerClient.GetBlobClient(fileName);
            await blobClient.UploadAsync(fileStream, true);
        }

        public async Task<List<StaffDocumentInfo>> ListFilesAsync()
        {
            await EnsureContainerExistsAsync();

            List<StaffDocumentInfo> files = new List<StaffDocumentInfo>();
            AsyncPageable<BlobItem> blobs = _containerClient.GetBlobsAsync();

            await foreach (BlobItem blob in blobs)
            {
                StaffDocumentInfo info = new StaffDocumentInfo();
                info.FileName = blob.Name;
                info.Size = blob.Properties.ContentLength ?? 0;
                info.LastModified = blob.Properties.LastModified;
                files.Add(info);
            }
            return files;
        }

        /// <summary>
        /// Opens a file for download. Returns null if the file does not exist.
        /// </summary>
        public async Task<Stream> DownloadFileAsync(string fileName)
        {
            await EnsureContainerExistsAsync();

            BlobClient blobClient = _containerClient.GetBlobClient(fileName);

            Response<bool> exists = await blobClient.ExistsAsync();
            if (exists.Value == false)
            {
                return null;
            }

            Response<BlobDownloadInfo> download = await blobClient.DownloadAsync();
            return download.Value.Content;
        }

        /// <summary>
        /// Deletes a file. Returns false if the file was not there.
        /// </summary>
        public async Task<bool> DeleteFileAsync(string fileName)
        {
            await EnsureContainerExistsAsync();

            BlobClient blobClient = _containerClient.GetBlobClient(fileName);
            Response<bool> deleted = await blobClient.DeleteIfExistsAsync();
            return deleted.Value;
        }
    }
}
