using System.Net;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using CLDV6212POE_ST10488555_ST10476800.Services;

namespace CLDV6212POE_ST10488555_ST10476800.Functions
{
    public class DocumentFunctions
    {
        private FileShareService _service;
        private ILogger _logger;

        public DocumentFunctions(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<DocumentFunctions>();
            string connectionString = Environment.GetEnvironmentVariable("AzureFilesStorage");
            _service = new FileShareService(connectionString);
        }

        [Function("UploadStaffDocument")]
        public async Task<HttpResponseData> UploadStaffDocument(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "documents/upload")] HttpRequestData req)
        {
            string contentType = null;
            IEnumerable<string> contentTypeValues;
            bool hasContentType = req.Headers.TryGetValues("Content-Type", out contentTypeValues);
            if (hasContentType)
            {
                foreach (string value in contentTypeValues)
                {
                    contentType = value;
                    break;
                }
            }

            if (contentType == null || !contentType.Contains("multipart/"))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Request must be multipart/form-data.");
                return badResponse;
            }

            string boundary = HeaderUtilities.RemoveQuotes(MediaTypeHeaderValue.Parse(contentType).Boundary.Value).Value;
            MultipartReader reader = new MultipartReader(boundary, req.Body);

            string fileName = null;
            MemoryStream fileMemoryStream = null;

            MultipartSection section = await reader.ReadNextSectionAsync();
            while (section != null)
            {
                ContentDispositionHeaderValue contentDisposition;
                bool hasContentDisposition = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out contentDisposition);

                if (hasContentDisposition && contentDisposition.FileName.HasValue)
                {
                    fileName = contentDisposition.FileName.Value.Trim('"');
                    fileMemoryStream = new MemoryStream();
                    await section.Body.CopyToAsync(fileMemoryStream);
                    fileMemoryStream.Position = 0;
                }

                section = await reader.ReadNextSectionAsync();
            }

            if (fileName == null || fileMemoryStream == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("No file found in the request.");
                return badResponse;
            }

            _service.UploadFile(fileName, fileMemoryStream);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteStringAsync("Uploaded: " + fileName);
            return response;
        }

        [Function("ListStaffDocuments")]
        public async Task<HttpResponseData> ListStaffDocuments(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "documents")] HttpRequestData req)
        {
            var files = _service.ListFiles();
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(files);
            return response;
        }

        [Function("DownloadStaffDocument")]
        public async Task<HttpResponseData> DownloadStaffDocument(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "documents/download/{fileName}")] HttpRequestData req,
            string fileName)
        {
            Stream fileStream = _service.DownloadFile(fileName);

            if (fileStream == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync("File not found.");
                return notFoundResponse;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/octet-stream");
            response.Headers.Add("Content-Disposition", "attachment; filename=" + fileName);
            await fileStream.CopyToAsync(response.Body);
            return response;
        }
    }
}