using System;
using System.Collections.Generic;

namespace EstateReportingAPI.DataTransferObjects;

/// <summary>API data model describing file import log information.</summary>

public class FileImportLog
{
    /// <summary>The file import log id value.</summary>
    public Guid FileImportLogId { get; set; }
    /// <summary>Date and time when the import log was recorded.</summary>
    public DateTimeOffset ImportLogDateTime { get; set; }
    /// <summary>The file details list value.</summary>
    public List<FileDetails> FileDetailsList { get; set; }
}
