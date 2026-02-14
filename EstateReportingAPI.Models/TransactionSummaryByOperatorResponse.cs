namespace EstateReportingAPI.Models;

public class TransactionSummaryByOperatorResponse
{
    public List<OperatorDetail> Operators { get; set; }
    public OperatorDetailSummary Summary { get; set; }
}