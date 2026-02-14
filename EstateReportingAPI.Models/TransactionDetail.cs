namespace EstateReportingAPI.Models;

public class TransactionDetail {
    public Guid Id { get; set; }
    public DateTime DateTime { get; set; }
    public String Merchant { get; set; }
    public Guid MerchantId { get; set; }
    public Int32 MerchantReportingId { get; set; }
    public String Operator { get; set; }
    public Guid OperatorId { get; set; }
    public Int32 OperatorReportingId { get; set; }
    public String Product { get; set; }
    public Guid ProductId { get; set; }
    public Int32 ProductReportingId { get; set; }
    public String Type { get; set; }
    public String Status { get; set; }
    public Decimal Value { get; set; }
    public Decimal TotalFees { get; set; }
    public String SettlementReference { get; set; }
}