using System;

namespace EstateReportingAPI.DataTransferObjects;

/// <summary>API data model describing merchant detail summary information.</summary>

public class MerchantDetailSummary {
    /// <summary>The total merchants value.</summary>
    public Int32 TotalMerchants { get; set; }
    /// <summary>The total count value.</summary>
    public Int32 TotalCount { get; set; }
    /// <summary>Total monetary value represented by this result.</summary>
    public Decimal TotalValue { get; set; }
    /// <summary>Average transaction value represented by this result.</summary>
    public Decimal AverageValue { get; set; }
}