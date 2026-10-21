using System;
using System.Text.Json.Serialization;

namespace EstateReportingAPI.DataTransferObjects;

/// <summary>API data model describing transaction mix breakdown information.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TransactionMixBreakdown
{
    /// <summary>Group transactions by product.</summary>
    Product,
    /// <summary>Group transactions by transaction type.</summary>
    TransactionType,
    /// <summary>Group transactions by operator.</summary>
    Operator,
    /// <summary>Group transactions by transaction status.</summary>
    Status
}

/// <summary>API data model describing transaction mix measure information.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TransactionMixMeasure
{
    /// <summary>Compare groups by transaction count.</summary>
    Count,
    /// <summary>Compare groups by transaction value.</summary>
    Value
}

/// <summary>Request criteria for transaction mix summary.</summary>

public class TransactionMixSummaryRequest
{
    /// <summary>Numeric reporting identifier used to filter or identify a merchant.</summary>
    public int? MerchantReportingId { get; set; }
    /// <summary>Inclusive start date of the report period.</summary>
    public DateOnly StartDate { get; set; }
    /// <summary>Inclusive end date of the report period.</summary>
    public DateOnly EndDate { get; set; }
    /// <summary>Dimension used to group transactions.</summary>
    public TransactionMixBreakdown Breakdown { get; set; }
    /// <summary>Metric used to rank or compare transaction groups.</summary>
    public TransactionMixMeasure Measure { get; set; }
    /// <summary>Maximum number of groups selected before pagination.</summary>
    public int TopN { get; set; } = 5;
    /// <summary>One-based page number to return.</summary>
    public int PageNumber { get; set; } = 1;
    /// <summary>Maximum number of items to include on a page.</summary>
    public int PageSize { get; set; } = 50;
}
