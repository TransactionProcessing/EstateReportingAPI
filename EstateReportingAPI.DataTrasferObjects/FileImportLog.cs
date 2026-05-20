using System;
using System.Collections.Generic;

namespace EstateReportingAPI.DataTransferObjects;

public class FileImportLog
{
    public Guid FileImportLogId { get; set; }
    public DateTime ImportLogDateTime { get; set; }
    public List<FileDetails> FileDetailsList { get; set; }
}