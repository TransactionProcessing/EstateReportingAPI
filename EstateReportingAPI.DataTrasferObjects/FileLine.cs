using System;

namespace EstateReportingAPI.DataTransferObjects;

public sealed class FileLine
{
    public int LineNumber { get; set; }
    public string LineContents { get; set; }
    public String LineStatus { get; set; }
}