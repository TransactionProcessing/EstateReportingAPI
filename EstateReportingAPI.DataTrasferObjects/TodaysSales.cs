﻿using System;
using System.Collections.Generic;
using System.Text;

namespace EstateReportingAPI.DataTransferObjects
{
    public class TodaysSales
    {
        public Decimal TodaysSalesValue { get; set; }
        public Int32 TodaysSalesCount { get; set; }
        public Decimal ComparisonSalesValue { get; set; }
        public Int32 ComparisonSalesCount { get; set; }
    }
}
