namespace EstateReportingAPI.DataTransferObjects{
    using System;

    public class TodaysSettlement
    {
        public Decimal TodaysSettlementValue { get; set; }
        public Int32 TodaysSettlementCount { get; set; }
        public Decimal ComparisonSettlementValue { get; set; }
        public Int32 ComparisonSettlementCount { get; set; }
    }
}