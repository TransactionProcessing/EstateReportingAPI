namespace EstateReportingAPI.Models;

public class FileDetails
{
    public Guid FileId { get; set; }
    public string FileName { get; set; }
    public string FileProfile { get; set; }
    public DateTime DateTimeUploaded { get; set; }
    public Guid UserId { get; set; }
    public string UploadedBy { get; set; }
    public Guid MerchantId { get; set; }
    public string MerchantName { get; set; }
    public List<FileLine> FileLines { get; set; } = new List<FileLine>();
}