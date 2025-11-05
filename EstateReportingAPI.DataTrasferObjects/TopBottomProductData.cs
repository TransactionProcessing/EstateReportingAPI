namespace EstateReportingAPI.DataTransferObjects{
    using Newtonsoft.Json;
    using System;

    public class TopBottomProductData
    {
        [JsonProperty("product_name")]
        public String ProductName { get; set; }
        [JsonProperty("sales_value")]
        public Decimal SalesValue { get; set; }
    }
}