namespace EstateReportingAPI.Models;

public class TransactionSearchRequest
{
    public TransactionSearchRequest(){
    }
    public List<Int32> Operators { get; set; }

    public ValueRange? ValueRange { get; set; }

    public List<Int32> Merchants { get; set; }

    public DateTime QueryDate { get; set; }

    public String ResponseCode { get; set; }

    public String AuthCode { get; set; }

    public String TransactionNumber { get; set; }
}

public record PagingRequest{
    public Int32 Page{ get; init;}
    public Int32 PageSize{ get; init; }

    public PagingRequest(Int32? page, Int32? pageSize){
        this.Page = page.GetValueOrDefault(1);
        this.PageSize = pageSize.GetValueOrDefault(10);
    }
}

public record SortingRequest{
    public SortField SortField{ get; init; }
    public SortDirection SortDirection { get; init; }

    public SortingRequest(SortField sortField, SortDirection sortDirection){
        this.SortField = sortField;
        this.SortDirection = sortDirection;
    }
}

public class TransactionResult{
    public Guid TransactionId { get; set; }
    public Int32 TransactionReportingId { get; set; }
    public Boolean IsAuthorised{ get; set; }
    public String ResponseCode{ get; set; }
    public String ResponseMessage { get; set; }
    public DateTime TransactionDateTime{ get; set; }
    public String TransactionSource{ get; set; }
    public String OperatorName{ get; set; }
    public Int32 OperatorReportingId { get; set; }
    public String Product { get; set; }
    public Int32 ProductReportingId { get; set; }
    public String MerchantName { get; set; }
    public Int32 MerchantReportingId { get; set; }
    public Decimal TransactionAmount { get; set; }
}

public enum SortField
{
    TransactionAmount = 1,
    MerchantName = 2,
    OperatorName = 3
}

public enum SortDirection
{
    Ascending,
    Descending,
}