namespace EstateReportingAPI.Models;

public class Merchant
{
    #region Properties
    public Guid MerchantId { get; set; }
    public Int32 MerchantReportingId { get; set; }
    public String Name { get; set; }
    public String Reference { get; set; }
    public Decimal Balance { get; set; }
    public Int32 SettlementSchedule { get; set; }
    public DateTime CreatedDateTime { get; set; }

    public Guid AddressId { get; set; }
    public String AddressLine1 { get; set; }
    public String AddressLine2 { get; set; }
    public String Town { get; set; }
    public String Region { get; set; }
    public String PostCode { get; set; }
    public String Country { get; set; }

    public Guid ContactId { get; set; }
    public String ContactName { get; set; }
    public String ContactEmail { get; set; }
    public String ContactPhone { get; set; }

    #endregion
}