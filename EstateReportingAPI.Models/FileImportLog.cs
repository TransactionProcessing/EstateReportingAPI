namespace EstateReportingAPI.Models;

public class FileImportLog
{
    public Guid FileImportLogId { get; set; }
    public DateTime ImportLogDateTime { get; set; }
    public List<FileDetails> FileDetailsList { get; set; }
}