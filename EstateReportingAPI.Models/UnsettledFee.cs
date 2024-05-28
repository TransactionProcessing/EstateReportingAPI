using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EstateReportingAPI.Models
{
    public class UnsettledFee
    {
        public String DimensionName{ get; set; }
        public Decimal FeesValue{ get; set; }
        public Int32 FeesCount { get; set; }
    }
}
