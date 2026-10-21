namespace EstateReportingAPI.DataTrasferObjects{
    using System;

    /// <summary>API data model describing calendar date information.</summary>

    public class CalendarDate
    {
        /// <summary>The date value.</summary>
        public DateTime Date { get; set; }
        /// <summary>The day of week value.</summary>
        public String DayOfWeek{ get; set; }
        /// <summary>The day of week number value.</summary>
        public Int32 DayOfWeekNumber{ get; set; }
        /// <summary>The day of week short value.</summary>
        public String DayOfWeekShort{ get; set; }
        /// <summary>The month name value.</summary>
        public String MonthName{ get; set; }
        /// <summary>The month name short value.</summary>
        public String MonthNameShort { get; set; }
        /// <summary>The month number value.</summary>
        public Int32 MonthNumber { get; set; }
        /// <summary>The week number value.</summary>
        public Int32 WeekNumber { get; set; }
        /// <summary>The week number string value.</summary>
        public String WeekNumberString { get; set; }
        /// <summary>The year value.</summary>
        public Int32 Year { get; set; }
        /// <summary>The year week number value.</summary>
        public String YearWeekNumber { get; set; }
    }
}