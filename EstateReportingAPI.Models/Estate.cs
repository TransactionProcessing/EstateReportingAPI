namespace EstateReportingAPI.Models;

public class Estate
{
    public Guid EstateId { get; set; }
    public string? EstateName { get; set; }
    public string? Reference { get; set; }
    public List<EstateOperator>? Operators { get; set; }
    public List<EstateMerchant>? Merchants { get; set; }
    public List<EstateContract>? Contracts { get; set; }
    public List<EstateUser>? Users { get; set; }
}