using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;

namespace EstateReportingAPI.Controllers
{
    using BusinessLogic;
    using DataTransferObjects;
    using Microsoft.AspNetCore.Authorization;
    using LastSettlement = Models.LastSettlement;

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
        public async Task<IActionResult> TodaysSettlement([FromHeader] Guid estateId, [FromQuery] DateTime comparisonDate, CancellationToken cancellationToken){
            Models.TodaysSettlement model = await this.ReportingManager.GetTodaysSettlement(estateId, comparisonDate, cancellationToken);
            
            TodaysSettlement response = new TodaysSettlement{
                                                                ComparisonSettlementCount = model.ComparisonSettlementCount,
                                                                ComparisonSettlementValue = model.ComparisonSettlementValue,
                                                                TodaysSettlementCount = model.TodaysSettlementCount,
                                                                TodaysSettlementValue = model.TodaysSettlementValue
                                                            };

            return this.Ok(response);
        }

        [HttpGet]
        [Route("lastsettlement")]
        public async Task<IActionResult> LastSettlement([FromHeader] Guid estateId, CancellationToken cancellationToken)
        {
            LastSettlement model = await this.ReportingManager.GetLastSettlement(estateId, cancellationToken);

            LastSettlement response = new LastSettlement()
                                        {
                                            SalesCount = model.SalesCount,
                                            FeesValue = model.FeesValue,
                                            SalesValue = model.SalesValue,
                                            SettlementDate = model.SettlementDate,
                                        };

            return this.Ok(response);
        }
    }
}
