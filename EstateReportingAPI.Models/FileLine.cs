namespace EstateReportingAPI.Models;

public sealed class FileLine
{
    public int LineNumber { get; set; }
    public string LineContents { get; set; }
    public String LineStatus { get; set; }
}