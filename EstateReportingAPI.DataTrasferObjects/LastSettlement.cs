using Newtonsoft.Json;

namespace EstateReportingAPI.DataTransferObjects{
    using System;

    public class LastSettlement
    {
        [JsonProperty("settlement_date")]
        public DateTime SettlementDate { get; set; }
        [JsonProperty("sales_value")]
        public Decimal SalesValue { get; set; }
        [JsonProperty("sales_count")]
        public Int32 SalesCount { get; set; }
        [JsonProperty("fees_value")]
        public Decimal FeesValue { get; set; }
    }
}