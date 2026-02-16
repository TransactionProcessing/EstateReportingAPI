namespace EstateReportingAPI.Models;

public class ProductPerformanceResponse
{
    public List<ProductPerformanceDetail> ProductDetails { get; set; }
    public ProductPerformanceSummary Summary { get; set; }
}