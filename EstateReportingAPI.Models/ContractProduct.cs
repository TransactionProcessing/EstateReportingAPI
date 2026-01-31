namespace EstateReportingAPI.Models;

public class ContractProduct
{   public Guid ContractId { get; set; }
    public Guid ProductId { get; set; }
    public String ProductName { get; set; }
    public String DisplayText { get; set; }
    public Int32 ProductType { get; set; }
    public Decimal? Value { get; set; }
    public List<ContractProductTransactionFee> TransactionFees { get; set; }
}