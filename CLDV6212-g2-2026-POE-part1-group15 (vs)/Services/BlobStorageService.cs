using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CLDV6212POE_ST10488555_ST10476800.Models;

namespace CLDV6212POE_ST10488555_ST10476800.Services
{
    public class BlobStorageService
    {
        private BlobContainerClient _containerClient;

        public BlobStorageService(string connectionString)
        {
            BlobServiceClient serviceClient = new BlobServiceClient(connectionString);
            _containerClient = serviceClient.GetBlobContainerClient("staff-docs");
            _containerClient.CreateIfNotExists();
        }

        public void UploadFile(string fileName, Stream fileStream)
        {
            BlobClient blobClient = _containerClient.GetBlobClient(fileName);
            blobClient.Upload(fileStream, overwrite: true);
        }

        public List<StaffDocumentInfo> ListFiles()
        {
            List<StaffDocumentInfo> results = new List<StaffDocumentInfo>();

            Pageable<BlobItem> blobs = _containerClient.GetBlobs();
            foreach (BlobItem blob in blobs)
            {
                StaffDocumentInfo info = new StaffDocumentInfo();
                info.FileName = blob.Name;
                info.Size = blob.Properties.ContentLength ?? 0;
                info.LastModified = blob.Properties.LastModified;
                results.Add(info);
            }

            return results;
        }

        public Stream DownloadFile(string fileName)
        {
            BlobClient blobClient = _containerClient.GetBlobClient(fileName);

            bool exists = blobClient.Exists();
            if (!exists)
            {
                return null;
            }

            BlobDownloadInfo download = blobClient.Download();
            return download.Content;
        }
    }
}