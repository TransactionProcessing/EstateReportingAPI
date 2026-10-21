using System;
using System.Collections.Generic;

namespace EstateReportingAPI.DataTransferObjects;

/// <summary>Response data for get recent activity receipt report.</summary>

public class GetRecentActivityReceiptReportResponse
{
    /// <summary>Date for which report data is requested or returned.</summary>
    public DateOnly ReportDate { get; set; }
    /// <summary>The items value.</summary>
    public List<RecentActivityReceiptItemDto> Items { get; set; } = [];
    /// <summary>The pagination value.</summary>
    public PaginationMetadata Pagination { get; set; } = new();
}

/// <summary>Data item included in an API response: Recent Activity Receipt Item Dto.</summary>

public class RecentActivityReceiptItemDto
{
    /// <summary>The reference value.</summary>
    public string? Reference { get; set; }
    /// <summary>The transaction type value.</summary>
    public string? TransactionType { get; set; }
    /// <summary>The product value.</summary>
    public string? Product { get; set; }
    /// <summary>The operator value.</summary>
    public string? Operator { get; set; }
    /// <summary>The status value.</summary>
    public string? Status { get; set; }
    /// <summary>The amount value.</summary>
    public decimal Amount { get; set; }
    /// <summary>Date and time of the transaction.</summary>
    public DateTimeOffset TransactionDateTime { get; set; }
    /// <summary>The receipt reference value.</summary>
    public string? ReceiptReference { get; set; }
}
