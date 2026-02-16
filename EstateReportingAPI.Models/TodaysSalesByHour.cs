namespace EstateReportingAPI.Models;

public class TodaysSalesByHour
{
    public Int32 Hour { get; set; }
    public Decimal TodaysSalesValue { get; set; }
    public Decimal ComparisonSalesValue { get; set; }
    public Int32 TodaysSalesCount { get; set; }
    public Int32 ComparisonSalesCount { get; set; }
}