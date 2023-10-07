using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EstateReportingAPI.IntegrationTests
{
    using BusinessLogic;
    using EstateManagement.Database.Entities;
    using EstateReportingAPI.Common;
    using Microsoft.Extensions.DependencyInjection;
    using Shared.EntityFramework;
    using System.Globalization;
    using Calendar = System.Globalization.Calendar;

    public static class Extensions{
        public static EstateManagement.Database.Entities.Calendar ToCalendar(this DateTime date){
            return new EstateManagement.Database.Entities.Calendar{
                                   Date = date,
                                   DayOfWeek = date.DayOfWeek.ToString(),
                                   DayOfWeekShort = date.DayOfWeek.ToString().Substring(0, 3),
                                   YearWeekNumber = $"{date.Year}{date.GetWeekNumber().ToString().PadLeft(2, '0')}",
                                   WeekNumberString = date.GetWeekNumber().ToString().PadLeft(2, '0'),
                                   MonthNameLong = date.ToString("MMMM"),
                                   MonthNameShort = date.ToString("MMM"),
                                   Year = date.Year,
                                   DayOfWeekNumber = date.GetDayOfWeekNumber(),
                                   MonthNumber = date.Month,
                                   WeekNumber = date.GetWeekNumber()
            };
        }

        public static Int32 GetWeekNumber(this DateTime date){
            // Define the calendar to use (in this case, the Gregorian calendar)
            Calendar calendar = CultureInfo.InvariantCulture.Calendar;

            // Get the week number for the current date
            int weekNumber = calendar.GetWeekOfYear(date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            return weekNumber;
        }

        public static Int32 GetDayOfWeekNumber(this DateTime date)
        {
            // Define the calendar to use (in this case, the Gregorian calendar)
            Calendar calendar = CultureInfo.InvariantCulture.Calendar;

            // Get the week number for the current date
            int dayOfWeekNumber = (Int32)calendar.GetDayOfWeek(date);
            return dayOfWeekNumber;
        }
    }
}
