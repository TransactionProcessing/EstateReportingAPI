using System;

namespace EstateReportingAPI.DataTransferObjects;

/// <summary>API data model describing transaction detail summary information.</summary>

public class TransactionDetailSummary
{
    /// <summary>Total monetary value represented by this result.</summary>
    public Decimal TotalValue { get; set; }
    /// <summary>The total fees value.</summary>
    public Decimal TotalFees { get; set; }
    /// <summary>Number of transactions represented by this result.</summary>
    public Int32 TransactionCount { get; set; }
}