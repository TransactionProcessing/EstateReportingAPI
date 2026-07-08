using System;

namespace EstateReportingAPI.Models;

public class GetRecentActivityReceiptReportRequest
{
    public DateTime ReportDate { get; set; }
    public int? MerchantReportingId { get; set; }
    public string? SearchText { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
