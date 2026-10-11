using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using CLDV6212POE_ST10488555_ST10476800.Models;

namespace CLDV6212POE_ST10488555_ST10476800.Services
{
    public class FileShareService
    {
        private ShareClient _shareClient;

        public FileShareService(string connectionString)
        {
            _shareClient = new ShareClient(connectionString, "staff-docs");
            _shareClient.CreateIfNotExists();
        }

        public void UploadFile(string fileName, Stream fileStream)
        {
            ShareDirectoryClient rootDirectory = _shareClient.GetRootDirectoryClient();
            ShareFileClient fileClient = rootDirectory.GetFileClient(fileName);

            fileClient.Create(fileStream.Length);

            if (fileStream.Length > 0)
            {
                fileClient.UploadRange(new Azure.HttpRange(0, fileStream.Length), fileStream);
            }
        }

        public List<StaffDocumentInfo> ListFiles()
        {
            List<StaffDocumentInfo> results = new List<StaffDocumentInfo>();
            ShareDirectoryClient rootDirectory = _shareClient.GetRootDirectoryClient();

            var items = rootDirectory.GetFilesAndDirectories();
            foreach (var item in items)
            {
                if (!item.IsDirectory)
                {
                    ShareFileClient fileClient = rootDirectory.GetFileClient(item.Name);
                    ShareFileProperties properties = fileClient.GetProperties();

                    StaffDocumentInfo info = new StaffDocumentInfo();
                    info.FileName = item.Name;
                    info.Size = properties.ContentLength;
                    info.LastModified = properties.LastModified;
                    results.Add(info);
                }
            }
            return results;
        }

        public Stream DownloadFile(string fileName)
        {
            ShareDirectoryClient rootDirectory = _shareClient.GetRootDirectoryClient();
            ShareFileClient fileClient = rootDirectory.GetFileClient(fileName);

            bool exists = fileClient.Exists();
            if (!exists)
            {
                return null;
            }

            ShareFileDownloadInfo download = fileClient.Download();
            return download.Content;
        }
    }
}