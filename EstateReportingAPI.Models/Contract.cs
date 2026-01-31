namespace EstateReportingAPI.Models;

public class Contract
{
    #region Properties
    public Guid EstateId { get; set; }
    public Int32 EstateReportingId { get; set; }
    public Guid ContractId { get; set; }
    public Int32 ContractReportingId { get; set; }
    public String Description { get; set; }
    public String OperatorName { get; set; }
    public Guid OperatorId { get; set; }
    public Int32 OperatorReportingId { get; set; }
    public List<ContractProduct> Products { get; set; }

    #endregion
}