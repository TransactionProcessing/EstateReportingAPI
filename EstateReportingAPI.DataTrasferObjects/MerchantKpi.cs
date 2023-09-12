using System;
using System.Collections.Generic;
using System.Text;

namespace EstateReportingAPI.DataTransferObjects
{
    public class MerchantKpi
    {
        public Int32 MerchantsWithSaleInLastHour{ get; set; }
        public Int32 MerchantsWithNoSaleToday { get; set; }
        public Int32 MerchantsWithNoSaleInLast7Days { get; set; }
    }
}
