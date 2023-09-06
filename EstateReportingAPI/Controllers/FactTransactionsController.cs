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

            // First we need to get a value of todays sales
            Decimal todaysSales = (from t in context.Transactions
                                   where t.IsAuthorised && t.TransactionType == "Sale"
                                                        && t.TransactionDate == DateTime.Now.Date
                                                        && t.TransactionTime <= DateTime.Now.TimeOfDay
                                   select t.TransactionAmount).Sum();

            Int32 todaysSalesCount = (from t in context.Transactions
                                      where t.IsAuthorised && t.TransactionType == "Sale"
                                                           && t.TransactionDate == DateTime.Now.Date
                                                           && t.TransactionTime <= DateTime.Now.TimeOfDay
                                      select t.TransactionAmount).Count();

            Decimal comparisonSales = (from t in context.Transactions
                                       where t.IsAuthorised && t.TransactionType == "Sale"
                                                            && t.TransactionDate == comparisonDate
                                                            && t.TransactionTime <= DateTime.Now.TimeOfDay
                                       select t.TransactionAmount).Sum();

            Int32 comparisonSalesCount = (from t in context.Transactions
                                          where t.IsAuthorised && t.TransactionType == "Sale"
                                                               && t.TransactionDate == comparisonDate
                                                               && t.TransactionTime <= DateTime.Now.TimeOfDay
                                          select t.TransactionAmount).Count();

            var response = new
                           {
                               TodaysSalesValue = todaysSales,
                               TodaysSalesCount = todaysSalesCount,
                               ComparisonSales = comparisonSales,
                               ComparisonSalesCount = comparisonSalesCount
                           };

            return this.Ok(response);
        }

        [HttpGet]
        [Route("todayssales/countbyhour")]
        public async Task<IActionResult> TodaysSalesCountByHour([FromHeader] Guid estateId, [FromQuery] DateTime comparisonDate, CancellationToken cancellationToken)
        {
            EstateManagementGenericContext? context = await this.ContextFactory.GetContext(estateId, FactTransactionsController.ConnectionStringIdentifier, cancellationToken);

            // First we need to get a value of todays sales
            var todaysSalesByHour = (from t in context.Transactions
                                     where t.IsAuthorised && t.TransactionType == "Sale"
                                                    && t.TransactionDate == DateTime.Now.Date
                                                    && t.TransactionTime <= DateTime.Now.TimeOfDay
                               group t.TransactionAmount by t.TransactionTime.Hours into g
                               select new
                                      {
                                          Hour = g.Key,
                                          TotalSalesCount = g.Count()
                                      }).ToList();

            var comparisonSalesByHour = (from t in context.Transactions
                                         where t.IsAuthorised && t.TransactionType == "Sale"
                                                              && t.TransactionDate == comparisonDate
                                                              && t.TransactionTime <= DateTime.Now.TimeOfDay
                                         group t.TransactionAmount by t.TransactionTime.Hours into g
                                         select new
                                                {
                                                    Hour = g.Key,
                                                    TotalSalesCount = g.Count()
                                                }).ToList();

            var response = (from today in todaysSalesByHour
                            join comparison in comparisonSalesByHour
                                on today.Hour equals comparison.Hour
                            select new
                                   {
                                       Hour = today.Hour,
                                       TodaysSalesCount = today.TotalSalesCount,
                                       ComparisonSalesCount = comparison.TotalSalesCount
                            }).ToList();
            
            return this.Ok(response);
        }

        [HttpGet]
        [Route("todayssales/valuebyhour")]
        public async Task<IActionResult> TodaysSalesValueByHour([FromHeader] Guid estateId, [FromQuery] DateTime comparisonDate, CancellationToken cancellationToken)
        {
            EstateManagementGenericContext? context = await this.ContextFactory.GetContext(estateId, FactTransactionsController.ConnectionStringIdentifier, cancellationToken);

            // First we need to get a value of todays sales
            var todaysSalesByHour = (from t in context.Transactions
                                     where t.IsAuthorised && t.TransactionType == "Sale"
                                                    && t.TransactionDate == DateTime.Now.Date
                                                    && t.TransactionTime <= DateTime.Now.TimeOfDay
                                     group t.TransactionAmount by t.TransactionTime.Hours into g
                                     select new
                                     {
                                         Hour = g.Key,
                                         TotalSalesValue = g.Sum()
                                     }).ToList();

            var comparisonSalesByHour = (from t in context.Transactions
                                         where t.IsAuthorised && t.TransactionType == "Sale"
                                                              && t.TransactionDate == comparisonDate
                                                              && t.TransactionTime <= DateTime.Now.TimeOfDay
                                         group t.TransactionAmount by t.TransactionTime.Hours into g
                                         select new
                                         {
                                             Hour = g.Key,
                                             TotalSalesValue = g.Sum()
                                         }).ToList();

            var response = (from today in todaysSalesByHour
                            join comparison in comparisonSalesByHour
                                on today.Hour equals comparison.Hour
                            select new
                            {
                                Hour = today.Hour,
                                TodaysSalesValue = today.TotalSalesValue,
                                ComparisonSalesValue = comparison.TotalSalesValue
                            }).ToList();

            return this.Ok(response);
        }
    }
}
