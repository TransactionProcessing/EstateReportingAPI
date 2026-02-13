using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace EstateReportingAPI.DataTransferObjects;

public class TransactionDetailReportRequest
{
    [JsonProperty("operators")]
    public List<Int32>? Operators { get; set; }
    [JsonProperty("merchants")]
    public List<Int32>? Merchants { get; set; }
    [JsonProperty("products")]
    public List<Int32>? Products { get; set; }
    [JsonProperty("start_date")]
    public DateTime StartDate { get; set; }
    [JsonProperty("end_date")]
    public DateTime EndDate { get; set; }
}

public class TransactionSummaryByMerchantRequest
{
    [JsonProperty("operators")]
    public List<Int32>? Operators { get; set; }
    [JsonProperty("merchants")]
    public List<Int32>? Merchants { get; set; }
    [JsonProperty("start_date")]
    public DateTime StartDate { get; set; }
    [JsonProperty("end_date")]
    public DateTime EndDate { get; set; }
}