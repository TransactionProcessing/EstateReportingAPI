using System.Collections.Generic;
using Newtonsoft.Json;

namespace EstateReportingAPI.DataTransferObjects;

public class TransactionSummaryByOperatorResponse
{
    [JsonProperty("operators")]
    public List<OperatorDetail> Operators { get; set; }
    [JsonProperty("summary")]
    public OperatorDetailSummary Summary { get; set; }
}