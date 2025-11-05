namespace EstateReportingAPI.DataTransferObjects{
    using Newtonsoft.Json;
    using System;

    public class TopBottomOperatorData
    {
        [JsonProperty("operator_name")]
        public String OperatorName { get; set; }
        [JsonProperty("sales_value")]
        public Decimal SalesValue { get; set; }
    }
}