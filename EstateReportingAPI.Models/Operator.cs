namespace EstateReportingAPI.Models;

public class Operator
{
    public Int32 EstateReportingId { get; set; }
    public Guid OperatorId { get; set; }
    public String Name { get; set; }
    public Int32 OperatorReportingId { get; set; }
    public Boolean RequireCustomMerchantNumber { get; set; }
    public Boolean RequireCustomTerminalNumber { get; set; }
}