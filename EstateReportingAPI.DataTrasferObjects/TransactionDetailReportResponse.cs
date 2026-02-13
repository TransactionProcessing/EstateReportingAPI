using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace EstateReportingAPI.DataTransferObjects;

public class TransactionDetailReportResponse
{
    [JsonProperty("transactions")]
    public List<TransactionDetail> Transactions { get; set; }
    [JsonProperty("summary")]
    public TransactionDetailSummary Summary { get; set; }
}

public class TransactionSummaryByMerchantResponse
{
    [JsonProperty("merchants")]
    public List<MerchantDetail> Merchants { get; set; }
    [JsonProperty("summary")]
    public MerchantDetailSummary Summary { get; set; }
}

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