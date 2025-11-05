namespace EstateReportingAPI.DataTransferObjects{
    using Newtonsoft.Json;
    using System;

    public class TodaysSalesCountByHour
    {
        [JsonProperty("hour")]
        public Int32 Hour { get; set; }
        [JsonProperty("todays_sales_count")]
        public Int32 TodaysSalesCount { get; set; }
        [JsonProperty("comparison_sales_count")]
        public Int32 ComparisonSalesCount { get; set; }
    }
}