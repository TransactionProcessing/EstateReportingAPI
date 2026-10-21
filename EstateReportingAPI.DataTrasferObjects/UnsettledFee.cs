using System;

namespace EstateReportingAPI.DataTransferObjects
{
    /// <summary>API data model describing unsettled fee information.</summary>
    public class UnsettledFee
    {
        /// <summary>The dimension name value.</summary>
        public String DimensionName { get; set; }
        /// <summary>The fees value value.</summary>
        public Decimal FeesValue { get; set; }
        /// <summary>The fees count value.</summary>
        public Int32 FeesCount { get; set; }
    }
}
