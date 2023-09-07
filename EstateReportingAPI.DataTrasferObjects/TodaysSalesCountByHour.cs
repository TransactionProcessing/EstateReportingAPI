namespace EstateReportingAPI.DataTransferObjects{
    using System;

    public class TodaysSalesCountByHour
    {
        public Int32 Hour { get; set; }
        public Int32 TodaysSalesCount { get; set; }
        public Decimal ComparisonSalesValue { get; set; }
        public Int32 ComparisonSalesCount { get; set; }
    }
}