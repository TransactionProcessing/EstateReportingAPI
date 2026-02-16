namespace EstateReportingAPI.Models;

public class ProductPerformanceSummary
{
    public Int32 TotalProducts { get; set; }
    public Int32 TotalCount { get; set; }
    public Decimal TotalValue { get; set; }
    public Decimal AveragePerProduct { get; set; }
}