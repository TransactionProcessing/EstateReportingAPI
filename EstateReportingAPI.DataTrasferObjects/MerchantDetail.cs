using System;

namespace EstateReportingAPI.DataTransferObjects;

public class MerchantDetail {
    public Guid MerchantId { get; set; }
    public Int32 MerchantReportingId { get; set; }
    public string MerchantName { get; set; }
    public Int32 TotalCount { get; set; }
    public Decimal TotalValue { get; set; }
    public Decimal AverageValue { get; set; }
    public Int32 AuthorisedCount { get; set; }
    public Int32 DeclinedCount { get; set; }
    public Decimal AuthorisedPercentage { get; set; }
}