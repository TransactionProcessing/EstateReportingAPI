namespace EstateReportingAPI.DataTransferObjects{
    using Newtonsoft.Json;
    using System;

    public class TodaysSalesByHour{
        [JsonProperty("hour")]
        public Int32 Hour{ get; set; }
        [JsonProperty("todays_sales_value")]
        public Decimal TodaysSalesValue { get; set; }
        [JsonProperty("comparison_sales_value")]
        public Decimal ComparisonSalesValue { get; set; }
        [JsonProperty("todays_sales_count")]
        public Int32 TodaysSalesCount { get; set; }
        [JsonProperty("comparison_sales_count")]
        public Int32 ComparisonSalesCount { get; set; }
    }
}