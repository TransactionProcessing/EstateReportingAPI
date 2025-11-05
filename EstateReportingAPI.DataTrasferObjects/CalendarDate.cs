namespace EstateReportingAPI.DataTrasferObjects{
    using Newtonsoft.Json;
    using System;

    public class CalendarDate
    {
        [JsonProperty("date")]
        public DateTime Date { get; set; }
        [JsonProperty("day_of_week")]
        public String DayOfWeek{ get; set; }
        [JsonProperty("day_of_week_number")]
        public Int32 DayOfWeekNumber{ get; set; }
        [JsonProperty("day_of_week_short")]
        public String DayOfWeekShort{ get; set; }
        [JsonProperty("month_name")]
        public String MonthName{ get; set; }
        [JsonProperty("month_name_short")]
        public String MonthNameShort { get; set; }
        [JsonProperty("month_number")]
        public Int32 MonthNumber { get; set; }
        [JsonProperty("week_number")]
        public Int32 WeekNumber { get; set; }
        [JsonProperty("week_number_string")]
        public String WeekNumberString { get; set; }
        [JsonProperty("year")]
        public Int32 Year { get; set; }
        [JsonProperty("year_week_number")]
        public String YearWeekNumber { get; set; }
    }
}