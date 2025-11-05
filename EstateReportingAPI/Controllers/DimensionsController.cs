using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.Models;
using MediatR;
using Shared.Results;
using Shared.Results.Web;
using SimpleResults;

namespace EstateReportingAPI.Controllers
{
    using DataTrasferObjects;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System.Diagnostics.CodeAnalysis;
    using BusinessLogic;
    using IdentityModel;

    [ExcludeFromCodeCoverage]
    [Route(DimensionsController.ControllerRoute)]
    [ApiController]
    [Authorize]
    public class DimensionsController : ControllerBase{
        private readonly IMediator Mediator;

        public DimensionsController(IMediator mediator) {
            this.Mediator = mediator;
        }

        #region Others

        /// <summary>
        /// The controller name
        /// </summary>
        private const String ControllerName = "dimensions";

        /// <summary>
        /// The controller route
        /// </summary>
        private const String ControllerRoute = "api/" + DimensionsController.ControllerName;

        #endregion

        [HttpGet]
        [Route("calendar/years")]
        public async Task<IResult> GetCalendarYears([FromHeader] Guid estateId,
                                                    CancellationToken cancellationToken) {

            CalendarQueries.GetYearsQuery query = new(estateId);
            Result<List<Int32>> result = await this.Mediator.Send(query, cancellationToken);

            return ResponseFactory.FromResult(result, (r) => {
                List<CalendarYear> response = new List<CalendarYear>();

                r.ForEach(y => response.Add(new CalendarYear { Year = y }));

                return response;
            });
        }


        [HttpGet]
        [Route("calendar/{year}/dates")]
        public async Task<IResult> GetCalendarDates([FromHeader] Guid estateId, [FromRoute] Int32 year, CancellationToken cancellationToken){
            CalendarQueries.GetAllDatesQuery query = new(estateId);
            Result<List<Calendar>> result = await this.Mediator.Send(query, cancellationToken);

            return ResponseFactory.FromResult(result, (r) => {
                List<CalendarDate> response = new List<CalendarDate>();

                r.ForEach(d => response.Add(new CalendarDate
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

                return response;
            });
        }

        [HttpGet]
        [Route("calendar/comparisondates")]
        public async Task<IResult> GetCalendarComparisonDates([FromHeader] Guid estateId, CancellationToken cancellationToken){
            CalendarQueries.GetComparisonDatesQuery query = new(estateId);
            Result<List<Calendar>> result = await this.Mediator.Send(query, cancellationToken);

            return ResponseFactory.FromResult(result, (r) => {
                List<ComparisonDate> response = [
                    new ComparisonDate { Date = DateTime.Now.Date.AddDays(-1), Description = "Yesterday", OrderValue = 0 },

                    new ComparisonDate { Date = DateTime.Now.Date.AddDays(-7), Description = "Last Week", OrderValue = 1 },

                    new ComparisonDate { Date = DateTime.Now.Date.AddMonths(-1), Description = "Last Month", OrderValue = 2 }

                ];

                Int32 orderValue = 3;
                r.ForEach(d =>
                {
                    response.Add(new ComparisonDate
                    {
                        Date = d.Date,
                        Description = d.Date.ToString("yyyy-MM-dd"),
                        OrderValue = orderValue
                    });
                    orderValue++;
                });

                return response.OrderBy(d => d.OrderValue);
            });
        }
        
        [HttpGet]
        [Route("merchants")]
        public async Task<IResult> GetMerchants([FromHeader] Guid estateId, CancellationToken cancellationToken) {
            MerchantQueries.GetMerchantsQuery query = new(estateId);
            Result<List<Models.Merchant>> result = await this.Mediator.Send(query, cancellationToken);

            return ResponseFactory.FromResult(result, (r) => {
                List<Merchant> response = new List<Merchant>();

                r.ForEach(m => response.Add(new Merchant {
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

                return response.OrderBy(m => m.Name);
            });
        }

        [HttpGet]
        [Route("operators")]
        public async Task<IResult> GetOperators([FromHeader] Guid estateId, CancellationToken cancellationToken)
        {
            OperatorQueries.GetOperatorsQuery query = new(estateId);
            Result<List<Models.Operator>> result = await this.Mediator.Send(query, cancellationToken);

            return ResponseFactory.FromResult(result, (r) => {
                List<Operator> response = new List<Operator>();

                r.ForEach(o => response.Add(new Operator { EstateReportingId = o.EstateReportingId, Name = o.Name, OperatorId = o.OperatorId, OperatorReportingId = o.OperatorReportingId }));

                return response.OrderBy(o => o.Name);
            });
        }

        [HttpGet]
        [Route("responsecodes")]
        public async Task<IResult> GetResponseCodes([FromHeader] Guid estateId, CancellationToken cancellationToken)
        {

            ResponseCodeQueries.GetResponseCodesQuery query = new(estateId);
            var result = await this.Mediator.Send(query, cancellationToken);

            return ResponseFactory.FromResult(result, (r) => {
                List<ResponseCode> response = new List<ResponseCode>();

                r.ForEach(o => response.Add(new ResponseCode { Code = o.Code, Description = o.Description }));

                return response.OrderBy(r => r.Code);
            });
        }
    }


}
