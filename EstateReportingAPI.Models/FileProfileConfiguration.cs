namespace EstateReportingAPI.Models;

public class FileProfileConfiguration
{
    public Guid FileProfileId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ListeningDirectory { get; set; } = string.Empty;
    public string RequestType { get; set; } = string.Empty;
    public string OperatorName { get; set; } = string.Empty;
    public string LineTerminator { get; set; } = string.Empty;
    public string FileFormatHandler { get; set; } = string.Empty;
}