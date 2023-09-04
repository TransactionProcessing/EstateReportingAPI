using System;
using System.Collections.Generic;
using System.Text;

namespace EstateReportingAPI.DataTrasferObjects
{
    public class CalendarYear
    {
        public Int32 Year{ get; set; }
    }

    public class ComparisonDate{
        public Int32 OrderValue{ get; set; }
        public DateTime Date { get; set; }
        public String Description { get; set; }

    }

    public class CalendarDate
    {
        public DateTime Date { get; set; }
        public String DayOfWeek{ get; set; }
        public Int32 DayOfWeekNumber{ get; set; }
        public String DayOfWeekShort{ get; set; }
        public String MonthName{ get; set; }
        public String MonthNameShort { get; set; }
        public Int32 MonthNumber { get; set; }
        public Int32 WeekNumber { get; set; }
        public String WeekNumberString { get; set; }
        public Int32 Year { get; set; }
        public String YearWeekNumber { get; set; }
    }
}
