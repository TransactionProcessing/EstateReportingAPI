using System;

namespace EstateReportingAPI.DataTransferObjects
{
    /// <summary>API data model describing merchant kpi information.</summary>
    public class MerchantKpi
    {
        /// <summary>The merchants with sale in last hour value.</summary>
        public Int32 MerchantsWithSaleInLastHour{ get; set; }
        /// <summary>The merchants with no sale today value.</summary>
        public Int32 MerchantsWithNoSaleToday { get; set; }
        /// <summary>The merchants with no sale in last7 days value.</summary>
        public Int32 MerchantsWithNoSaleInLast7Days { get; set; }
    }
}
