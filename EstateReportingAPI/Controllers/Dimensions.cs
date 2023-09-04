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
        [Route("getcalendaryears")]
        public async Task<IActionResult> GetCalendarYears([FromHeader] Guid estateId, CancellationToken cancellationToken){
            
            EstateManagementGenericContext? context = await this.ContextFactory.GetContext(estateId, DimensionsController.ConnectionStringIdentifier, cancellationToken);

            List<Int32> years = context.Calendar.Where(c => c.Date <= DateTime.Now.Date).GroupBy(c => c.Year).Select(y => y.Key).ToList();
            
            List<CalendarYear> response = new List<CalendarYear>();

            years.ForEach(y => response.Add(new CalendarYear{
                                                                Year = y
                                                            }));

            return this.Ok(response);
        }

        //[HttpGet]
        //public async Task<IActionResult> GetCalendarDates([FromHeader] Guid estateId, [FromQuery] Int32 year, CancellationToken cancellationToken)
        //{
        //    EstateManagementGenericContext? context = await this.ContextFactory.GetContext(estateId, DimensionsController.ConnectionStringIdentifier, cancellationToken);

        //    List<Calendar> dates= context.Calendar.Where(c => c.Date <= DateTime.Now.Date).ToList();

        //    List<CalendarDate> response = new List<CalendarDate>();

        //    dates.ForEach(d => response.Add(new CalendarDate{
        //                                                        Year = d.Year,
        //                                                        Date = d.Date,
        //                                                        DayOfWeek = d.DayOfWeek,
        //                                                        DayOfWeekNumber = d.DayOfWeekNumber,
        //                                                        DayOfWeekShort = d.DayOfWeekShort,
        //                                                        MonthName = d.MonthNameLong,
        //                                                        MonthNameShort = d.MonthNameShort,
        //                                                        MonthNumber = d.MonthNumber,
        //                                                        WeekNumber = d.WeekNumber ?? 0,
        //                                                        WeekNumberString = d.WeekNumberString,
        //                                                        YearWeekNumber = d.YearWeekNumber,
        //                                                    }));

        //    return this.Ok();
        //}
    }

    
}
