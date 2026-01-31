namespace EstateReportingAPI.Models;

public class MerchantContractProduct
{
    public Guid MerchantId { get; set; }
    public Guid ContractId { get; set; }
    public Guid ProductId { get; set; }
    public String ProductName { get; set; }
    public String DisplayText { get; set; }
    public Int32 ProductType { get; set; }
    public Decimal? Value { get; set; }
}