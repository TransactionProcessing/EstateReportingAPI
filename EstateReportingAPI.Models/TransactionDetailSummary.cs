namespace EstateReportingAPI.Models;

public class TransactionDetailSummary {
    public Decimal TotalValue { get; set; }
    public Decimal TotalFees { get; set; }
    public Int32 TransactionCount { get; set; }
}