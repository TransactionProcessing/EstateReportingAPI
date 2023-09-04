using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;

namespace EstateReportingAPI.Controllers
{
    using System.Collections.Specialized;
    using DataTrasferObjects;
    using EstateManagement.Database.Contexts;
    using System.Web;
    using Microsoft.IdentityModel.Protocols;
    using Shared.EntityFramework;

    [ExcludeFromCodeCoverage]
    [Route(FactTransactionsController.ControllerRoute)]
    [ApiController]
    public class FactTransactionsController : ControllerBase
    {
        #region Others

        /// <summary>
        /// The controller name
        /// </summary>
        public const String ControllerName = "transactions";

        /// <summary>
        /// The controller route
        /// </summary>
        private const String ControllerRoute = "api/facts/" + FactTransactionsController.ControllerName;

        #endregion

        private readonly IDbContextFactory<EstateManagementGenericContext> ContextFactory;

        public FactTransactionsController(IDbContextFactory<EstateManagementGenericContext> contextFactory){
            this.ContextFactory = contextFactory;
        }

        private const String ConnectionStringIdentifier = "EstateReportingReadModel";

        [HttpGet]
        [Route("todayssales")]
        public async Task<IActionResult> TodaysSales([FromHeader] Guid estateId, [FromQuery] DateTime comparisonDate, CancellationToken cancellationToken)
        {
            EstateManagementGenericContext? context = await this.ContextFactory.GetContext(estateId, FactTransactionsController.ConnectionStringIdentifier, cancellationToken);
            
            // TODO: this code will be refactored once there is a decimal value in either the Transactions table or the TransactionsAdditionalRequestData
            // https://github.com/TransactionProcessing/EstateManagement/issues/403
            // First we need to get a value of todays sales
            List<String> todaysSales = (from t in context.Transactions
                                        join r in context.TransactionsAdditionalRequestData on t.TransactionReportingId equals r.TransactionReportingId
                                        where t.IsAuthorised && t.TransactionType == "Sale"
                                                             && t.TransactionDate == DateTime.Now.Date
                                                             && t.TransactionTime <= DateTime.Now.TimeOfDay
                                                             && r.Amount != null
                                        select r.Amount).ToList();

            List<String> comparisonSales = (from t in context.Transactions
                                            join r in context.TransactionsAdditionalRequestData on t.TransactionReportingId equals r.TransactionReportingId
                                            where t.IsAuthorised && t.TransactionType == "Sale"
                                                                 && t.TransactionDate == comparisonDate
                                                                 && t.TransactionTime <= DateTime.Now.TimeOfDay
                                                                 && r.Amount != null
                                            select r.Amount).ToList();

            var response = new{
                                  TodaysSalesValue = todaysSales.Sum(Decimal.Parse),
                                  TodaysSalesCount = todaysSales.Count,
                                  ComparisonSales = comparisonSales.Sum(Decimal.Parse),
                                  ComparisonSalesCount = comparisonSales.Count()
            };

            return this.Ok(response);
        }
    }
}
