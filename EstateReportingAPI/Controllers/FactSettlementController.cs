using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;

namespace EstateReportingAPI.Controllers
{
    using BusinessLogic;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc.Formatters;
    using Models;
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

        public FactSettlementsController(IReportingManager reportingManager){
            this.ReportingManager = reportingManager;
        }
        
        [HttpGet]
        [Route("todayssettlement")]
        public async Task<IActionResult> TodaysSettlement([FromHeader] Guid estateId, [FromQuery] Int32 merchantReportingId, [FromQuery] Int32 operatorReportingId, [FromQuery] DateTime comparisonDate, CancellationToken cancellationToken){
            Models.TodaysSettlement model = await this.ReportingManager.GetTodaysSettlement(estateId, merchantReportingId, operatorReportingId, comparisonDate, cancellationToken);
            
            TodaysSettlement response = new TodaysSettlement{
                                                                ComparisonSettlementCount = model.ComparisonSettlementCount,
                                                                ComparisonSettlementValue = model.ComparisonSettlementValue,
                                                                ComparisonPendingSettlementCount = model.ComparisonPendingSettlementCount,
                                                                ComparisonPendingSettlementValue = model.ComparisonPendingSettlementValue,
                                                                
                                                                TodaysSettlementCount = model.TodaysSettlementCount,
                                                                TodaysSettlementValue = model.TodaysSettlementValue,
                                                                TodaysPendingSettlementCount = model.TodaysPendingSettlementCount,
                                                                TodaysPendingSettlementValue = model.TodaysPendingSettlementValue
                                                            };

            return this.Ok(response);
        }

        [HttpGet]
        [Route("lastsettlement")]
        public async Task<IActionResult> LastSettlement([FromHeader] Guid estateId,
                                                        CancellationToken cancellationToken) {
            LastSettlement model = await this.ReportingManager.GetLastSettlement(estateId, cancellationToken);

            if (model == null) {
                return this.Ok(new LastSettlement());
            }

            LastSettlement response = new LastSettlement() {
                SalesCount = model.SalesCount,
                FeesValue = model.FeesValue,
                SalesValue = model.SalesValue,
                SettlementDate = model.SettlementDate,
            };

            return this.Ok(response);
        }

        [HttpGet]
        [Route("unsettledfees")]
        public async Task<IActionResult> GetUnsettledFees([FromHeader] Guid estateId,
                                                          [FromQuery] DateTime startDate,
                                                          [FromQuery] DateTime endDate,
                                                          [FromQuery] string? merchantReportingIds, [FromQuery] string? operatorReportingIds, 
                                                          [FromQuery] string? productReportingIds,
                                                       [FromQuery] GroupByOption? groupByOption, CancellationToken cancellationToken)
        {
            List<Int32> merchantIdFilter = new List<Int32>();
            if (String.IsNullOrEmpty(merchantReportingIds) == false)
            {
                List<String> merchantListStrings = merchantReportingIds.Split(',').ToList();
                foreach (String merchantListString in merchantListStrings)
                {
                    merchantIdFilter.Add(Int32.Parse(merchantListString));
                }
            }

            List<Int32> operatorIdFilter = new List<Int32>();
            if (String.IsNullOrEmpty(operatorReportingIds) == false)
            {
                List<String> operatorListStrings = operatorReportingIds.Split(',').ToList();
                foreach (String operatorListString in operatorListStrings)
                {
                    operatorIdFilter.Add(Int32.Parse(operatorListString));
                }
            }

            List<Int32> productIdFilter = new List<Int32>();
            if (String.IsNullOrEmpty(productReportingIds) == false)
            {
                List<String> productListStrings = productReportingIds.Split(',').ToList();
                foreach (String productListString in productListStrings)
                {
                    productIdFilter.Add(Int32.Parse(productListString));
                }
            }

            Models.GroupByOption groupByOptionConverted = ConvertGroupByOption(groupByOption.GetValueOrDefault());
            List<UnsettledFee> model = await this.ReportingManager.GetUnsettledFees(estateId,  startDate, endDate, merchantIdFilter,
                                                                                    operatorIdFilter, productIdFilter, groupByOptionConverted, cancellationToken);

            List<EstateReportingAPI.DataTransferObjects.UnsettledFee> response = new();
            
            foreach (UnsettledFee unsettledFee in model){
                response.Add(new DataTransferObjects.UnsettledFee{
                                                                     DimensionName = unsettledFee.DimensionName,
                                                                     FeesCount = unsettledFee.FeesCount,
                                                                     FeesValue = unsettledFee.FeesValue
                                                                 });
            };

            return this.Ok(response);
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
