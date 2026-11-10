using Azure;
using CoffeeNChillGateway.Models;
using CoffeeNChillGateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChillGateway.Controllers
{
    /// <summary>
    /// Staff documents stored in Blob Storage. Only Admin and Staff can use these endpoints.
    /// </summary>
    [ApiController]
    [Route("api/documents")]
    [Authorize(Roles = "Admin,Staff")]
    public class DocumentsController : ControllerBase
    {
        // The biggest file we accept: 10 MB
        private const long MaxFileSize = 10 * 1024 * 1024;

        private BlobStorageService _blobService;
        private ILogger<DocumentsController> _logger;

        public DocumentsController(BlobStorageService blobService, ILogger<DocumentsController> logger)
        {
            _blobService = blobService;
            _logger = logger;
        }

        /// <summary>
        /// Uploads a staff document.
        /// </summary>
        /// <param name="dto">The form with the file. It can be up to 10 MB.</param>
        /// <response code="201">The file was uploaded.</response>
        /// <response code="400">There is no file, the file is empty or it is too big.</response>
        /// <response code="503">The storage is not reachable.</response>
        [HttpPost]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> UploadDocument([FromForm] UploadDocumentDto dto)
        {
            IFormFile file = dto.File;

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { Message = "No file found in the request, or the file was empty." });
            }

            if (file.Length > MaxFileSize)
            {
                return BadRequest(new { Message = "The file is too big. The limit is 10 MB." });
            }

            // Path.GetFileName removes any folder path the client might send with the name
            string fileName = Path.GetFileName(file.FileName);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return BadRequest(new { Message = "The file needs a name." });
            }

            try
            {
                using (Stream stream = file.OpenReadStream())
                {
                    await _blobService.UploadFileAsync(fileName, stream);
                }
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Blob Storage error while uploading {FileName}.", fileName);
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new { Message = "Could not save the file to Blob Storage." });
            }

            _logger.LogInformation("Uploaded {FileName} ({Size} bytes).", fileName, file.Length);
            return StatusCode(StatusCodes.Status201Created,
                new { Message = "Uploaded: " + fileName, FileName = fileName, Size = file.Length });
        }

        /// <summary>
        /// Lists all staff documents with their size and last modified date.
        /// </summary>
        /// <response code="200">The list of documents.</response>
        /// <response code="503">The storage is not reachable.</response>
        [HttpGet]
        [ProducesResponseType(typeof(List<StaffDocumentInfo>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> ListDocuments()
        {
            try
            {
                List<StaffDocumentInfo> files = await _blobService.ListFilesAsync();
                return Ok(files);
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Blob Storage error while listing documents.");
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new { Message = "Could not reach Blob Storage." });
            }
        }

        /// <summary>
        /// Downloads a staff document.
        /// </summary>
        /// <param name="fileName">The name of the file, for example "rota.pdf".</param>
        /// <response code="200">The file.</response>
        /// <response code="404">There is no file with this name.</response>
        /// <response code="503">The storage is not reachable.</response>
        [HttpGet("{fileName}")]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> DownloadDocument(string fileName)
        {
            try
            {
                Stream fileStream = await _blobService.DownloadFileAsync(fileName);
                if (fileStream == null)
                {
                    return NotFound(new { Message = "File not found." });
                }

                return File(fileStream, "application/octet-stream", fileName);
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Blob Storage error while downloading {FileName}.", fileName);
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new { Message = "Could not reach Blob Storage." });
            }
        }

        /// <summary>
        /// Deletes a staff document.
        /// </summary>
        /// <param name="fileName">The name of the file to delete.</param>
        /// <response code="204">The file was deleted.</response>
        /// <response code="404">There is no file with this name.</response>
        /// <response code="503">The storage is not reachable.</response>
        [HttpDelete("{fileName}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> DeleteDocument(string fileName)
        {
            try
            {
                bool deleted = await _blobService.DeleteFileAsync(fileName);
                if (deleted == false)
                {
                    return NotFound(new { Message = "File not found." });
                }

                _logger.LogInformation("Deleted {FileName}.", fileName);
                return NoContent();
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Blob Storage error while deleting {FileName}.", fileName);
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new { Message = "Could not reach Blob Storage." });
            }
        }
    }
}