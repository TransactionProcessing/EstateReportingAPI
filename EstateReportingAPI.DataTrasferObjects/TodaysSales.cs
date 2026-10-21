using System;

namespace EstateReportingAPI.DataTransferObjects
{
    /// <summary>API data model describing todays sales information.</summary>
    public class TodaysSales
    {
        /// <summary>The todays average sales value value.</summary>
        public Decimal TodaysAverageSalesValue { get; set; }
        /// <summary>The todays sales value value.</summary>
        public Decimal TodaysSalesValue { get; set; }
        /// <summary>The todays sales count value.</summary>
        public Int32 TodaysSalesCount { get; set; }
        /// <summary>The comparison sales value value.</summary>
        public Decimal ComparisonSalesValue { get; set; }
        /// <summary>The comparison sales count value.</summary>
        public Int32 ComparisonSalesCount { get; set; }
        /// <summary>The comparison average sales value value.</summary>
        public Decimal ComparisonAverageSalesValue { get; set; }
    }
}
