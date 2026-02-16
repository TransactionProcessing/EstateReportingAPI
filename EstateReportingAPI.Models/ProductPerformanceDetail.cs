namespace EstateReportingAPI.Models;

public class ProductPerformanceDetail
{
    public String ProductName { get; set; }
    public Guid ProductId { get; set; }
    public Int32 ProductReportingId { get; set; }
    public Guid ContractId { get; set; }
    public Int32 ContractReportingId { get; set; }
    public Int32 TransactionCount { get; set; }
    public Decimal TransactionValue { get; set; }
    public Decimal PercentageOfTotal { get; set; }
}