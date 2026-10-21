namespace EstateReportingAPI.DataTransferObjects{
    using System;

    /// <summary>API data model describing todays sales by hour information.</summary>

    public class TodaysSalesByHour{
        /// <summary>The hour value.</summary>
        public Int32 Hour{ get; set; }
        /// <summary>The todays sales value value.</summary>
        public Decimal TodaysSalesValue { get; set; }
        /// <summary>The comparison sales value value.</summary>
        public Decimal ComparisonSalesValue { get; set; }
        /// <summary>The todays sales count value.</summary>
        public Int32 TodaysSalesCount { get; set; }
        /// <summary>The comparison sales count value.</summary>
        public Int32 ComparisonSalesCount { get; set; }
    }
}