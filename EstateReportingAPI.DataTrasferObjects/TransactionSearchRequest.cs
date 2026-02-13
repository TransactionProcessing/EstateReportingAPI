using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace EstateReportingAPI.DataTransferObjects;

public class TransactionSearchRequest{

    [JsonProperty("operators")]
    public List<Int32>? Operators{ get; set; }

    [JsonProperty("value_range")]
    public ValueRange? ValueRange{ get; set; }

    [JsonProperty("merchants")]
    public List<Int32>? Merchants{ get; set; }
        
    [JsonProperty("query_date")]
    public DateTime QueryDate{ get; set; }

    [JsonProperty("response_code")]
    public String? ResponseCode{ get; set; }

    [JsonProperty("auth_code")]
    public String? AuthCode{ get; set; }

    [JsonProperty("transaction_number")]
    public String? TransactionNumber{ get; set; }
}