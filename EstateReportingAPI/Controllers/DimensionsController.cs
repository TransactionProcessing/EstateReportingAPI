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
        public async Task<IActionResult> GetCalendarYears([FromHeader] Guid estateId, CancellationToken cancellationToken){

           CalendarQueries.GetYearsQuery query = new(estateId);
           Result<List<Int32>> result = await this.Mediator.Send(query, cancellationToken);

           if (result.IsFailed)
               return result.ToActionResultX();

            List<CalendarYear> response = new List<CalendarYear>();

            result.Data.ForEach(y => response.Add(new CalendarYear{
                                                                Year = y
                                                            }));

            return Result.Success(response).ToActionResultX();
        }

        [HttpGet]
        [Route("calendar/{year}/dates")]
        public async Task<IActionResult> GetCalendarDates([FromHeader] Guid estateId, [FromRoute] Int32 year, CancellationToken cancellationToken){
            CalendarQueries.GetAllDatesQuery query = new(estateId);
            Result<List<Calendar>> result = await this.Mediator.Send(query, cancellationToken);
            if (result.IsFailed)
                return result.ToActionResultX();

            List<CalendarDate> response = new List<CalendarDate>();

            result.Data.ForEach(d => response.Add(new CalendarDate{
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

            return Result.Success(response).ToActionResultX();
        }

        [HttpGet]
        [Route("calendar/comparisondates")]
        public async Task<IActionResult> GetCalendarComparisonDates([FromHeader] Guid estateId, CancellationToken cancellationToken){
            CalendarQueries.GetComparisonDatesQuery query = new(estateId);
            Result<List<Calendar>> result = await this.Mediator.Send(query, cancellationToken);
            if (result.IsFailed)
                return result.ToActionResultX();
            
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
            result.Data.ForEach(d => {
                              response.Add(new ComparisonDate{
                                                                 Date = d.Date,
                                                                 Description = d.Date.ToString("yyyy-MM-dd"),
                                                                 OrderValue = orderValue
                                                             });
                              orderValue++;
                          });

            return Result.Success(response.OrderBy(d => d.OrderValue)).ToActionResultX();
        }
        
        [HttpGet]
        [Route("merchants")]
        public async Task<IActionResult> GetMerchants([FromHeader] Guid estateId, CancellationToken cancellationToken) {
            MerchantQueries.GetMerchantsQuery query = new(estateId);
            Result<List<Models.Merchant>> result = await this.Mediator.Send(query, cancellationToken);
            
            List<Merchant> response = new List<Merchant>();

            result.Data.ForEach(m => response.Add(new Merchant{
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

            return Result.Success(response.OrderBy(m=> m.Name)).ToActionResultX();
        }

        [HttpGet]
        [Route("operators")]
        public async Task<IActionResult> GetOperators([FromHeader] Guid estateId, CancellationToken cancellationToken)
        {
            OperatorQueries.GetOperatorsQuery query = new(estateId);
            Result<List<Models.Operator>> result = await this.Mediator.Send(query, cancellationToken);

            List<Operator> response = new List<Operator>();

            result.Data.ForEach(o => response.Add(new Operator
                                                {
                                                    EstateReportingId = o.EstateReportingId,
                                                    Name = o.Name,
                                                    OperatorId = o.OperatorId,
                                                    OperatorReportingId = o.OperatorReportingId
                                                }));

            return Result.Success(response.OrderBy(o => o.Name)).ToActionResultX();
        }

        [HttpGet]
        [Route("responsecodes")]
        public async Task<IActionResult> GetResponseCodes([FromHeader] Guid estateId, CancellationToken cancellationToken)
        {

            ResponseCodeQueries.GetResponseCodesQuery query = new(estateId);
            var result = await this.Mediator.Send(query, cancellationToken);
            List<ResponseCode> response = new List<ResponseCode>();

            result.Data.ForEach(o => response.Add(new ResponseCode{
                                                                        Code = o.Code,
                                                                        Description = o.Description
                                                                    }));

            return Result.Success(response.OrderBy(r => r.Code)).ToActionResultX();
        }
    }


}
