using System;
using System.Collections.Generic;

namespace EstateReportingAPI.DataTransferObjects;

/// <summary>Response data for transaction mix summary.</summary>

public class TransactionMixSummaryResponse
{
    /// <summary>The from date value.</summary>
    public DateOnly FromDate { get; set; }
    /// <summary>The to date value.</summary>
    public DateOnly ToDate { get; set; }
    /// <summary>Dimension used to group transactions.</summary>
    public TransactionMixBreakdown Breakdown { get; set; }
    /// <summary>Metric used to rank or compare transaction groups.</summary>
    public TransactionMixMeasure Measure { get; set; }
    /// <summary>The total count value.</summary>
    public int TotalCount { get; set; }
    /// <summary>Total monetary value represented by this result.</summary>
    public decimal TotalValue { get; set; }
    /// <summary>The groups value.</summary>
    public List<TransactionMixSummaryGroup> Groups { get; set; } = [];
    /// <summary>The transactions value.</summary>
    public List<TransactionMixSummaryTransaction> Transactions { get; set; } = [];
    /// <summary>The pagination value.</summary>
    public PaginationMetadata Pagination { get; set; } = new();
}

/// <summary>API data model describing transaction mix summary group information.</summary>

public class TransactionMixSummaryGroup
{
    /// <summary>The group key value.</summary>
    public string? GroupKey { get; set; }
    /// <summary>The group name value.</summary>
    public string? GroupName { get; set; }
    /// <summary>Number of transactions represented by this result.</summary>
    public int TransactionCount { get; set; }
    /// <summary>Total monetary value of the transactions represented by this result.</summary>
    public decimal TransactionValue { get; set; }
}

/// <summary>API data model describing transaction mix summary transaction information.</summary>

public class TransactionMixSummaryTransaction
{
    /// <summary>The id value.</summary>
    public Guid Id { get; set; }
    /// <summary>Date and time of the transaction.</summary>
    public DateTimeOffset DateTime { get; set; }
    /// <summary>The merchant value.</summary>
    public string? Merchant { get; set; }
    /// <summary>Unique identifier of the merchant.</summary>
    public Guid MerchantId { get; set; }
    /// <summary>Numeric reporting identifier used to filter or identify a merchant.</summary>
    public int MerchantReportingId { get; set; }
    /// <summary>The operator value.</summary>
    public string? Operator { get; set; }
    /// <summary>Unique identifier of the operator.</summary>
    public Guid OperatorId { get; set; }
    /// <summary>The operator reporting id value.</summary>
    public int OperatorReportingId { get; set; }
    /// <summary>The product value.</summary>
    public string? Product { get; set; }
    /// <summary>Unique identifier of the product.</summary>
    public Guid ProductId { get; set; }
    /// <summary>The product reporting id value.</summary>
    public int ProductReportingId { get; set; }
    /// <summary>The type value.</summary>
    public string? Type { get; set; }
    /// <summary>The status value.</summary>
    public string? Status { get; set; }
    /// <summary>The value value.</summary>
    public decimal Value { get; set; }
    /// <summary>The total fees value.</summary>
    public decimal TotalFees { get; set; }
    /// <summary>The settlement reference value.</summary>
    public string? SettlementReference { get; set; }
    /// <summary>The transaction number value.</summary>
    public int TransactionNumber { get; set; }
}
