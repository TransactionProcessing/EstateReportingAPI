namespace EstateReportingAPI.Models;

public class LastSettlement
{
    public DateTime SettlementDate { get; set; }
    public Decimal SalesValue { get; set; }
    public Int32 SalesCount { get; set; }
    public Decimal FeesValue { get; set; }
}