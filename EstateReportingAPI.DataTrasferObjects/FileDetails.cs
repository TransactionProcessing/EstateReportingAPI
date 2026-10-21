using System;
using System.Collections.Generic;

namespace EstateReportingAPI.DataTransferObjects;

/// <summary>API data model describing file details information.</summary>

public class FileDetails
{
    /// <summary>The file id value.</summary>
    public Guid FileId { get; set; }
    /// <summary>The file name value.</summary>
    public string FileName { get; set; }
    /// <summary>The file profile value.</summary>
    public string FileProfile { get; set; }
    /// <summary>Date and time when the file was uploaded.</summary>
    public DateTimeOffset DateTimeUploaded { get; set; }
    /// <summary>The user id value.</summary>
    public Guid UserId { get; set; }
    /// <summary>The uploaded by value.</summary>
    public string UploadedBy { get; set; }
    /// <summary>Unique identifier of the merchant.</summary>
    public Guid MerchantId { get; set; }
    /// <summary>The merchant name value.</summary>
    public string MerchantName { get; set; }
    /// <summary>The file lines value.</summary>
    public List<FileLine> FileLines { get; set; } = new List<FileLine>();
}
