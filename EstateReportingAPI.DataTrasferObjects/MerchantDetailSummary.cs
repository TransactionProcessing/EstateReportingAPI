using System;
using Newtonsoft.Json;

namespace EstateReportingAPI.DataTransferObjects;

public class MerchantDetailSummary {
    [JsonProperty("total_merchants")]
    public Int32 TotalMerchants { get; set; }
    [JsonProperty("total_count")]
    public Int32 TotalCount { get; set; }
    [JsonProperty("total_value")]
    public Decimal TotalValue { get; set; }
    [JsonProperty("average_value")]
    public Decimal AverageValue { get; set; }
}