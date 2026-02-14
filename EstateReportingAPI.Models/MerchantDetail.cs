namespace EstateReportingAPI.Models;

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

public class OperatorDetail
{
    public Guid OperatorId { get; set; }
    public Int32 OperatorReportingId { get; set; }
    public string OperatorName { get; set; }
    public Int32 TotalCount { get; set; }
    public Decimal TotalValue { get; set; }
    public Decimal AverageValue { get; set; }
    public Int32 AuthorisedCount { get; set; }
    public Int32 DeclinedCount { get; set; }
    public Decimal AuthorisedPercentage { get; set; }
}