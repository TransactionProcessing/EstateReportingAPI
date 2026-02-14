namespace EstateReportingAPI.Models;

public class OperatorDetailSummary
{
    public Int32 TotalOperators { get; set; }
    public Int32 TotalCount { get; set; }
    public Decimal TotalValue { get; set; }
    public Decimal AverageValue { get; set; }
}