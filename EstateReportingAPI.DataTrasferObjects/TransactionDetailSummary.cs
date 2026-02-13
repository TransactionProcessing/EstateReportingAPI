using System;
using Newtonsoft.Json;

namespace EstateReportingAPI.DataTransferObjects;

public class TransactionDetailSummary
{
    [JsonProperty("total_value")]
    public Decimal TotalValue { get; set; }
    [JsonProperty("total_fees")]
    public Decimal TotalFees { get; set; }
    [JsonProperty("transaction_count")]
    public Int32 TransactionCount { get; set; }
}