using System;
using System.Collections.Generic;
using System.Text;

namespace EstateReportingAPI.DataTransferObjects
{
    public class MerchantDailyPerformanceSummaryRequest
    {
        public Int32 MerchantReportingId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public sealed class MerchantDailyPerformanceSummaryResponse
    {
        public List<MetricItem> Metrics { get; set; } = [];

        public List<DrillDownTransaction> DrillDownTransactions { get; set; } = [];
    }

    public sealed class MetricItem
    {
        public string Title { get; set; }

        public Decimal Value { get; set; }

        public string Description { get; set; }

        public int Category { get; set; }
    }

    public sealed class DrillDownTransaction
    {
        public string Reference { get; set; }

        public string Product { get; set; }

        public string Status { get; set; }

        public Decimal Amount { get; set; }
        
        public DateTime TransactionDateTime { get; set; }
    }
}
