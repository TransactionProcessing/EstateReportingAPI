namespace EstateReportingAPI.DataTransferObjects{
    using Newtonsoft.Json;
    using System;

    public class TodaysSettlement
    {
        [JsonProperty("todays_settlement_value")]
        public Decimal TodaysSettlementValue { get; set; }
        [JsonProperty("todays_pending_settlement_value")]
        public Decimal TodaysPendingSettlementValue { get; set; }
        [JsonProperty("todays_settlement_count")]
        public Int32 TodaysSettlementCount { get; set; }
        [JsonProperty("todays_pending_settlement_count")]
        public Int32 TodaysPendingSettlementCount { get; set; }
        [JsonProperty("comparison_settlement_value")]
        public Decimal ComparisonSettlementValue { get; set; }
        [JsonProperty("comparison_pending_settlement_value")]
        public Decimal ComparisonPendingSettlementValue { get; set; }
        [JsonProperty("comparison_settlement_count")]
        public Int32 ComparisonSettlementCount { get; set; }
        [JsonProperty("comparison_pending_settlement_count")]
        public Int32 ComparisonPendingSettlementCount { get; set; }
    }
}