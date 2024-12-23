using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;
using EstateReportingAPI.BusinessLogic.Queries;

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
        public const String ControllerName = "settlements";

        /// <summary>
        /// The controller route
        /// </summary>
        private const String ControllerRoute = "api/facts/" + FactSettlementsController.ControllerName;

        #endregion

        private readonly IReportingManager ReportingManager;
        private readonly IMediator Mediator;

        public FactSettlementsController(IReportingManager reportingManager, IMediator mediator) {
            this.ReportingManager = reportingManager;
            this.Mediator = mediator;
        }
        
        [HttpGet]
        [Route("todayssettlement")]
        public async Task<IActionResult> TodaysSettlement([FromHeader] Guid estateId, [FromQuery] Int32 merchantReportingId, [FromQuery] Int32 operatorReportingId, [FromQuery] DateTime comparisonDate, CancellationToken cancellationToken){
            SettlementQueries.GetTodaysSettlementQuery query = new(estateId, merchantReportingId, operatorReportingId, comparisonDate);
            var result = await this.Mediator.Send(query, cancellationToken);
            if (result.IsFailed)
                return result.ToActionResultX();

            TodaysSettlement response = new TodaysSettlement{
                                                                ComparisonSettlementCount = result.Data.ComparisonSettlementCount,
                                                                ComparisonSettlementValue = result.Data.ComparisonSettlementValue,
                                                                ComparisonPendingSettlementCount = result.Data.ComparisonPendingSettlementCount,
                                                                ComparisonPendingSettlementValue = result.Data.ComparisonPendingSettlementValue,
                                                                
                                                                TodaysSettlementCount = result.Data.TodaysSettlementCount,
                                                                TodaysSettlementValue = result.Data.TodaysSettlementValue,
                                                                TodaysPendingSettlementCount = result.Data.TodaysPendingSettlementCount,
                                                                TodaysPendingSettlementValue = result.Data.TodaysPendingSettlementValue
                                                            };

            return Result.Success(response).ToActionResultX();
        }

        [HttpGet]
        [Route("lastsettlement")]
        public async Task<IActionResult> LastSettlement([FromHeader] Guid estateId,
                                                        CancellationToken cancellationToken) {
            SettlementQueries.GetLastSettlementQuery query = new(estateId);

            var result = await this.Mediator.Send(query, cancellationToken);
            if (result.IsFailed)
                return result.ToActionResultX();
            
            LastSettlement response = new LastSettlement() {
                SalesCount = result.Data.SalesCount,
                FeesValue = result.Data.FeesValue,
                SalesValue = result.Data.SalesValue,
                SettlementDate = result.Data.SettlementDate,
            };

            return Result.Success(response).ToActionResultX();
        }

        [HttpGet]
        [Route("unsettledfees")]
        public async Task<IActionResult> GetUnsettledFees([FromHeader] Guid estateId,
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
            var result = await this.Mediator.Send(query, cancellationToken);
            if (result.IsFailed)
                return result.ToActionResultX();
            
            List<EstateReportingAPI.DataTransferObjects.UnsettledFee> response = new();
            
            foreach (UnsettledFee unsettledFee in result.Data)
            {
                response.Add(new DataTransferObjects.UnsettledFee{
                                                                     DimensionName = unsettledFee.DimensionName,
                                                                     FeesCount = unsettledFee.FeesCount,
                                                                     FeesValue = unsettledFee.FeesValue
                                                                 });
            };

            return Result.Success(response).ToActionResultX();
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
