namespace EstateReportingAPI.DataTransferObjects{
    using Newtonsoft.Json;
    using System;

    public class TodaysSalesValueByHour{
        [JsonProperty("hour")]
        public Int32 Hour{ get; set; }
        [JsonProperty("todays_sales_value")]
        public Decimal TodaysSalesValue { get; set; }
        [JsonProperty("comparison_sales_value")]
        public Decimal ComparisonSalesValue { get; set; }
    }
}