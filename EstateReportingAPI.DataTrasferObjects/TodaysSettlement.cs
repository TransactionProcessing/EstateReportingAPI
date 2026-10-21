namespace EstateReportingAPI.DataTransferObjects{
    using System;

    /// <summary>API data model describing todays settlement information.</summary>

    public class TodaysSettlement
    {
        /// <summary>The todays settlement value value.</summary>
        public Decimal TodaysSettlementValue { get; set; }
        /// <summary>The todays pending settlement value value.</summary>
        public Decimal TodaysPendingSettlementValue { get; set; }
        /// <summary>The todays settlement count value.</summary>
        public Int32 TodaysSettlementCount { get; set; }
        /// <summary>The todays pending settlement count value.</summary>
        public Int32 TodaysPendingSettlementCount { get; set; }
        /// <summary>The comparison settlement value value.</summary>
        public Decimal ComparisonSettlementValue { get; set; }
        /// <summary>The comparison pending settlement value value.</summary>
        public Decimal ComparisonPendingSettlementValue { get; set; }
        /// <summary>The comparison settlement count value.</summary>
        public Int32 ComparisonSettlementCount { get; set; }
        /// <summary>The comparison pending settlement count value.</summary>
        public Int32 ComparisonPendingSettlementCount { get; set; }
    }
}