using System;

namespace EstateReportingAPI.DataTransferObjects;

/// <summary>API data model describing operator detail information.</summary>

public class OperatorDetail
{
    /// <summary>Unique identifier of the operator.</summary>
    public Guid OperatorId { get; set; }
    /// <summary>The operator reporting id value.</summary>
    public Int32 OperatorReportingId { get; set; }
    /// <summary>The operator name value.</summary>
    public string OperatorName { get; set; }
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