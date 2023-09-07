using EstateManagement.Database.Contexts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;

namespace EstateReportingAPI.Controllers
{
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

        private readonly IDbContextFactory<EstateManagementGenericContext> ContextFactory;

        public FactSettlementsController(IDbContextFactory<EstateManagementGenericContext> contextFactory)
        {
            this.ContextFactory = contextFactory;
        }

        private const String ConnectionStringIdentifier = "EstateReportingReadModel";

        [HttpGet]
        [Route("todayssettlement")]
        public async Task<IActionResult> TodaysSettlement([FromHeader] Guid estateId, [FromQuery] DateTime comparisonDate, CancellationToken cancellationToken)
        {
            EstateManagementGenericContext? context = await this.ContextFactory.GetContext(estateId, FactSettlementsController.ConnectionStringIdentifier, cancellationToken);

            // First we need to get a value of todays sales
            Decimal todaysSettlementValue = (from s in context.Settlements
                                             join f in context.MerchantSettlementFees on s.SettlementReportingId equals f.SettlementReportingId
                                             where f.IsSettled && s.SettlementDate == DateTime.Now.Date
                                             select f.CalculatedValue).Sum();

            Int32 todaysSettlementCount = (from s in context.Settlements
                                           join f in context.MerchantSettlementFees on s.SettlementReportingId equals f.SettlementReportingId
                                           where f.IsSettled && s.SettlementDate == DateTime.Now.Date
                                           select f.CalculatedValue).Count();

            Decimal comparisonSettlementValue = (from s in context.Settlements
                                            join f in context.MerchantSettlementFees on s.SettlementReportingId equals f.SettlementReportingId
                                            where f.IsSettled && s.SettlementDate == comparisonDate
                                            select f.CalculatedValue).Sum();

            Int32 comparisonSettlementCount = (from s in context.Settlements
                                               join f in context.MerchantSettlementFees on s.SettlementReportingId equals f.SettlementReportingId
                                               where f.IsSettled && s.SettlementDate == comparisonDate
                                               select f.CalculatedValue).Count();
            
            var response = new TodaysSettlement{
                                                   ComparisonSettlementCount = comparisonSettlementCount,
                                                   ComparisonSettlementValue = comparisonSettlementValue,
                                                   TodaysSettlementCount = todaysSettlementCount,
                                                   TodaysSettlementValue = todaysSettlementValue
                                               };

            return this.Ok(response);
        }
    }
}
