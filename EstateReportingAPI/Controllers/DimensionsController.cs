namespace EstateReportingAPI.Controllers
{
    using DataTrasferObjects;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System.Diagnostics.CodeAnalysis;
    using BusinessLogic;

    [ExcludeFromCodeCoverage]
    [Route(DimensionsController.ControllerRoute)]
    [ApiController]
    [Authorize]
    public class DimensionsController : ControllerBase{
        private readonly IReportingManager ReportingManager;

        public DimensionsController(IReportingManager reportingManager){
            this.ReportingManager = reportingManager;
        }

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

            List<Int32> years = await this.ReportingManager.GetCalendarYears(estateId, cancellationToken);

            List<CalendarYear> response = new List<CalendarYear>();

            years.ForEach(y => response.Add(new CalendarYear{
                                                                Year = y
                                                            }));

            return this.Ok(response);
        }

        [HttpGet]
        [Route("calendar/{year}/dates")]
        public async Task<IActionResult> GetCalendarDates([FromHeader] Guid estateId, [FromRoute] Int32 year, CancellationToken cancellationToken){
            List<Models.Calendar> dates = await this.ReportingManager.GetCalendarDates(estateId, cancellationToken);

            List<CalendarDate> response = new List<CalendarDate>();

            dates.ForEach(d => response.Add(new CalendarDate{
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
        public async Task<IActionResult> GetCalendarComparisonDates([FromHeader] Guid estateId, CancellationToken cancellationToken){
            List<Models.Calendar> dates = await this.ReportingManager.GetCalendarComparisonDates(estateId, cancellationToken);

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
        
        [HttpGet]
        [Route("merchants")]
        public async Task<IActionResult> GetMerchants([FromHeader] Guid estateId, CancellationToken cancellationToken){
            List<Models.Merchant> merchants = await this.ReportingManager.GetMerchants(estateId, cancellationToken);

            List<Merchant> response = new List<Merchant>();

            merchants.ForEach(m => response.Add(new Merchant{
                                                                MerchantReportingId = m.MerchantReportingId,
                                                                MerchantId = m.MerchantId,
                                                                EstateReportingId = m.EstateReportingId,
                                                                Name = m.Name,
                                                                LastSaleDateTime = m.LastSaleDateTime,
                                                                CreatedDateTime = m.CreatedDateTime,
                                                                LastSale = m.LastSale,
                                                                LastStatement = m.LastStatement,
                                                                PostCode = m.PostCode,
                                                                Reference = m.Reference,
                                                                Region = m.Region,
                                                                Town = m.Town,
                                                            }));

            return this.Ok(response.OrderBy(m => m.Name));
        }

        [HttpGet]
        [Route("operators")]
        public async Task<IActionResult> GetOperators([FromHeader] Guid estateId, CancellationToken cancellationToken)
        {
            List<Models.Operator> operators = await this.ReportingManager.GetOperators(estateId, cancellationToken);

            List<Operator> response = new List<Operator>();

            operators.ForEach(o => response.Add(new Operator
                                                {
                                                    EstateReportingId = o.EstateReportingId,
                                                    Name = o.Name,
                                                    OperatorId = o.OperatorId
                                                }));

            return this.Ok(response.OrderBy(m => m.Name));
        }
    }


}
