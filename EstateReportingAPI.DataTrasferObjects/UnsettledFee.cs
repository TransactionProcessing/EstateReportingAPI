using System;
using System.Collections.Generic;
using System.Text;

namespace EstateReportingAPI.DataTransferObjects
{
    public class UnsettledFee
    {
        public String DimensionName { get; set; }
        public Decimal FeesValue { get; set; }
        public Int32 FeesCount { get; set; }
    }
}
