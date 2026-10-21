namespace EstateReportingAPI.DataTransferObjects{
    using System;

    /// <summary>API data model describing last settlement information.</summary>

    public class LastSettlement
    {
        /// <summary>The settlement date value.</summary>
        public DateTime SettlementDate { get; set; }
        /// <summary>The sales value value.</summary>
        public Decimal SalesValue { get; set; }
        /// <summary>The sales count value.</summary>
        public Int32 SalesCount { get; set; }
        /// <summary>The fees value value.</summary>
        public Decimal FeesValue { get; set; }
    }
}