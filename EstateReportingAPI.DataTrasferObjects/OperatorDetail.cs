using System;
using Newtonsoft.Json;

namespace EstateReportingAPI.DataTransferObjects;

public class OperatorDetail
{
    [JsonProperty("operator_id")]
    public Guid OperatorId { get; set; }
    [JsonProperty("operator_reporting_id")]
    public Int32 OperatorReportingId { get; set; }
    [JsonProperty("operator_name")]
    public string OperatorName { get; set; }
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