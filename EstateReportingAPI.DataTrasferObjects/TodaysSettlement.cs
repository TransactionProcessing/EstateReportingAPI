namespace EstateReportingAPI.DataTransferObjects{
    using System;

    public class TodaysSettlement
    {
        public Decimal TodaysSettlementValue { get; set; }
        public Decimal TodaysPendingSettlementValue { get; set; }
        public Int32 TodaysSettlementCount { get; set; }
        public Int32 TodaysPendingSettlementCount { get; set; }
        public Decimal ComparisonSettlementValue { get; set; }
        public Decimal ComparisonPendingSettlementValue { get; set; }
        public Int32 ComparisonSettlementCount { get; set; }
        public Int32 ComparisonPendingSettlementCount { get; set; }
    }
}