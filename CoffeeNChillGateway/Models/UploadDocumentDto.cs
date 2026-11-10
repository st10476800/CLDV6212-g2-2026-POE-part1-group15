using System.ComponentModel.DataAnnotations;

namespace CoffeeNChillGateway.Models
{
    /// <summary>
    /// The form data for uploading a staff document.
    /// </summary>
    public class UploadDocumentDto
    {
        /// <summary>
        /// The file to upload. It can be up to 10 MB.
        /// </summary>
        [Required]
        public IFormFile File { get; set; }
    }
}
