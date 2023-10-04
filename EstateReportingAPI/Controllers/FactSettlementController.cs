using EstateManagement.Database.Contexts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;

namespace EstateReportingAPI.Controllers
{
    using BusinessLogic;
    using DataTransferObjects;
    using Shared.EntityFramework;

    [ExcludeFromCodeCoverage]
    [Route(FactSettlementsController.ControllerRoute)]
    [ApiController]
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
    }
}
