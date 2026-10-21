using System;
using System.Collections.Generic;
using System.Text;

namespace EstateReportingAPI.DataTransferObjects
{
    /// <summary>Request criteria for merchant daily performance summary.</summary>
    public class MerchantDailyPerformanceSummaryRequest
    {
        /// <summary>Numeric reporting identifier used to filter or identify a merchant.</summary>
        public Int32 MerchantReportingId { get; set; }
        /// <summary>Inclusive start date of the report period.</summary>
        public DateOnly StartDate { get; set; }
        /// <summary>Inclusive end date of the report period.</summary>
        public DateOnly EndDate { get; set; }
    }

    /// <summary>Response data for merchant daily performance summary.</summary>

    public sealed class MerchantDailyPerformanceSummaryResponse
    {
        /// <summary>The metrics value.</summary>
        public List<MetricItem> Metrics { get; set; } = [];

        /// <summary>The drill down transactions value.</summary>

        public List<DrillDownTransaction> DrillDownTransactions { get; set; } = [];
    }

    /// <summary>API data model describing metric item information.</summary>

    public sealed class MetricItem
    {
        /// <summary>The title value.</summary>
        public string Title { get; set; }

        /// <summary>The value value.</summary>

        public Decimal Value { get; set; }

        /// <summary>The description value.</summary>

        public string Description { get; set; }

        /// <summary>The category value.</summary>

        public int Category { get; set; }
        /// <summary>The type value.</summary>
        public Int32 Type { get; set; }
    }

    /// <summary>API data model describing drill down transaction information.</summary>

    public sealed class DrillDownTransaction
    {
        /// <summary>The reference value.</summary>
        public string Reference { get; set; }

        /// <summary>The product value.</summary>

        public string Product { get; set; }

        /// <summary>The status value.</summary>

        public string Status { get; set; }

        /// <summary>The amount value.</summary>

        public Decimal Amount { get; set; }
        
        /// <summary>Date and time of the transaction.</summary>
        
        public DateTimeOffset TransactionDateTime { get; set; }
    }
}
