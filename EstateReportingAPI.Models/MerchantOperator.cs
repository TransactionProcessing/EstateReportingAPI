namespace EstateReportingAPI.Models;

public class MerchantOperator {
    public Guid MerchantId { get; set; }
    public Guid OperatorId { get; set; }
    public String OperatorName { get; set; }
    public String MerchantNumber { get; set; }
    public String TerminalNumber { get; set; }
    public Boolean IsDeleted { get; set; }
}