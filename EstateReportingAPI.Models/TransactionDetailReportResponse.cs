namespace EstateReportingAPI.Models;

public class TransactionDetailReportResponse {
    public List<TransactionDetail> Transactions { get; set; }
    public TransactionDetailSummary Summary { get; set; }
}