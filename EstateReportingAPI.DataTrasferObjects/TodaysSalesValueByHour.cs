namespace EstateReportingAPI.DataTransferObjects{
    using System;

    public class TodaysSalesValueByHour{
        public Int32 Hour{ get; set; }
        public Decimal TodaysSalesValue { get; set; }
        public Decimal ComparisonSalesValue { get; set; }
    }
}