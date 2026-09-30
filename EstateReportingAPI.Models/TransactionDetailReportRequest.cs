namespace EstateReportingAPI.Models;

public class TransactionDetailReportRequest
{
    public List<Int32>? Operators { get; set; }
    public List<Int32>? Merchants { get; set; }
    public List<Int32>? Products { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
