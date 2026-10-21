using System;

namespace EstateReportingAPI.DataTransferObjects;

/// <summary>API data model describing merchant detail information.</summary>

public class MerchantDetail {
    /// <summary>Unique identifier of the merchant.</summary>
    public Guid MerchantId { get; set; }
    /// <summary>Numeric reporting identifier used to filter or identify a merchant.</summary>
    public Int32 MerchantReportingId { get; set; }
    /// <summary>The merchant name value.</summary>
    public string MerchantName { get; set; }
    /// <summary>The total count value.</summary>
    public Int32 TotalCount { get; set; }
    /// <summary>Total monetary value represented by this result.</summary>
    public Decimal TotalValue { get; set; }
    /// <summary>Average transaction value represented by this result.</summary>
    public Decimal AverageValue { get; set; }
    /// <summary>Number of authorised transactions.</summary>
    public Int32 AuthorisedCount { get; set; }
    /// <summary>Number of declined transactions.</summary>
    public Int32 DeclinedCount { get; set; }
    /// <summary>Percentage of transactions that were authorised.</summary>
    public Decimal AuthorisedPercentage { get; set; }
}