using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;

namespace EstateReportingAPI.Controllers
{
    using System.Collections.Specialized;
    using System.Globalization;
    using DataTrasferObjects;
    using EstateManagement.Database.Contexts;
    using System.Web;
    using DataTransferObjects;
    using Microsoft.IdentityModel.Protocols;
    using EstateManagement.Database.Entities;
    using System.Linq;
    using Microsoft.EntityFrameworkCore;

    [ExcludeFromCodeCoverage]
    [Route(FactTransactionsController.ControllerRoute)]
    [ApiController]
    public class FactTransactionsController : ControllerBase{
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

        private readonly Shared.EntityFramework.IDbContextFactory<EstateManagementGenericContext> ContextFactory;

        public FactTransactionsController(Shared.EntityFramework.IDbContextFactory<EstateManagementGenericContext> contextFactory){
            this.ContextFactory = contextFactory;
        }

        private const String ConnectionStringIdentifier = "EstateReportingReadModel";

        [HttpGet]
        [Route("todayssales")]
        public async Task<IActionResult> TodaysSales([FromHeader] Guid estateId, [FromQuery] DateTime comparisonDate, CancellationToken cancellationToken){
            EstateManagementGenericContext? context = await this.ContextFactory.GetContext(estateId, FactTransactionsController.ConnectionStringIdentifier, cancellationToken);

            // First we need to get a value of todays sales
            Decimal todaysSalesValue = (from t in context.Transactions
                                        where t.IsAuthorised && t.TransactionType == "Sale"
                                                             && t.TransactionDate == DateTime.Now.Date
                                                             && t.TransactionTime <= DateTime.Now.TimeOfDay
                                        select t.TransactionAmount).Sum();

            Int32 todaysSalesCount = (from t in context.Transactions
                                      where t.IsAuthorised && t.TransactionType == "Sale"
                                                           && t.TransactionDate == DateTime.Now.Date
                                                           && t.TransactionTime <= DateTime.Now.TimeOfDay
                                      select t.TransactionAmount).Count();

            Decimal comparisonSalesValue = (from t in context.Transactions
                                            where t.IsAuthorised && t.TransactionType == "Sale"
                                                                 && t.TransactionDate == comparisonDate
                                                                 && t.TransactionTime <= DateTime.Now.TimeOfDay
                                            select t.TransactionAmount).Sum();

            Int32 comparisonSalesCount = (from t in context.Transactions
                                          where t.IsAuthorised && t.TransactionType == "Sale"
                                                               && t.TransactionDate == comparisonDate
                                                               && t.TransactionTime <= DateTime.Now.TimeOfDay
                                          select t.TransactionAmount).Count();

            var response = new TodaysSales{
                                              ComparisonSalesCount = comparisonSalesCount,
                                              ComparisonSalesValue = comparisonSalesValue,
                                              TodaysSalesCount = todaysSalesCount,
                                              TodaysSalesValue = todaysSalesValue

                                          };

            return this.Ok(response);
        }

        [HttpGet]
        [Route("todayssales/countbyhour")]
        public async Task<IActionResult> TodaysSalesCountByHour([FromHeader] Guid estateId, [FromQuery] DateTime comparisonDate, CancellationToken cancellationToken){
            EstateManagementGenericContext? context = await this.ContextFactory.GetContext(estateId, FactTransactionsController.ConnectionStringIdentifier, cancellationToken);

            // First we need to get a value of todays sales
            var todaysSalesByHour = (from t in context.Transactions
                                     where t.IsAuthorised && t.TransactionType == "Sale"
                                                          && t.TransactionDate == DateTime.Now.Date
                                                          && t.TransactionTime <= DateTime.Now.TimeOfDay
                                     group t.TransactionAmount by t.TransactionTime.Hours
                                     into g
                                     select new{
                                                   Hour = g.Key,
                                                   TotalSalesCount = g.Count()
                                               }).ToList();

            var comparisonSalesByHour = (from t in context.Transactions
                                         where t.IsAuthorised && t.TransactionType == "Sale"
                                                              && t.TransactionDate == comparisonDate
                                                              && t.TransactionTime <= DateTime.Now.TimeOfDay
                                         group t.TransactionAmount by t.TransactionTime.Hours
                                         into g
                                         select new{
                                                       Hour = g.Key,
                                                       TotalSalesCount = g.Count()
                                                   }).ToList();

            var response = (from today in todaysSalesByHour
                            join comparison in comparisonSalesByHour
                                on today.Hour equals comparison.Hour
                            select new TodaysSalesCountByHour{
                                                                 Hour = today.Hour,
                                                                 TodaysSalesCount = today.TotalSalesCount,
                                                                 ComparisonSalesCount = comparison.TotalSalesCount
                                                             }).ToList();



            return this.Ok(response);
        }

