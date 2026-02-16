using System.Collections.Generic;
using Newtonsoft.Json;

namespace EstateReportingAPI.DataTransferObjects;

public class TransactionSummaryByMerchantResponse
{
    [JsonProperty("merchants")]
    public List<MerchantDetail> Merchants { get; set; }
    [JsonProperty("summary")]
    public MerchantDetailSummary Summary { get; set; }
}