namespace EstateReportingAPI.Models;

public class TodaysSales
{
    public Decimal TodaysSalesValue { get; set; }
    public Decimal TodaysAverageSalesValue { get; set; }

    public Int32 TodaysSalesCount { get; set; }
    public Decimal ComparisonSalesValue { get; set; }
    public Decimal ComparisonAverageSalesValue { get; set; }
    public Int32 ComparisonSalesCount { get; set; }
}