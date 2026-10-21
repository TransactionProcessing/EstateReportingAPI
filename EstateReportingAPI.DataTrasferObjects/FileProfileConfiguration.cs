using System;

namespace EstateReportingAPI.DataTransferObjects;

/// <summary>API data model describing file profile configuration information.</summary>

public class FileProfileConfiguration
{
    /// <summary>The file profile id value.</summary>
    public Guid FileProfileId { get; set; }
    /// <summary>The name value.</summary>
    public string Name { get; set; }
    /// <summary>The listening directory value.</summary>
    public string ListeningDirectory { get; set; }
    /// <summary>The request type value.</summary>
    public string RequestType { get; set; }
    /// <summary>The operator name value.</summary>
    public string OperatorName { get; set; }
    /// <summary>The line terminator value.</summary>
    public string LineTerminator { get; set; }
    /// <summary>The file format handler value.</summary>
    public string FileFormatHandler { get; set; }
}