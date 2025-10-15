using TransactionProcessor.Database.Contexts;
using TransactionProcessor.Database.Entities;
using TransactionProcessor.Database.Entities.Summary;

namespace EstateReportingAPI.BusinessLogic{
    using Microsoft.EntityFrameworkCore;
    using Models;
    using Shared.EntityFramework;
    using System;
    using System.Linq;
    using System.Threading;
    using Calendar = Models.Calendar;
    using Merchant = Models.Merchant;
    using Operator = Models.Operator;


    public partial class ReportingManager : IReportingManager{
        private readonly IDbContextResolver<EstateManagementContext> Resolver;

        
        private Guid Id;
        private static readonly String EstateManagementDatabaseName = "TransactionProcessorReadModel";
        #region Constructors

        public ReportingManager(IDbContextResolver<EstateManagementContext> resolver) {
            this.Resolver = resolver;
        }

        #endregion

        #region Methods

        public async Task<List<Calendar>> GetCalendarComparisonDates(Guid estateId, CancellationToken cancellationToken){
            using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, estateId.ToString());
            await using EstateManagementContext context = resolvedContext.Context;

            DateTime startOfYear = new DateTime(DateTime.Now.Year, 1, 1);

            List<TransactionProcessor.Database.Entities.Calendar> entities = context.Calendar.Where(c => c.Date >= startOfYear && c.Date < DateTime.Now.Date.AddDays(-1)).OrderByDescending(d => d.Date).ToList();

            List<Calendar> result = new List<Calendar>();
            foreach (TransactionProcessor.Database.Entities.Calendar calendar in entities){
                result.Add(new Calendar{
                                           Date = calendar.Date,
                                           DayOfWeek = calendar.DayOfWeek,
                                           Year = calendar.Year,
                                           DayOfWeekNumber = calendar.DayOfWeekNumber,
                                           DayOfWeekShort = calendar.DayOfWeekShort,
                                           MonthNameLong = calendar.MonthNameLong,
                                           MonthNameShort = calendar.MonthNameShort,
                                           MonthNumber = calendar.MonthNumber,
                                           WeekNumber = calendar.WeekNumber,
                                           WeekNumberString = calendar.WeekNumberString,
                                           YearWeekNumber = calendar.YearWeekNumber,
                                       });
            }

            return result;
        }

        public async Task<List<Calendar>> GetCalendarDates(Guid estateId, CancellationToken cancellationToken){
            using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, estateId.ToString());
            await using EstateManagementContext context = resolvedContext.Context;

            List<TransactionProcessor.Database.Entities.Calendar> entities = context.Calendar.Where(c => c.Date <= DateTime.Now.Date).ToList();

            List<Calendar> result = new List<Calendar>();
            foreach (TransactionProcessor.Database.Entities.Calendar calendar in entities){
                result.Add(new Calendar{
                                           Date = calendar.Date,
                                           DayOfWeek = calendar.DayOfWeek,
                                           Year = calendar.Year,
                                           DayOfWeekNumber = calendar.DayOfWeekNumber,
                                           DayOfWeekShort = calendar.DayOfWeekShort,
                                           MonthNameLong = calendar.MonthNameLong,
                                           MonthNameShort = calendar.MonthNameShort,
                                           MonthNumber = calendar.MonthNumber,
                                           WeekNumber = calendar.WeekNumber,
                                           WeekNumberString = calendar.WeekNumberString,
                                           YearWeekNumber = calendar.YearWeekNumber,
                                       });
            }

