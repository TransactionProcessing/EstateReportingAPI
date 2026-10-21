using System;
using System.Collections.Generic;

namespace EstateReportingAPI.DataTransferObjects;
/// <summary>Request criteria for transaction summary by operator.</summary>
public class TransactionSummaryByOperatorRequest
{
    /// <summary>Optional operator reporting identifiers to include.</summary>
    public List<Int32>? Operators { get; set; }
    /// <summary>Optional merchant reporting identifiers to include.</summary>
    public List<Int32>? Merchants { get; set; }
    /// <summary>Inclusive start date of the report period.</summary>
    public DateOnly StartDate { get; set; }
    /// <summary>Inclusive end date of the report period.</summary>
    public DateOnly EndDate { get; set; }
    /// <summary>One-based page number to return.</summary>
    public int PageNumber { get; set; } = 1;
    /// <summary>Maximum number of items to include on a page.</summary>
    public int PageSize { get; set; } = 50;
}
