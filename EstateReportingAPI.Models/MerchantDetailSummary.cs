namespace EstateReportingAPI.Models;

public class MerchantDetailSummary
{
    public Int32 TotalMerchants { get; set; }
    public Int32 TotalCount { get; set; }
    public Decimal TotalValue { get; set; }
    public Decimal AverageValue { get; set; }
}