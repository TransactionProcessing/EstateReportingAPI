using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;
using EstateReportingAPI.BusinessLogic.Queries;
using Shared.Results.Web;

namespace EstateReportingAPI.Controllers
{
    using BusinessLogic;
    using MediatR;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc.Formatters;
    using Models;
    using Shared.Results;
    using SimpleResults;
    using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
    using GroupByOption = DataTransferObjects.GroupByOption;
    using LastSettlement = Models.LastSettlement;
    using TodaysSettlement = DataTransferObjects.TodaysSettlement;

    [ExcludeFromCodeCoverage]
    [Route(FactSettlementsController.ControllerRoute)]
    [ApiController]
    [Authorize]
    public class FactSettlementsController : ControllerBase
    {
        #region Others

        /// <summary>
        /// The controller name
        /// </summary>
        private const String ControllerName = "settlements";

        /// <summary>
        /// The controller route
        /// </summary>
        private const String ControllerRoute = "api/facts/" + FactSettlementsController.ControllerName;

        #endregion

        private readonly IMediator Mediator;

        public FactSettlementsController(IMediator mediator) {
            this.Mediator = mediator;
        }
        
        [HttpGet]
        [Route("todayssettlement")]
        public async Task<IResult> TodaysSettlement([FromHeader] Guid estateId, [FromQuery] Int32 merchantReportingId, [FromQuery] Int32 operatorReportingId, [FromQuery] DateTime comparisonDate, CancellationToken cancellationToken){
            SettlementQueries.GetTodaysSettlementQuery query = new(estateId, merchantReportingId, operatorReportingId, comparisonDate);
            Result<Models.TodaysSettlement> result = await this.Mediator.Send(query, cancellationToken);
            
            return ResponseFactory.FromResult(result, (r) => new TodaysSettlement
            {
                ComparisonSettlementCount = r.ComparisonSettlementCount,
                ComparisonSettlementValue = r.ComparisonSettlementValue,
                ComparisonPendingSettlementCount = r.ComparisonPendingSettlementCount,
                ComparisonPendingSettlementValue = r.ComparisonPendingSettlementValue,

                TodaysSettlementCount = r.TodaysSettlementCount,
                TodaysSettlementValue = r.TodaysSettlementValue,
                TodaysPendingSettlementCount = r.TodaysPendingSettlementCount,
                TodaysPendingSettlementValue = r.TodaysPendingSettlementValue
            });
        }

        [HttpGet]
        [Route("lastsettlement")]
        public async Task<IResult> LastSettlement([FromHeader] Guid estateId,
                                                        CancellationToken cancellationToken) {
            SettlementQueries.GetLastSettlementQuery query = new(estateId);

            Result<LastSettlement> result = await this.Mediator.Send(query, cancellationToken);
            
            return ResponseFactory.FromResult(result, (r) => new LastSettlement()
            {
                SalesCount = r.SalesCount,
                FeesValue = r.FeesValue,
                SalesValue = r.SalesValue,
                SettlementDate = r.SettlementDate,
            });
        }

        [HttpGet]
        [Route("unsettledfees")]
        public async Task<IResult> GetUnsettledFees([FromHeader] Guid estateId,
                                                          [FromQuery] DateTime startDate,
                                                          [FromQuery] DateTime endDate,
                                                          [FromQuery] string? merchantIds, [FromQuery] string? operatorIds, 
                                                          [FromQuery] string? productIds,
                                                       [FromQuery] GroupByOption? groupByOption, CancellationToken cancellationToken)
        {
            List<Int32> merchantIdFilter = new List<Int32>();
            if (String.IsNullOrEmpty(merchantIds) == false)
            {
                List<String> merchantListStrings = merchantIds.Split(',').ToList();
                foreach (String merchantListString in merchantListStrings)
                {
                    merchantIdFilter.Add(Int32.Parse(merchantListString));
                }
            }

            List<Int32> operatorIdFilter = new List<Int32>();
            if (String.IsNullOrEmpty(operatorIds) == false)
            {
                List<String> operatorListStrings = operatorIds.Split(',').ToList();
                foreach (String operatorListString in operatorListStrings)
                {
                    operatorIdFilter.Add(Int32.Parse(operatorListString));
                }
            }

            List<Int32> productIdFilter = new List<Int32>();
            if (String.IsNullOrEmpty(productIds) == false)
            {
                List<String> productListStrings = productIds.Split(',').ToList();
                foreach (String productListString in productListStrings)
                {
                    productIdFilter.Add(Int32.Parse(productListString));
                }
            }

            Models.GroupByOption groupByOptionConverted = ConvertGroupByOption(groupByOption.GetValueOrDefault());
            SettlementQueries.GetUnsettledFeesQuery query = new(estateId, startDate, endDate,merchantIdFilter, operatorIdFilter, productIdFilter, groupByOptionConverted);
            Result<List<UnsettledFee>> result = await this.Mediator.Send(query, cancellationToken);

            return ResponseFactory.FromResult(result, (r) => {
                List<EstateReportingAPI.DataTransferObjects.UnsettledFee> response = new();

                foreach (UnsettledFee unsettledFee in result.Data)
                {
                    response.Add(new DataTransferObjects.UnsettledFee
                    {
                        DimensionName = unsettledFee.DimensionName,
                        FeesCount = unsettledFee.FeesCount,
                        FeesValue = unsettledFee.FeesValue
                    });
                }

                return response;
            });
        }

        private static Models.GroupByOption ConvertGroupByOption(GroupByOption groupByOption){
            return groupByOption switch{
                GroupByOption.Merchant => Models.GroupByOption.Merchant,
                GroupByOption.Product => Models.GroupByOption.Product,
                _ => Models.GroupByOption.Operator
            };
        }
    }
}
