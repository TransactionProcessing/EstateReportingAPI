namespace EstateReportingAPI.Models;

public class TransactionSummaryByMerchantResponse
{
    public List<MerchantDetail> Merchants { get; set; }
    public MerchantDetailSummary Summary { get; set; }
}