using System;

namespace EstateReportingAPI.DataTransferObjects;

/// <summary>API data model describing file line information.</summary>

public sealed class FileLine
{
    /// <summary>One-based line number in the imported file.</summary>
    public int LineNumber { get; set; }
    /// <summary>Original contents of the file line.</summary>
    public string LineContents { get; set; }
    /// <summary>Processing status of the file line.</summary>
    public String LineStatus { get; set; }
}