namespace CoffeeNChillGateway.Models
{
    /// <summary>
    /// Details about one staff document in Blob Storage.
    /// </summary>
    public class StaffDocumentInfo
    {
        /// <summary>The file name.</summary>
        public string FileName { get; set; }

        /// <summary>The size in bytes.</summary>
        public long Size { get; set; }

        /// <summary>The last time the file was changed.</summary>
        public DateTimeOffset? LastModified { get; set; }
    }
}
