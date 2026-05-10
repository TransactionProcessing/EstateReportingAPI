using System.Collections.Generic;

namespace EstateReportingAPI.DataTransferObjects;
public class TransactionSummaryByMerchantResponse
{
    public List<MerchantDetail> Merchants { get; set; }
    public MerchantDetailSummary Summary { get; set; }
}