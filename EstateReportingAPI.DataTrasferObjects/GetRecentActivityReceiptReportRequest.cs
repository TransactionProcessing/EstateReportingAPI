using System;

namespace EstateReportingAPI.DataTransferObjects;

/// <summary>Request criteria for get recent activity receipt report.</summary>

public class GetRecentActivityReceiptReportRequest
{
    /// <summary>Date for which report data is requested or returned.</summary>
    public DateOnly ReportDate { get; set; }
    /// <summary>Numeric reporting identifier used to filter or identify a merchant.</summary>
    public int? MerchantReportingId { get; set; }
    /// <summary>Optional text used to search matching activity.</summary>
    public string? SearchText { get; set; }
    /// <summary>One-based page number to return.</summary>
    public int PageNumber { get; set; } = 1;
    /// <summary>Maximum number of items to include on a page.</summary>
    public int PageSize { get; set; } = 10;
}
