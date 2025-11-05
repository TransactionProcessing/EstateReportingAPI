namespace EstateReportingAPI.DataTransferObjects{
    using Newtonsoft.Json;
    using System;

    public class TopBottomMerchantData
    {
        [JsonProperty("merchant_name")]
        public String MerchantName { get; set; }
        [JsonProperty("sales_value")]
        public Decimal SalesValue { get; set; }
    }
}