        [HttpGet]
        [Route("todayssales/valuebyhour")]
        public async Task<IActionResult> TodaysSalesValueByHour([FromHeader] Guid estateId, [FromQuery] DateTime comparisonDate, CancellationToken cancellationToken){
            EstateManagementGenericContext? context = await this.ContextFactory.GetContext(estateId, FactTransactionsController.ConnectionStringIdentifier, cancellationToken);

            // First we need to get a value of todays sales
            var todaysSalesByHour = (from t in context.Transactions
                                     where t.IsAuthorised && t.TransactionType == "Sale"
                                                          && t.TransactionDate == DateTime.Now.Date
                                                          && t.TransactionTime <= DateTime.Now.TimeOfDay
                                     group t.TransactionAmount by t.TransactionTime.Hours
                                     into g
                                     select new{
                                                   Hour = g.Key,
                                                   TotalSalesValue = g.Sum()
                                               }).ToList();

            var comparisonSalesByHour = (from t in context.Transactions
                                         where t.IsAuthorised && t.TransactionType == "Sale"
                                                              && t.TransactionDate == comparisonDate
                                                              && t.TransactionTime <= DateTime.Now.TimeOfDay
                                         group t.TransactionAmount by t.TransactionTime.Hours
                                         into g
                                         select new{
                                                       Hour = g.Key,
                                                       TotalSalesValue = g.Sum()
                                                   }).ToList();

            var response = (from today in todaysSalesByHour
                            join comparison in comparisonSalesByHour
                                on today.Hour equals comparison.Hour
                            select new TodaysSalesValueByHour{
                                                                 Hour = today.Hour,
                                                                 TodaysSalesValue = today.TotalSalesValue,
                                                                 ComparisonSalesValue = comparison.TotalSalesValue
                                                             }).ToList();

            return this.Ok(response);
        }

        [HttpGet]
        [Route("merchantkpis")]
        public async Task<IActionResult> GetMerchantsTransactionKpis([FromHeader] Guid estateId, CancellationToken cancellationToken){
            EstateManagementGenericContext? context = await this.ContextFactory.GetContext(estateId, FactTransactionsController.ConnectionStringIdentifier, cancellationToken);


            var merchantsWithSaleInLastHour = (from m in context.Merchants
                                               where m.LastSaleDate == DateTime.Now.Date
                                                     && m.LastSaleDateTime >= DateTime.Now.AddHours(-1)
                                               select m.MerchantReportingId).Count();

            var merchantsWithNoSaleToday = (from m in context.Merchants
                                            where m.LastSaleDate == DateTime.Now.Date.AddDays(-1)
                                            select m.MerchantReportingId).Count();

            var merchantsWithNoSaleInLast7Days = (from m in context.Merchants
                                                  where m.LastSaleDate <= DateTime.Now.Date.AddDays(-7)
                                                  select m.MerchantReportingId).Count();

            var response = new MerchantKpi{
                                              MerchantsWithSaleInLastHour = merchantsWithSaleInLastHour,
                                              MerchantsWithNoSaleToday = merchantsWithNoSaleToday,
                                              MerchantsWithNoSaleInLast7Days = merchantsWithNoSaleInLast7Days
                                          };

            return this.Ok(response);
        }

