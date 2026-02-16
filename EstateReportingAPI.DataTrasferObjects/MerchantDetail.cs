using System;
using Newtonsoft.Json;

namespace EstateReportingAPI.DataTransferObjects;

public class MerchantDetail {
    [JsonProperty("merchant_id")]
    public Guid MerchantId { get; set; }
    [JsonProperty("merchant_reporting_id")]
    public Int32 MerchantReportingId { get; set; }
    [JsonProperty("merchant_name")]
    public string MerchantName { get; set; }
    [JsonProperty("total_count")]
    public Int32 TotalCount { get; set; }
    [JsonProperty("total_value")]   
    public Decimal TotalValue { get; set; }
    [JsonProperty("average_value")]    
    public Decimal AverageValue { get; set; }
    [JsonProperty("authorised_count")]    
    public Int32 AuthorisedCount { get; set; }
    [JsonProperty("declined_count")]    
    public Int32 DeclinedCount { get; set; }
    [JsonProperty("authorised_percentage")]
    public Decimal AuthorisedPercentage { get; set; }
}