using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace EstateReportingAPI.DataTransferObjects
{
    public class TodaysSales
    {
        [JsonProperty("todays_average_sales_value")]
        public Decimal TodaysAverageSalesValue { get; set; }
        [JsonProperty("todays_sales_value")]
        public Decimal TodaysSalesValue { get; set; }
        [JsonProperty("todays_sales_count")]
        public Int32 TodaysSalesCount { get; set; }
        [JsonProperty("comparison_sales_value")]
        public Decimal ComparisonSalesValue { get; set; }
        [JsonProperty("comparison_sales_count")]
        public Int32 ComparisonSalesCount { get; set; }
        [JsonProperty("comparison_average_sales_value")]
        public Decimal ComparisonAverageSalesValue { get; set; }
    }
}
