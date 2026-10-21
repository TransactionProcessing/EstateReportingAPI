using System.Collections.Generic;

namespace EstateReportingAPI.DataTransferObjects;
/// <summary>Response data for transaction summary by operator.</summary>
public class TransactionSummaryByOperatorResponse
{
    /// <summary>Optional operator reporting identifiers to include.</summary>
    public List<OperatorDetail> Operators { get; set; }
    /// <summary>The summary value.</summary>
    public OperatorDetailSummary Summary { get; set; }
    /// <summary>The pagination value.</summary>
    public PaginationMetadata Pagination { get; set; } = new();
}
