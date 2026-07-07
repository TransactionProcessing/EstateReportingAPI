using System;
using System.Collections.Generic;

namespace EstateReportingAPI.Models;

public class GetRecentActivityReceiptReportResponse
{
    public DateTime ReportDate { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public List<RecentActivityReceiptItemDto> Items { get; set; } = [];
}

public class RecentActivityReceiptItemDto
{
    public string? Reference { get; set; }
    public string? TransactionType { get; set; }
    public string? Product { get; set; }
    public string? Operator { get; set; }
    public string? Status { get; set; }
    public decimal Amount { get; set; }
    public DateTime TransactionDateTime { get; set; }
    public string? ReceiptReference { get; set; }
}