            return result;
        }

        public async Task<List<Int32>> GetCalendarYears(Guid estateId, CancellationToken cancellationToken){
            using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, estateId.ToString());
            await using EstateManagementContext context = resolvedContext.Context;

            List<Int32> years = context.Calendar.Where(c => c.Date <= DateTime.Now.Date).GroupBy(c => c.Year).Select(y => y.Key).ToList();

            return years;
        }

        public async Task<LastSettlement> GetLastSettlement(Guid estateId, CancellationToken cancellationToken){
            using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, estateId.ToString());
            await using EstateManagementContext context = resolvedContext.Context;

            DateTime settlementDate = await context.SettlementSummary.Where(s => s.IsCompleted).OrderByDescending(s => s.SettlementDate).Select(s => s.SettlementDate).FirstOrDefaultAsync(cancellationToken);

            IQueryable<LastSettlement> settlements = from settlement in context.SettlementSummary
                                                     where settlement.SettlementDate == settlementDate
                                                     group new
                                                     {
                                                         settlement.SettlementDate,
                                                         FeeValue = settlement.FeeValue.GetValueOrDefault(),
                                                         SalesValue = settlement.SalesValue.GetValueOrDefault(),
                                                         SalesCount = settlement.SalesCount.GetValueOrDefault(),
                                                     } by settlement.SettlementDate into grouped
                              select new LastSettlement
                              {
                                  FeesValue = grouped.Sum(g => g.FeeValue),
                                  SalesCount = grouped.Sum(g => g.SalesCount),
                                  SalesValue = grouped.Sum(g => g.SalesValue),
                                  SettlementDate = grouped.Key
                              };

            return await settlements.SingleOrDefaultAsync(cancellationToken);
        }

        public async Task<MerchantKpi> GetMerchantsTransactionKpis(Guid estateId, CancellationToken cancellationToken){
            using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, estateId.ToString());
            await using EstateManagementContext context = resolvedContext.Context;

            Int32 merchantsWithSaleInLastHour = (from m in context.Merchants
                                                 where m.LastSaleDate == DateTime.Now.Date
                                                       && m.LastSaleDateTime >= DateTime.Now.AddHours(-1)
                                                 select m.MerchantReportingId).Count();

            Int32 merchantsWithNoSaleToday = (from m in context.Merchants
                                              where m.LastSaleDate == DateTime.Now.Date.AddDays(-1)
                                              select m.MerchantReportingId).Count();

            Int32 merchantsWithNoSaleInLast7Days = (from m in context.Merchants
                                                    where m.LastSaleDate <= DateTime.Now.Date.AddDays(-7)
                                                    select m.MerchantReportingId).Count();

            MerchantKpi response = new MerchantKpi{
                                                      MerchantsWithSaleInLastHour = merchantsWithSaleInLastHour,
                                                      MerchantsWithNoSaleToday = merchantsWithNoSaleToday,
                                                      MerchantsWithNoSaleInLast7Days = merchantsWithNoSaleInLast7Days
                                                  };

            return response;
        }

        public async Task<List<Merchant>> GetMerchantsByLastSale(Guid estateId, DateTime startDateTime, DateTime endDateTime, CancellationToken cancellationToken){
            using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, estateId.ToString());
            await using EstateManagementContext context = resolvedContext.Context;

            List<Merchant> response = new();

            var merchants = await context.Merchants
                                   .Where(m => m.LastSaleDateTime >= startDateTime && m.LastSaleDateTime <= endDateTime)
                                                    .Select(m => new
                                                    {
                                                        MerchantReportingId = m.MerchantReportingId,
                                                        EstateReportingId = context.Estates.Single(e => e.EstateId == m.EstateId).EstateReportingId,
                                                        Name = m.Name,
                                                        LastSaleDateTime = m.LastSaleDateTime,
                                                        LastSale = m.LastSaleDate,
                                                        CreatedDateTime = m.CreatedDateTime,
                                                        LastStatement = m.LastStatementGenerated,
                                                        MerchantId = m.MerchantId,
                                                        Reference = m.Reference,
                                                        AddressInfo = context.MerchantAddresses
                                                                                          .Where(ma => ma.MerchantId == m.MerchantId)
                                                                                          .OrderByDescending(ma => ma.CreatedDateTime)
                                                                                          .Select(ma => new
                                                                                          {
                                                                                              PostCode = ma.PostalCode,
                                                                                              Region = ma.Region,
                                                                                              Town = ma.Town,
                                                                                              // Add more properties as needed
                                                                                          })
                                                                                          .FirstOrDefault() // Get the first matching MerchantAddress or null
                                                    }).ToListAsync(cancellationToken);

            merchants.ForEach(m => response.Add(new Merchant
            {
                LastSaleDateTime = m.LastSaleDateTime,
                CreatedDateTime = m.CreatedDateTime,
                EstateReportingId = m.EstateReportingId,
                LastSale = m.LastSale,
                LastStatement = m.LastStatement,
                MerchantId = m.MerchantId,
                MerchantReportingId = m.MerchantReportingId,
                Name = m.Name,
                Reference = m.Reference,
                PostCode = m.AddressInfo?.PostCode,
                Region = m.AddressInfo?.Region,
                Town = m.AddressInfo?.Town
            }));

            return response;
        }

        public async Task<List<ResponseCode>> GetResponseCodes(Guid estateId, CancellationToken cancellationToken){
            using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, estateId.ToString());
            await using EstateManagementContext context = resolvedContext.Context;
            List<ResponseCode> response = new List<ResponseCode>();
            
            List<ResponseCodes> responseCodes = await context.ResponseCodes.ToListAsync(cancellationToken);

            responseCodes.ForEach(r => response.Add(new ResponseCode{
                                                                        Code = r.ResponseCode,
                                                                        Description = r.Description
                                                                    }));

            return response;
        }

        public async Task<TodaysSales> GetTodaysFailedSales(Guid estateId, DateTime comparisonDate, String responseCode, CancellationToken cancellationToken){
            using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, estateId.ToString());
            await using EstateManagementContext context = resolvedContext.Context;

            List<decimal> todaysSales = await (from t in context.TodayTransactions
                                        where t.IsAuthorised == false 
                                              && t.TransactionType == "Sale"
                                              && t.ResponseCode == responseCode
                                        select t.TransactionAmount).ToListAsync(cancellationToken);

            List<decimal> comparisonSales = await (from t in context.TransactionHistory
                where t.IsAuthorised == false && t.TransactionType == "Sale"
                                                                              && t.TransactionDate == comparisonDate
                                                                              && t.TransactionTime <= DateTime.Now.TimeOfDay
                                                                              && t.ResponseCode == responseCode
                                     select t.TransactionAmount).ToListAsync(cancellationToken);

            TodaysSales response = new TodaysSales{
                                                      ComparisonSalesCount = comparisonSales.Count,
                                                      ComparisonSalesValue = comparisonSales.Sum(),
                                                      ComparisonAverageSalesValue = SafeDivide(comparisonSales.Sum(),comparisonSales.Count),
                                                      TodaysSalesCount = todaysSales.Count,
                                                      TodaysSalesValue = todaysSales.Sum(),
                                                      TodaysAverageSalesValue = SafeDivide(todaysSales.Sum(),todaysSales.Count)
            };
            return response;
        }

        private IQueryable<TodayTransaction> GetTodaysSales(EstateManagementContext context,
                                                                        Int32 merchantReportingId, Int32 operatorReportingId){
                var salesForDate = (from t in context.TodayTransactions
                                    where t.IsAuthorised && t.TransactionType == "Sale"
                                                         && t.TransactionDate == DateTime.Now.Date
                                                         && t.TransactionTime <= DateTime.Now.TimeOfDay
                                    select t).AsQueryable();
            
            if (merchantReportingId> 0){
                salesForDate = salesForDate.Where(t => t.MerchantReportingId == merchantReportingId).AsQueryable();
            }

            if (operatorReportingId> 0)
            {
                salesForDate = salesForDate.Where(t => t.OperatorReportingId == operatorReportingId).AsQueryable();
            }

            return salesForDate;
        }

        private IQueryable<TransactionHistory> GetSalesForDate(EstateManagementContext context,
                                                                           DateTime queryDate,
                                                                           Int32 merchantReportingId, Int32 operatorReportingId)
        {
            var salesForDate = (from t in context.TransactionHistory
                                where t.IsAuthorised && t.TransactionType == "Sale"
                                                     && t.TransactionDate == queryDate.Date
                                                     && t.TransactionTime <= DateTime.Now.TimeOfDay
                                select t).AsQueryable();

            if (merchantReportingId > 0)
            {
                salesForDate = salesForDate.Where(t => t.MerchantReportingId == merchantReportingId).AsQueryable();
            }

            if (operatorReportingId > 0)
            {
                salesForDate = salesForDate.Where(t => t.OperatorReportingId == operatorReportingId).AsQueryable();
            }

            return salesForDate;
        }

        public async Task<TodaysSales> GetTodaysSales(Guid estateId, Int32 merchantReportingId, Int32 operatorReportingId, DateTime comparisonDate, CancellationToken cancellationToken){
            using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, estateId.ToString());
            await using EstateManagementContext context = resolvedContext.Context;

            IQueryable<TodayTransaction> todaysSales = GetTodaysSales(context, merchantReportingId, operatorReportingId);
            IQueryable<TransactionHistory> comparisonSales = GetSalesForDate(context, comparisonDate, merchantReportingId, operatorReportingId);

            var todaysSalesValue = await todaysSales.SumAsync(t => t.TransactionAmount, cancellationToken);
            var todaysSalesCount = await todaysSales.CountAsync(cancellationToken);
            var comparisonSalesValue = await comparisonSales.SumAsync(t => t.TransactionAmount, cancellationToken);
            var comparisonSalesCount = await comparisonSales.CountAsync(cancellationToken);

            TodaysSales response = new TodaysSales{
                                                      ComparisonSalesCount = comparisonSalesCount,
                                                      ComparisonSalesValue = comparisonSalesValue,
                                                      TodaysSalesCount = todaysSalesCount,
                                                      TodaysSalesValue = todaysSalesValue,
                                                      TodaysAverageSalesValue = todaysSalesValue / todaysSalesCount,
                                                      ComparisonAverageSalesValue = comparisonSalesValue / comparisonSalesCount
            };
            return response;
        }

        public async Task<List<TodaysSalesCountByHour>> GetTodaysSalesCountByHour(Guid estateId, Int32 merchantReportingId, Int32 operatorReportingId, DateTime comparisonDate, CancellationToken cancellationToken){
            using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, estateId.ToString());
            await using EstateManagementContext context = resolvedContext.Context;

            IQueryable<TodayTransaction> todaysSales = GetTodaysSales(context, merchantReportingId, operatorReportingId);
            IQueryable<TransactionHistory> comparisonSales = GetSalesForDate(context, comparisonDate, merchantReportingId, operatorReportingId);

            // First we need to get a value of todays sales
            var todaysSalesByHour = await (from t in todaysSales
                                     group t.TransactionAmount by t.Hour
                                     into g
                                     select new{
                                                   Hour = g.Key,
                                                   TotalSalesCount = g.Count()
                                               }).ToListAsync(cancellationToken);

            var comparisonSalesByHour = await (from t in comparisonSales
                                         group t.TransactionAmount by t.Hour
                                         into g
                                         select new{
                                                       Hour = g.Key,
                                                       TotalSalesCount = g.Count()
                                                   }).ToListAsync(cancellationToken);

            List<TodaysSalesCountByHour> response = (from today in todaysSalesByHour
                                                     join comparison in comparisonSalesByHour
                                                         on today.Hour equals comparison.Hour
                                                     select new TodaysSalesCountByHour
                                                     {
                                                         Hour = today.Hour.Value,
                                                         TodaysSalesCount = today.TotalSalesCount,
                                                         ComparisonSalesCount = comparison.TotalSalesCount
                                                     }).ToList();

            return response;
        }

        public async Task<List<TodaysSalesValueByHour>> GetTodaysSalesValueByHour(Guid estateId, Int32 merchantReportingId, Int32 operatorReportingId, DateTime comparisonDate, CancellationToken cancellationToken){
            using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, estateId.ToString());
            await using EstateManagementContext context = resolvedContext.Context;

            IQueryable<TodayTransaction> todaysSales =  GetTodaysSales(context, merchantReportingId, operatorReportingId);
            IQueryable<TransactionHistory> comparisonSales = GetSalesForDate(context, comparisonDate, merchantReportingId, operatorReportingId);

            // First we need to get a value of todays sales
            var todaysSalesByHour = await (from t in todaysSales
                                           group t.TransactionAmount by t.Hour
                                           into g
                                           select new
                                           {
                                               Hour = g.Key,
                                               TotalSalesValue = g.Sum()
                                           }).ToListAsync(cancellationToken);

            var comparisonSalesByHour = await (from t in comparisonSales
                                               group t.TransactionAmount by t.Hour
                                         into g
                                               select new
                                               {
                                                   Hour = g.Key,
                                                   TotalSalesValue = g.Sum()
                                               }).ToListAsync(cancellationToken);

            List<TodaysSalesValueByHour> response = (from today in todaysSalesByHour
                                                     join comparison in comparisonSalesByHour
                                                         on today.Hour equals comparison.Hour
                                                     select new TodaysSalesValueByHour
                                                     {
                                                         Hour = today.Hour.Value,
                                                         TodaysSalesValue = today.TotalSalesValue,
                                                         ComparisonSalesValue = comparison.TotalSalesValue
                                                     }).ToList();

            return response;
        }

        private IQueryable<MerchantSettlementFee> GetSettlementDataForDate(EstateManagementContext context, Int32 merchantReportingId, Int32 operatorReportingId, DateTime queryDate)
        {
            if (queryDate.Date == DateTime.Today.Date)
            {
                return this.GetTodaysSettlement(context, merchantReportingId, operatorReportingId);
            }
            
            var settlementData = (from s in context.Settlements
                join f in context.MerchantSettlementFees on s.SettlementId equals f.SettlementId
                join t in context.TransactionHistory on f.TransactionId equals t.TransactionId
                where s.SettlementDate == queryDate.Date
                select new { Settlement = s, Fees = f, t.MerchantReportingId, t.OperatorReportingId }).AsQueryable();

            if (merchantReportingId > 0)
            {
                settlementData = settlementData.Where(t => t.MerchantReportingId== merchantReportingId).AsQueryable();
            }

            if (operatorReportingId > 0)
            {
                settlementData = settlementData.Where(t => t.OperatorReportingId == operatorReportingId).AsQueryable();
            }

            return settlementData.AsQueryable().Select(s => s.Fees);
        }

        private IQueryable<MerchantSettlementFee> GetTodaysSettlement(EstateManagementContext? context, Int32 merchantReportingId, Int32 operatorReportingId)
        {
            var settlementData = (from s in context.Settlements
                                  join f in context.MerchantSettlementFees on s.SettlementId equals f.SettlementId
                                  join t in context.TodayTransactions on f.TransactionId equals t.TransactionId
                                  where s.SettlementDate == DateTime.Today.Date
                                  select new { Settlement = s, Fees = f, Transactions = t}).AsQueryable();

            if (merchantReportingId > 0)
            {
                settlementData = settlementData.Where(t => t.Transactions.MerchantReportingId == merchantReportingId).AsQueryable();
            }

            if (operatorReportingId > 0)
            {
                settlementData = settlementData.Where(t => t.Transactions.OperatorReportingId == operatorReportingId).AsQueryable();
            }

            return settlementData.AsQueryable().Select(s => s.Fees);
        }

        public async Task<TodaysSettlement> GetTodaysSettlement(Guid estateId, Int32 merchantReportingId, Int32 operatorReportingId, DateTime comparisonDate, CancellationToken cancellationToken){
            using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, estateId.ToString());
            await using EstateManagementContext context = resolvedContext.Context;

            IQueryable<MerchantSettlementFee> todaySettlementData = GetTodaysSettlement(context, merchantReportingId, operatorReportingId);
            IQueryable<MerchantSettlementFee> comparisonSettlementData = GetSettlementDataForDate(context, merchantReportingId, operatorReportingId, comparisonDate);

            var todaySettlement = await (from f in todaySettlementData
                                         group f by f.IsSettled into grouped
                                         select new
                                         {
                                             IsSettled = grouped.Key,
                                             CalculatedValueSum = grouped.Sum(item => item.CalculatedValue),
                                             CalculatedValueCount = grouped.Count()
                                         }).ToListAsync(cancellationToken);

            var comparisonSettlement = await (from f in comparisonSettlementData
                                              group f by f.IsSettled into grouped
                                              select new
                                              {
                                                  IsSettled = grouped.Key,
                                                  CalculatedValueSum = grouped.Sum(item => item.CalculatedValue),
                                                  CalculatedValueCount = grouped.Count()
                                              }).ToListAsync(cancellationToken);


            TodaysSettlement response = new TodaysSettlement
            {
                ComparisonSettlementCount = comparisonSettlement.FirstOrDefault(x => x.IsSettled)?.CalculatedValueCount ?? 0,
                ComparisonSettlementValue = comparisonSettlement.FirstOrDefault(x => x.IsSettled)?.CalculatedValueSum ?? 0,
                ComparisonPendingSettlementCount = comparisonSettlement.FirstOrDefault(x => x.IsSettled == false)?.CalculatedValueCount ?? 0,
                ComparisonPendingSettlementValue = comparisonSettlement.FirstOrDefault(x => x.IsSettled == false)?.CalculatedValueSum ?? 0,
                TodaysSettlementCount = todaySettlement.FirstOrDefault(x => x.IsSettled)?.CalculatedValueCount ?? 0,
                TodaysSettlementValue = todaySettlement.FirstOrDefault(x => x.IsSettled)?.CalculatedValueSum ?? 0,
                TodaysPendingSettlementCount = todaySettlement.FirstOrDefault(x => x.IsSettled == false)?.CalculatedValueCount ?? 0,
                TodaysPendingSettlementValue = todaySettlement.FirstOrDefault(x => x.IsSettled == false)?.CalculatedValueSum ?? 0
            };

            return response;
        }

        public async Task<List<TopBottomData>> GetTopBottomData(Guid estateId, TopBottom direction, Int32 resultCount, Dimension dimension, CancellationToken cancellationToken){
            using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, estateId.ToString());
            await using EstateManagementContext context = resolvedContext.Context;

            IQueryable<TodayTransaction> mainQuery = context.TodayTransactions
                .Where(joined => joined.IsAuthorised == true
                                 && joined.TransactionType == "Sale"
                                 && joined.TransactionDate == DateTime.Now.Date);

            IQueryable<TopBottomData> queryable = null;
            if (dimension == Dimension.Product)
            {
                // Products
                queryable = mainQuery
                            .Join(context.ContractProducts,
                                  t => t.ContractProductReportingId,
                                  contractProduct => contractProduct.ContractProductReportingId,
                                  (t, contractProduct) => new
                                  {
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
                            .Join(context.Operators,
                                  t => t.OperatorReportingId,
                                  o => o.OperatorReportingId,
                                  (t, o) => new
                                  {
                                      Transaction = t,
                                      Operator = o
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
                                  (t, merchant) => new
                                  {
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

            if (direction == TopBottom.Top)
            {
                // Top X
                queryable = queryable.OrderByDescending(g => g.SalesValue);
            }
            else if (direction == TopBottom.Bottom)
            {
                // Bottom X
                queryable = queryable.OrderBy(g => g.SalesValue);
            }

            // TODO: bad request??
            return await queryable.Take(resultCount).ToListAsync(cancellationToken);
        }

        public async Task<TodaysSales> GetMerchantPerformance(Guid estateId, DateTime comparisonDate, List<Int32> merchantReportingIds, CancellationToken cancellationToken){
            using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, estateId.ToString());
            await using EstateManagementContext context = resolvedContext.Context;

            // First we need to get a value of todays sales
            var todaysSalesQuery = (from t in context.TodayTransactions
                                        where t.IsAuthorised && t.TransactionType == "Sale"
                                                             && t.TransactionDate == DateTime.Now.Date
                                                             && t.TransactionTime <= DateTime.Now.TimeOfDay
                                        select t);

            var comparisonSalesQuery = (from t in context.TransactionHistory
                                             where t.IsAuthorised && t.TransactionType == "Sale"
                                                                  && t.TransactionDate == comparisonDate
                                                                  && t.TransactionTime <= DateTime.Now.TimeOfDay
                                             select t);


            if (merchantReportingIds.Any()){
                todaysSalesQuery = todaysSalesQuery.Where(t => merchantReportingIds.Contains(t.MerchantReportingId));
                comparisonSalesQuery = comparisonSalesQuery.Where(t => merchantReportingIds.Contains(t.MerchantReportingId));
            }

            TodaysSales response = new TodaysSales
            {
                ComparisonSalesCount = comparisonSalesQuery.Count(),
                ComparisonSalesValue = comparisonSalesQuery.Sum(t => t.TransactionAmount),
                TodaysSalesCount = todaysSalesQuery.Count(),
                TodaysSalesValue = todaysSalesQuery.Sum(t => t.TransactionAmount),
            };
            response.ComparisonAverageSalesValue =
                SafeDivide(response.ComparisonSalesValue, response.ComparisonSalesCount);
            response.TodaysAverageSalesValue =
                SafeDivide(response.TodaysSalesValue, response.TodaysSalesCount);

            return response;
        }

        private Int32 SafeDivide(Int32 number, Int32 divisor)
        {
            if (divisor == 0)
            {
                return number;
            }

            return number / divisor;
        }

        private Decimal SafeDivide(Decimal number, Int32 divisor)
        {
            if (divisor == 0)
            {
                return number;
            }

            return number / divisor;
        }

        public async Task<TodaysSales> GetProductPerformance(Guid estateId, DateTime comparisonDate, List<Int32> productReportingIds, CancellationToken cancellationToken)
        {
            using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, estateId.ToString());
            await using EstateManagementContext context = resolvedContext.Context;

            // First we need to get a value of todays sales
            var todaysSalesQuery = (from t in context.TodayTransactions
                where t.IsAuthorised && t.TransactionType == "Sale"
                                     && t.TransactionDate == DateTime.Now.Date
                                     && t.TransactionTime <= DateTime.Now.TimeOfDay
                select t);

            var comparisonSalesQuery = (from t in context.TransactionHistory
                where t.IsAuthorised && t.TransactionType == "Sale"
                                     && t.TransactionDate == comparisonDate
                                     && t.TransactionTime <= DateTime.Now.TimeOfDay
                select t);


            if (productReportingIds.Any())
            {
                todaysSalesQuery = todaysSalesQuery.Where(t => productReportingIds.Contains(t.ContractProductReportingId));
                comparisonSalesQuery = comparisonSalesQuery.Where(t => productReportingIds.Contains(t.ContractProductReportingId));
            }

            TodaysSales response = new TodaysSales
            {
                ComparisonSalesCount = comparisonSalesQuery.Count(),
                ComparisonSalesValue = comparisonSalesQuery.Sum(t => t.TransactionAmount),
                TodaysSalesCount = todaysSalesQuery.Count(),
                TodaysSalesValue = todaysSalesQuery.Sum(t => t.TransactionAmount),
            };
            response.ComparisonAverageSalesValue =
                SafeDivide(response.ComparisonSalesValue, response.ComparisonSalesCount);
            response.TodaysAverageSalesValue =
                SafeDivide(response.TodaysSalesValue, response.TodaysSalesCount);

            return response;
        }

        public async Task<TodaysSales> GetOperatorPerformance(Guid estateId, DateTime comparisonDate, List<Int32> operatorReportingIds, CancellationToken cancellationToken){
            using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, estateId.ToString());
            await using EstateManagementContext context = resolvedContext.Context;

            // First we need to get a value of todays sales
            var todaysSalesQuery = (from t in context.TodayTransactions
                where t.IsAuthorised && t.TransactionType == "Sale"
                                     && t.TransactionDate == DateTime.Now.Date
                                     && t.TransactionTime <= DateTime.Now.TimeOfDay
                select t);

            var comparisonSalesQuery = (from t in context.TransactionHistory
                where t.IsAuthorised && t.TransactionType == "Sale"
                                     && t.TransactionDate == comparisonDate
                                     && t.TransactionTime <= DateTime.Now.TimeOfDay
                select t);


            if (operatorReportingIds.Any())
            {
                todaysSalesQuery = todaysSalesQuery.Where(t => operatorReportingIds.Contains(t.OperatorReportingId));
                comparisonSalesQuery = comparisonSalesQuery.Where(t => operatorReportingIds.Contains(t.OperatorReportingId));
            }

            TodaysSales response = new TodaysSales
            {
                ComparisonSalesCount = comparisonSalesQuery.Count(),
                ComparisonSalesValue = comparisonSalesQuery.Sum(t => t.TransactionAmount),
                TodaysSalesCount = todaysSalesQuery.Count(),
                TodaysSalesValue = todaysSalesQuery.Sum(t => t.TransactionAmount),
            };
            response.ComparisonAverageSalesValue =
                SafeDivide(response.ComparisonSalesValue, response.ComparisonSalesCount);
            response.TodaysAverageSalesValue =
                SafeDivide(response.TodaysSalesValue, response.TodaysSalesCount);

            return response;
        }
        
        public async Task<List<Merchant>> GetMerchants(Guid estateId,
                                                       CancellationToken cancellationToken) {
            using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, estateId.ToString());
            await using EstateManagementContext context = resolvedContext.Context;

            var merchants = context.Merchants.Select(m => new {
                MerchantReportingId = m.MerchantReportingId,
                Name = m.Name,
                LastSaleDateTime = m.LastSaleDateTime,
                LastSale = m.LastSaleDate,
                CreatedDateTime = m.CreatedDateTime,
                LastStatement = m.LastStatementGenerated,
                MerchantId = m.MerchantId,
                Reference = m.Reference,
                AddressInfo = context.MerchantAddresses.Where(ma => ma.MerchantId == m.MerchantId).OrderByDescending(ma => ma.CreatedDateTime).Select(ma => new {
                    PostCode = ma.PostalCode, Region = ma.Region, Town = ma.Town,
                    // Add more properties as needed
                }).FirstOrDefault(), // Get the first matching MerchantAddress or null
                EstateReportingId = context.Estates.Single(e => e.EstateId == m.EstateId).EstateReportingId
            });

            List<Merchant> merchantList = new List<Merchant>();
            foreach (var result in merchants) {
                var model = new Merchant {
                    MerchantId = result.MerchantId,
                    Name = result.Name,
                    Reference = result.Reference,
                    MerchantReportingId = result.MerchantReportingId,
                    CreatedDateTime = result.CreatedDateTime,
                    EstateReportingId = result.EstateReportingId,
                    LastSale = result.LastSale,
                    LastSaleDateTime = result.LastSaleDateTime,
                    LastStatement = result.LastStatement
                };

                if (result.AddressInfo != null) {
                    model.PostCode = result.AddressInfo.PostCode;
                    model.Town = result.AddressInfo.Town;
                    model.Region = result.AddressInfo.Region;
                }

                merchantList.Add(model);
            }

            return merchantList;
        }

        public async Task<List<Operator>> GetOperators(Guid estateId, CancellationToken cancellationToken){
            using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, estateId.ToString());
            await using EstateManagementContext context = resolvedContext.Context;

            List<Operator> operators = await (from o in context.Operators
                                              select new Operator
                                                     {
                                                         Name = o.Name,
                                                         EstateReportingId = context.Estates.Single(e => e.EstateId == o.EstateId).EstateReportingId,
                                                         OperatorId = o.OperatorId,
                                                         OperatorReportingId = o.OperatorReportingId
                                                     }).ToListAsync(cancellationToken);
            
            return operators;
        }

        #endregion

        #region Others

        private const String ConnectionStringIdentifier = "EstateReportingReadModel";

        #endregion
    }
}