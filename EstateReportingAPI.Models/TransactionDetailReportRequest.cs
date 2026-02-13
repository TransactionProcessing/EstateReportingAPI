namespace EstateReportingAPI.Models;

public class TransactionDetailReportRequest
{
    public List<Int32>? Operators { get; set; }
    public List<Int32>? Merchants { get; set; }
    public List<Int32>? Products { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class TransactionDetailReportResponse {
    public List<TransactionDetail> Transactions { get; set; }
    public TransactionDetailSummary Summary { get; set; }
}

public class TransactionDetailSummary {
    public Decimal TotalValue { get; set; }
    public Decimal TotalFees { get; set; }
    public Int32 TransactionCount { get; set; }
}

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

public class TransactionSummaryByMerchantRequest
{
    public List<Int32>? Operators { get; set; }
    public List<Int32>? Merchants { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class TransactionSummaryByMerchantResponse
{
    public List<MerchantDetail> Merchants { get; set; }
    public MerchantDetailSummary Summary { get; set; }
}

public class MerchantDetail
{
    public Guid MerchantId { get; set; }
    public Int32 MerchantReportingId { get; set; }
    public string MerchantName { get; set; }
    public Int32 TotalCount { get; set; }
    public Decimal TotalValue { get; set; }
    public Decimal AverageValue { get; set; }
    public Int32 AuthorisedCount { get; set; }
    public Int32 DeclinedCount { get; set; }
    public Decimal AuthorisedPercentage { get; set; }
}

public class MerchantDetailSummary
{
    public Int32 TotalMerchants { get; set; }
    public Int32 TotalCount { get; set; }
    public Decimal TotalValue { get; set; }
    public Decimal AverageValue { get; set; }
}