        [HttpGet]
        [Route("todaysfailedsales")]
        public async Task<IActionResult> TodaysFailedSales([FromHeader] Guid estateId, [FromQuery] DateTime comparisonDate, [FromQuery] String responseCode, CancellationToken cancellationToken){
            EstateManagementGenericContext? context = await this.ContextFactory.GetContext(estateId, FactTransactionsController.ConnectionStringIdentifier, cancellationToken);

            // First we need to get a value of todays sales
            Decimal todaysSalesValue = (from t in context.Transactions
                                        where t.IsAuthorised == false && t.TransactionType == "Sale"
                                                                      && t.TransactionDate == DateTime.Now.Date
                                                                      && t.TransactionTime <= DateTime.Now.TimeOfDay
                                                                      && t.ResponseCode == responseCode
                                        select t.TransactionAmount).Sum();

            Int32 todaysSalesCount = (from t in context.Transactions
                                      where t.IsAuthorised == false && t.TransactionType == "Sale"
                                                                    && t.TransactionDate == DateTime.Now.Date
                                                                    && t.TransactionTime <= DateTime.Now.TimeOfDay
                                                                    && t.ResponseCode == responseCode
                                      select t.TransactionAmount).Count();

            Decimal comparisonSalesValue = (from t in context.Transactions
                                            where t.IsAuthorised == false && t.TransactionType == "Sale"
                                                                          && t.TransactionDate == comparisonDate
                                                                          && t.TransactionTime <= DateTime.Now.TimeOfDay
                                                                          && t.ResponseCode == responseCode
                                            select t.TransactionAmount).Sum();

            Int32 comparisonSalesCount = (from t in context.Transactions
                                          where t.IsAuthorised == false && t.TransactionType == "Sale"
                                                                        && t.TransactionDate == comparisonDate
                                                                        && t.TransactionTime <= DateTime.Now.TimeOfDay
                                                                        && t.ResponseCode == responseCode
                                          select t.TransactionAmount).Count();

            TodaysSales response = new TodaysSales{
                                                      ComparisonSalesCount = comparisonSalesCount,
                                                      ComparisonSalesValue = comparisonSalesValue,
                                                      TodaysSalesCount = todaysSalesCount,
                                                      TodaysSalesValue = todaysSalesValue

                                                  };

            return this.Ok(response);
        }

        [HttpGet]
        [Route("products/topbottombyvalue")]
        public async Task<IActionResult> GetTopBottomProductsByValue([FromHeader] Guid estateId, [FromQuery] TopBottom topOrBottom, [FromQuery] Int32 count, CancellationToken cancellationToken){
            
            List<TopBottomData> topbottomData = await this.GetTopBottomData(estateId, topOrBottom, count, Dimension.Product, cancellationToken);

            List<TopBottomProductData> response = new List<TopBottomProductData>();
            topbottomData.ForEach(t => {
                                      response.Add(new TopBottomProductData{
                                                                               ProductName = t.DimensionName,
                                                                               SalesValue = t.SalesValue
                                                                           });});
            return this.Ok(response);
        }

        [HttpGet]
        [Route("merchants/topbottombyvalue")]
        public async Task<IActionResult> GetTopBottomMerchantsByValue([FromHeader] Guid estateId, [FromQuery] TopBottom topOrBottom, [FromQuery] Int32 count, CancellationToken cancellationToken)
        {
            List<TopBottomData> topbottomData = await this.GetTopBottomData(estateId, topOrBottom, count, Dimension.Merchant, cancellationToken);

            List<TopBottomMerchantData> response = new List<TopBottomMerchantData>();
            topbottomData.ForEach(t => {
                                      response.Add(new TopBottomMerchantData
                                      {
                                                       MerchantName = t.DimensionName,
                                                       SalesValue = t.SalesValue
                                                   });
                                  });
            return this.Ok(response);
        }

