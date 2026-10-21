using System.Collections.Generic;

namespace EstateReportingAPI.DataTransferObjects;
/// <summary>Response data for transaction summary by merchant.</summary>
public class TransactionSummaryByMerchantResponse
{
    /// <summary>Optional merchant reporting identifiers to include.</summary>
    public List<MerchantDetail> Merchants { get; set; }
    /// <summary>The summary value.</summary>
    public MerchantDetailSummary Summary { get; set; }
    /// <summary>The pagination value.</summary>
    public PaginationMetadata Pagination { get; set; } = new();
}
