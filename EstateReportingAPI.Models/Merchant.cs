namespace EstateReportingAPI.Models;

public class Merchant
{
    #region Properties

    public DateTime CreatedDateTime { get; set; }
    public Int32 EstateReportingId { get; set; }
    public DateTime LastSale { get; set; }
    public DateTime LastSaleDateTime { get; set; }
    public DateTime LastStatement { get; set; }
    public Guid MerchantId { get; set; }
    public Int32 MerchantReportingId { get; set; }
    public String Name { get; set; }
    public String PostCode { get; set; }
    public String Reference { get; set; }
    public String Region { get; set; }
    public String Town { get; set; }

    #endregion
}