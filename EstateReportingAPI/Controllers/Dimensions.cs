namespace EstateReportingAPI.Controllers
{
    using DataTrasferObjects;
    using EstateManagement.Database.Contexts;
    using EstateManagement.Database.Entities;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Shared.EntityFramework;
    using System.Diagnostics.CodeAnalysis;

    [ExcludeFromCodeCoverage]
    [Route(DimensionsController.ControllerRoute)]
    [ApiController]
    //[Authorize]
    public class DimensionsController : ControllerBase
    {
        private readonly IDbContextFactory<EstateManagementGenericContext> ContextFactory;

        public DimensionsController(IDbContextFactory<EstateManagementGenericContext> contextFactory){
            this.ContextFactory = contextFactory;
        }

        private const String ConnectionStringIdentifier = "EstateReportingReadModel";

        #region Others

        /// <summary>
        /// The controller name
        /// </summary>
        public const String ControllerName = "dimensions";

        /// <summary>
        /// The controller route
        /// </summary>
        private const String ControllerRoute = "api/" + DimensionsController.ControllerName;

        #endregion

        [HttpGet]
        [Route("calendar/years")]
        public async Task<IActionResult> GetCalendarYears([FromHeader] Guid estateId, CancellationToken cancellationToken){
            
            EstateManagementGenericContext? context = await this.ContextFactory.GetContext(estateId, DimensionsController.ConnectionStringIdentifier, cancellationToken);

            List<Int32> years = context.Calendar.Where(c => c.Date <= DateTime.Now.Date).GroupBy(c => c.Year).Select(y => y.Key).ToList();
            
            List<CalendarYear> response = new List<CalendarYear>();

            years.ForEach(y => response.Add(new CalendarYear{
                                                                Year = y
                                                            }));

            return this.Ok(response);
        }

        [HttpGet]
        [Route("calendar/{year}/dates")]
        public async Task<IActionResult> GetCalendarDates([FromHeader] Guid estateId, [FromRoute] Int32 year, CancellationToken cancellationToken)
        {
            EstateManagementGenericContext? context = await this.ContextFactory.GetContext(estateId, DimensionsController.ConnectionStringIdentifier, cancellationToken);

            List<Calendar> dates = context.Calendar.Where(c => c.Date <= DateTime.Now.Date).ToList();

            List<CalendarDate> response = new List<CalendarDate>();

            dates.ForEach(d => response.Add(new CalendarDate
            {
                Year = d.Year,
                Date = d.Date,
                DayOfWeek = d.DayOfWeek,
                DayOfWeekNumber = d.DayOfWeekNumber,
                DayOfWeekShort = d.DayOfWeekShort,
                MonthName = d.MonthNameLong,
                MonthNameShort = d.MonthNameShort,
                MonthNumber = d.MonthNumber,
                WeekNumber = d.WeekNumber ?? 0,
                WeekNumberString = d.WeekNumberString,
                YearWeekNumber = d.YearWeekNumber,
            }));

            return this.Ok(response);
        }

        [HttpGet]
        [Route("calendar/comparisondates")]
        public async Task<IActionResult> GetCalendarComparisonDates([FromHeader] Guid estateId, CancellationToken cancellationToken)
        {
            EstateManagementGenericContext? context = await this.ContextFactory.GetContext(estateId, DimensionsController.ConnectionStringIdentifier, cancellationToken);

            DateTime startOfYear = new DateTime(DateTime.Now.Year, 1, 1);

            List<Calendar> dates = context.Calendar.Where(c => c.Date >= startOfYear && c.Date < DateTime.Now.Date.AddDays(-1)).OrderByDescending(d => d.Date).ToList();

            List<ComparisonDate> response = new List<ComparisonDate>();

            response.Add(new ComparisonDate{
                                               Date = DateTime.Now.Date.AddDays(-1),
                                               Description = "Yesterday",
                                               OrderValue = 0
                                           });

            response.Add(new ComparisonDate{
                                               Date = DateTime.Now.Date.AddDays(-7),
                                               Description = "Last Week",
                                               OrderValue = 1
                                           });
            response.Add(new ComparisonDate{
                                               Date = DateTime.Now.Date.AddMonths(-1),
                                               Description = "Last Month",
                                               OrderValue = 2
                                           });
            Int32 orderValue = 3;
            dates.ForEach(d => {
                              response.Add(new ComparisonDate{
                                                                 Date = d.Date,
                                                                 Description = d.Date.ToString("yyyy-MM-dd"),
                                                                 OrderValue = orderValue
                                                             });
                              orderValue++;
                          });

            return this.Ok(response.OrderBy(d => d.OrderValue));
        }
    }

    
}