        [HttpGet]
        [Route("operators/topbottombyvalue")]
        public async Task<IActionResult> GetTopBottomOperatorsByValue([FromHeader] Guid estateId, [FromQuery] TopBottom topOrBottom, [FromQuery] Int32 count, CancellationToken cancellationToken)
        {
            List<TopBottomData> topbottomData = await this.GetTopBottomData(estateId, topOrBottom, count, Dimension.Operator, cancellationToken);

            List<TopBottomOperatorData> response = new List<TopBottomOperatorData>();
            topbottomData.ForEach(t => {
                                      response.Add(new TopBottomOperatorData
                                      {
                                                       OperatorName = t.DimensionName,
                                                       SalesValue = t.SalesValue
                                                   });
                                  });
            return this.Ok(response);
        }

        private async Task<List<TopBottomData>> GetTopBottomData(Guid estateId, TopBottom direction, Int32 resultCount, Dimension dimension, CancellationToken cancellationToken){

            EstateManagementGenericContext? context = await this.ContextFactory.GetContext(estateId, FactTransactionsController.ConnectionStringIdentifier, cancellationToken);

            IQueryable<Transaction> mainQuery = context.Transactions
                                                       .Where(joined => joined.IsAuthorised == true
                                                                        && joined.TransactionType == "Sale"
                                                                        && joined.TransactionDate == new DateTime(2023, 09, 14));
            IQueryable<TopBottomData> queryable = null;
            if (dimension == Dimension.Product)
            {
                // Products
                queryable = mainQuery
                            .Join(context.ContractProducts,
                                  t => t.ContractProductReportingId,
                                  contractProduct => contractProduct.ContractProductReportingId,
                                  (t, contractProduct) => new{
                                                                 Transaction = t,
                                                                 ContractProduct = contractProduct
                                                             })
                            .GroupBy(joined => joined.ContractProduct.ProductName)
                            .Select(g => new TopBottomData
                                         {
                                             DimensionName = g.Key,
                                             SalesValue = g.Sum(t => t.Transaction.TransactionAmount)
                                                 });
            }
            else if (dimension == Dimension.Operator)
            {
                // Operators
                queryable = mainQuery
                            .Join(context.MerchantOperators,
                                  t => t.OperatorIdentifier,
                                  oper => oper.Name,
                                  (t, oper) => new {
                                                                  Transaction = t,
                                                                  Operator = oper
                                                              })
                            .GroupBy(joined => joined.Operator.Name)
                            .Select(g => new TopBottomData
                            {
                                DimensionName = g.Key,
                                SalesValue = g.Sum(t => t.Transaction.TransactionAmount)
                                         });
            }
            else if (dimension == Dimension.Merchant)
            {
                // Operators
                queryable = mainQuery
                            .Join(context.Merchants,
                                  t => t.MerchantReportingId,
                                  merchant => merchant.MerchantReportingId,
                                  (t, merchant) => new {
                                                           Transaction = t,
                                                           Merchant = merchant
                                                       })
                            .GroupBy(joined => joined.Merchant.Name)
                            .Select(g => new TopBottomData
                            {
                                DimensionName = g.Key,
                                SalesValue = g.Sum(t => t.Transaction.TransactionAmount)
                                         });
            }

            if (direction == TopBottom.Top){
                // Top X
                queryable = queryable.OrderByDescending(g => g.SalesValue);
            }
            else if (direction == TopBottom.Bottom)
            {
                // Bottom X
                queryable = queryable.OrderBy(g => g.SalesValue);
            }
            else{
                // TODO: bad request??
            }

            return await queryable.Take(resultCount).ToListAsync(cancellationToken);
        }
    }

    

    public class TopBottomData
    {
        public String DimensionName{ get; set; }
        public Decimal SalesValue { get; set; }
    }

    public enum TopBottom{
        Top = 0,
        Bottom = 1
    }

    public enum Dimension
    {
        Product = 0,
        Operator = 1,
        Merchant = 2
    }
}
