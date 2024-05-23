namespace EstateReportingAPI.BusinessLogic{
    using EstateManagement.Database.Contexts;
    using EstateManagement.Database.Entities;
    using Microsoft.EntityFrameworkCore;
    using Models;
    using Shared.Exceptions;
    using System.Linq;
    using System.Linq.Dynamic.Core;
    using System.Linq.Expressions;
    using System.Threading;
    using Calendar = Models.Calendar;
    using Merchant = Models.Merchant;
    using System.Linq.Expressions;
    using System.Reflection;

    public class ReportingManager : IReportingManager{
        #region Fields

        private readonly Shared.EntityFramework.IDbContextFactory<EstateManagementGenericContext> ContextFactory;

        private Guid Id;

        #endregion

        #region Constructors

        public ReportingManager(Shared.EntityFramework.IDbContextFactory<EstateManagementGenericContext> contextFactory){
            this.ContextFactory = contextFactory;
        }

        #endregion

        #region Methods

        public async Task<List<Calendar>> GetCalendarComparisonDates(Guid estateId, CancellationToken cancellationToken){
            EstateManagementGenericContext? context = await this.ContextFactory.GetContext(estateId, ReportingManager.ConnectionStringIdentifier, cancellationToken);

            DateTime startOfYear = new DateTime(DateTime.Now.Year, 1, 1);

            List<EstateManagement.Database.Entities.Calendar> entities = context.Calendar.Where(c => c.Date >= startOfYear && c.Date < DateTime.Now.Date.AddDays(-1)).OrderByDescending(d => d.Date).ToList();

            List<Calendar> result = new List<Calendar>();
            foreach (EstateManagement.Database.Entities.Calendar calendar in entities){
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
            EstateManagementGenericContext? context = await this.ContextFactory.GetContext(estateId, ReportingManager.ConnectionStringIdentifier, cancellationToken);

            List<EstateManagement.Database.Entities.Calendar> entities = context.Calendar.Where(c => c.Date <= DateTime.Now.Date).ToList();

            List<Calendar> result = new List<Calendar>();
            foreach (EstateManagement.Database.Entities.Calendar calendar in entities){
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
            EstateManagementGenericContext? context = await this.ContextFactory.GetContext(estateId, ReportingManager.ConnectionStringIdentifier, cancellationToken);
            
            List<Int32> years = context.Calendar.Where(c => c.Date <= DateTime.Now.Date).GroupBy(c => c.Year).Select(y => y.Key).ToList();

            return years;
        }

        public async Task<LastSettlement> GetLastSettlement(Guid estateId, CancellationToken cancellationToken){
            EstateManagementGenericContext? context = await this.ContextFactory.GetContext(estateId, ReportingManager.ConnectionStringIdentifier, cancellationToken);

            DateTime settlementDate = await context.Settlements.Where(s => s.IsCompleted).OrderByDescending(s => s.SettlementDate).Select(s => s.SettlementDate).FirstOrDefaultAsync(cancellationToken);

            IQueryable<LastSettlement> settlements = from settlement in context.Settlements
                                                     join merchantSettlementFee in context.MerchantSettlementFees
                                                         on settlement.SettlementReportingId equals merchantSettlementFee.SettlementReportingId
                                                     join transaction in context.Transactions
                                                         on merchantSettlementFee.TransactionReportingId equals transaction.TransactionReportingId
                                                     where settlement.SettlementDate == settlementDate
                                                     && merchantSettlementFee.IsSettled
                                                     group new
                                                           {
                                                               settlement.SettlementDate,
                                                               CalculatedValue = merchantSettlementFee.CalculatedValue,
                                                               TransactionAmount = transaction.TransactionAmount
                                                           } by settlement.SettlementDate into grouped
                                                     select new LastSettlement
                                                            {
                                                                SettlementDate = grouped.Key,
                                                                FeesValue = grouped.Sum(item => item.CalculatedValue),
                                                                SalesValue = grouped.Sum(item => item.TransactionAmount),
                                                                SalesCount= grouped.Count()
                                                            };
            return await settlements.SingleOrDefaultAsync(cancellationToken);
        }

        public async Task<MerchantKpi> GetMerchantsTransactionKpis(Guid estateId, CancellationToken cancellationToken){
            EstateManagementGenericContext? context = await this.ContextFactory.GetContext(estateId, ReportingManager.ConnectionStringIdentifier, cancellationToken);

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
            EstateManagementGenericContext? context = await this.ContextFactory.GetContext(estateId, ReportingManager.ConnectionStringIdentifier, cancellationToken);

            List<Merchant> response = new();

            var merchants = await context.Merchants
                                   .Where(m => m.LastSaleDateTime >= startDateTime && m.LastSaleDateTime <= endDateTime)
                                                    .Select(m => new
                                                    {
                                                        MerchantReportingId = m.MerchantReportingId,
                                                        EstateReportingId = m.EstateReportingId,
                                                        Name = m.Name,
                                                        LastSaleDateTime = m.LastSaleDateTime,
                                                        LastSale = m.LastSaleDate,
                                                        CreatedDateTime = m.CreatedDateTime,
                                                        LastStatement = m.LastStatementGenerated,
                                                        MerchantId = m.MerchantId,
                                                        Reference = m.Reference,
                                                        AddressInfo = context.MerchantAddresses
                                                                                          .Where(ma => ma.MerchantReportingId == m.MerchantReportingId)
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
            
            merchants.ForEach(m => response.Add(new Merchant{
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
            EstateManagementGenericContext? context = await this.ContextFactory.GetContext(estateId, ReportingManager.ConnectionStringIdentifier, cancellationToken);
            List<ResponseCode> response = new List<ResponseCode>();
            
            List<ResponseCodes> responseCodes = await context.ResponseCodes.ToListAsync(cancellationToken);

            responseCodes.ForEach(r => response.Add(new ResponseCode{
                                                                        Code = r.ResponseCode,
                                                                        Description = r.Description
                                                                    }));

            return response;
        }

        public async Task<TodaysSales> GetTodaysFailedSales(Guid estateId, DateTime comparisonDate, String responseCode, CancellationToken cancellationToken){
            EstateManagementGenericContext? context = await this.ContextFactory.GetContext(estateId, ReportingManager.ConnectionStringIdentifier, cancellationToken);

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
                                                      ComparisonAverageSalesValue = comparisonSalesValue/comparisonSalesCount,
                                                      TodaysSalesCount = todaysSalesCount,
                                                      TodaysSalesValue = todaysSalesValue,
                                                      TodaysAverageSalesValue = todaysSalesValue/todaysSalesCount
                                                  };

            return response;
        }

        private async Task<IQueryable<Transaction>> GetSalesForDate(EstateManagementGenericContext context,
                                                                    DateTime queryDate,
                                                                    Guid? merchantId,
                                                                    Guid? operatorId){
            var salesForDate = (from t in context.Transactions
                                where t.IsAuthorised && t.TransactionType == "Sale"
                                                     && t.TransactionDate == queryDate.Date
                                                     && t.TransactionTime <= DateTime.Now.TimeOfDay
                                select t).AsQueryable();

            if (merchantId.HasValue){
                EstateManagement.Database.Entities.Merchant? m = await context.Merchants.SingleOrDefaultAsync(m => m.MerchantId == merchantId.Value);

                if (m == null){
                    throw new NotFoundException($"Merchant Id {merchantId} not found");
                }

                salesForDate = salesForDate.Where(t => t.MerchantReportingId == m.MerchantReportingId).AsQueryable();
            }

            if (operatorId.HasValue){
                var o = await context.EstateOperators.SingleOrDefaultAsync(o => o.OperatorId == operatorId.Value);

                if (o == null){
                    throw new NotFoundException($"Operator Id {operatorId} not found");
                }

                salesForDate = salesForDate.Where(t => t.EstateOperatorReportingId == o.EstateOperatorReportingId).AsQueryable();
            }

            return salesForDate;
        }

        public async Task<TodaysSales> GetTodaysSales(Guid estateId, Guid? merchantId, Guid? operatorId, DateTime comparisonDate, CancellationToken cancellationToken){
            EstateManagementGenericContext? context = await this.ContextFactory.GetContext(estateId, ReportingManager.ConnectionStringIdentifier, cancellationToken);

            IQueryable<Transaction> todaysSales = await GetSalesForDate(context, DateTime.Now, merchantId,operatorId);
            IQueryable<Transaction> comparisonSales = await GetSalesForDate(context, comparisonDate, merchantId, operatorId);
            
            var todaysSalesValue = await todaysSales.SumAsync(t => t.TransactionAmount, cancellationToken);
            var todaysSalesCount = await todaysSales.CountAsync(cancellationToken);
            var comparisonSalesValue = await comparisonSales.SumAsync(t => t.TransactionAmount, cancellationToken);
            var comparisonSalesCount = await comparisonSales.CountAsync(cancellationToken);

            TodaysSales response = new TodaysSales{
                                                      ComparisonSalesCount = comparisonSalesCount,
                                                      ComparisonSalesValue = comparisonSalesValue,
                                                      TodaysSalesCount = todaysSalesCount,
                                                      TodaysSalesValue = todaysSalesValue
                                                  };
            return response;
        }

        public async Task<List<TodaysSalesCountByHour>> GetTodaysSalesCountByHour(Guid estateId, Guid? merchantId, Guid? operatorId, DateTime comparisonDate, CancellationToken cancellationToken){
            EstateManagementGenericContext? context = await this.ContextFactory.GetContext(estateId, ReportingManager.ConnectionStringIdentifier, cancellationToken);

            IQueryable<Transaction> todaysSales = await GetSalesForDate(context, DateTime.Now, merchantId, operatorId);
            IQueryable<Transaction> comparisonSales = await GetSalesForDate(context, comparisonDate, merchantId, operatorId);

            // First we need to get a value of todays sales
            var todaysSalesByHour = (from t in todaysSales
                                     group t.TransactionAmount by t.TransactionTime.Hours
                                     into g
                                     select new{
                                                   Hour = g.Key,
                                                   TotalSalesCount = g.Count()
                                               }).ToList();

            var comparisonSalesByHour = (from t in comparisonSales
                                         group t.TransactionAmount by t.TransactionTime.Hours
                                         into g
                                         select new{
                                                       Hour = g.Key,
                                                       TotalSalesCount = g.Count()
                                                   }).ToList();

            List<TodaysSalesCountByHour> response = (from today in todaysSalesByHour
                                                     join comparison in comparisonSalesByHour
                                                         on today.Hour equals comparison.Hour
                                                     select new TodaysSalesCountByHour{
                                                                                          Hour = today.Hour,
                                                                                          TodaysSalesCount = today.TotalSalesCount,
                                                                                          ComparisonSalesCount = comparison.TotalSalesCount
                                                                                      }).ToList();

            return response;
        }

        public async Task<List<TodaysSalesValueByHour>> GetTodaysSalesValueByHour(Guid estateId, Guid? merchantId, Guid? operatorId, DateTime comparisonDate, CancellationToken cancellationToken){
            EstateManagementGenericContext? context = await this.ContextFactory.GetContext(estateId, ReportingManager.ConnectionStringIdentifier, cancellationToken);

            IQueryable<Transaction> todaysSales = await GetSalesForDate(context, DateTime.Now, merchantId, operatorId);
            IQueryable<Transaction> comparisonSales = await GetSalesForDate(context, comparisonDate, merchantId, operatorId);

            // First we need to get a value of todays sales
            var todaysSalesByHour = await (from t in todaysSales
                                           group t.TransactionAmount by t.TransactionTime.Hours
                                           into g
                                           select new{
                                                         Hour = g.Key,
                                                         TotalSalesValue = g.Sum()
                                                     }).ToListAsync(cancellationToken);

            var comparisonSalesByHour = await (from t in comparisonSales
                                         group t.TransactionAmount by t.TransactionTime.Hours
                                         into g
                                         select new{
                                                       Hour = g.Key,
                                                       TotalSalesValue = g.Sum()
                                                   }).ToListAsync(cancellationToken);

            List<TodaysSalesValueByHour> response = (from today in todaysSalesByHour
                                                     join comparison in comparisonSalesByHour
                                                         on today.Hour equals comparison.Hour
                                                     select new TodaysSalesValueByHour{
                                                                                          Hour = today.Hour,
                                                                                          TodaysSalesValue = today.TotalSalesValue,
                                                                                          ComparisonSalesValue = comparison.TotalSalesValue
                                                                                      }).ToList();

            return response;
        }

        private async Task<IQueryable<MerchantSettlementFee>> GetSettlementDataForDate(EstateManagementGenericContext context, Guid? merchantId, Guid? operatorId, DateTime queryDate)
        {
            var settlementData = (from s in context.Settlements
                                  join f in context.MerchantSettlementFees on s.SettlementReportingId equals f.SettlementReportingId
                                  join t in context.Transactions on f.TransactionReportingId equals t.TransactionReportingId
                                  where s.SettlementDate == queryDate
                                  select new { Settlement = s, Fees = f, Transaction = t }).AsQueryable();
            
            if (merchantId.HasValue)
            {
                EstateManagement.Database.Entities.Merchant? m = await context.Merchants.SingleOrDefaultAsync(m => m.MerchantId == merchantId.Value);

                if (m == null)
                {
                    throw new NotFoundException($"Merchant Id {merchantId} not found");
                }

                settlementData = settlementData.Where(t => t.Settlement.MerchantReportingId== m.MerchantReportingId).AsQueryable();
            }

            if (operatorId.HasValue)
            {
                EstateOperator? o = await context.EstateOperators.SingleOrDefaultAsync(o => o.OperatorId == operatorId.Value);

                if (o == null)
                {
                    throw new NotFoundException($"Operator Id {operatorId} not found");
                }

                settlementData = settlementData.Where(t => t.Transaction.EstateOperatorReportingId == o.EstateOperatorReportingId).AsQueryable();
            }



            return settlementData.AsQueryable().Select(s => s.Fees);
        }

        public async Task<TodaysSettlement> GetTodaysSettlement(Guid estateId, Guid? merchantId, Guid? operatorId, DateTime comparisonDate, CancellationToken cancellationToken){
            EstateManagementGenericContext? context = await this.ContextFactory.GetContext(estateId, ReportingManager.ConnectionStringIdentifier, cancellationToken);

            IQueryable<MerchantSettlementFee> todaySettlementData = await GetSettlementDataForDate(context, merchantId, operatorId, DateTime.Now.Date);
            IQueryable<MerchantSettlementFee> comparisonSettlementData = await GetSettlementDataForDate(context, merchantId, operatorId, comparisonDate);

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


            TodaysSettlement response = new TodaysSettlement{
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
            EstateManagementGenericContext? context = await this.ContextFactory.GetContext(estateId, ReportingManager.ConnectionStringIdentifier, cancellationToken);

            IQueryable<Transaction> mainQuery = context.Transactions
                                                       .Where(joined => joined.IsAuthorised == true
                                                                        && joined.TransactionType == "Sale"
                                                                        && joined.TransactionDate == DateTime.Now.Date);
            IQueryable<TopBottomData> queryable = null;
            if (dimension == Dimension.Product){
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
                            .Select(g => new TopBottomData{
                                                              DimensionName = g.Key,
                                                              SalesValue = g.Sum(t => t.Transaction.TransactionAmount)
                                                          });
            }
            else if (dimension == Dimension.Operator){
                // Operators
                queryable = mainQuery
                            .Join(context.EstateOperators,
                                  t => t.EstateOperatorReportingId,
                                  oper => oper.EstateOperatorReportingId,
                                  (t, oper) => new{
                                                      Transaction = t,
                                                      Operator = oper
                                                  })
                            .GroupBy(joined => joined.Operator.Name)
                            .Select(g => new TopBottomData{
                                                              DimensionName = g.Key,
                                                              SalesValue = g.Sum(t => t.Transaction.TransactionAmount)
                                                          });
            }
            else if (dimension == Dimension.Merchant){
                // Operators
                queryable = mainQuery
                            .Join(context.Merchants,
                                  t => t.MerchantReportingId,
                                  merchant => merchant.MerchantReportingId,
                                  (t, merchant) => new{
                                                          Transaction = t,
                                                          Merchant = merchant
                                                      })
                            .GroupBy(joined => joined.Merchant.Name)
                            .Select(g => new TopBottomData{
                                                              DimensionName = g.Key,
                                                              SalesValue = g.Sum(t => t.Transaction.TransactionAmount)
                                                          });
            }

            if (direction == TopBottom.Top){
                // Top X
                queryable = queryable.OrderByDescending(g => g.SalesValue);
            }
            else if (direction == TopBottom.Bottom){
                // Bottom X
                queryable = queryable.OrderBy(g => g.SalesValue);
            }

            // TODO: bad request??
            return await queryable.Take(resultCount).ToListAsync(cancellationToken);
        }

        public async Task<TodaysSales> GetMerchantPerformance(Guid estateId, DateTime comparisonDate, List<Int32> merchantIds, CancellationToken cancellationToken){
            EstateManagementGenericContext? context = await this.ContextFactory.GetContext(estateId, ReportingManager.ConnectionStringIdentifier, cancellationToken);

            // First we need to get a value of todays sales
            var todaysSalesQuery = (from t in context.Transactions
                                        where t.IsAuthorised && t.TransactionType == "Sale"
                                                             && t.TransactionDate == DateTime.Now.Date
                                                             && t.TransactionTime <= DateTime.Now.TimeOfDay
                                        select t);
            
            var comparisonSalesQuery = (from t in context.Transactions
                                             where t.IsAuthorised && t.TransactionType == "Sale"
                                                                  && t.TransactionDate == comparisonDate
                                                                  && t.TransactionTime <= DateTime.Now.TimeOfDay
                                             select t);


            if (merchantIds.Any()){
                todaysSalesQuery = todaysSalesQuery.Where(t => merchantIds.Contains(t.MerchantReportingId));
                comparisonSalesQuery = comparisonSalesQuery.Where(t => merchantIds.Contains(t.MerchantReportingId));
            }

            TodaysSales response = new TodaysSales
            {
                ComparisonSalesCount = comparisonSalesQuery.Count(),
                ComparisonSalesValue = comparisonSalesQuery.Sum(t => t.TransactionAmount),
                ComparisonAverageSalesValue = comparisonSalesQuery.Sum(t => t.TransactionAmount) / comparisonSalesQuery.Count(),
                TodaysSalesCount = todaysSalesQuery.Count(),
                TodaysSalesValue = todaysSalesQuery.Sum(t => t.TransactionAmount),
                TodaysAverageSalesValue = todaysSalesQuery.Sum(t => t.TransactionAmount) / todaysSalesQuery.Count()
            };
            return response;
        }

        public async Task<TodaysSales> GetProductPerformance(Guid estateId, DateTime comparisonDate, List<Int32> productIds, CancellationToken cancellationToken)
        {
            EstateManagementGenericContext? context = await this.ContextFactory.GetContext(estateId, ReportingManager.ConnectionStringIdentifier, cancellationToken);

            // First we need to get a value of todays sales
            var todaysSalesQuery = (from t in context.Transactions
                                    where t.IsAuthorised && t.TransactionType == "Sale"
                                                         && t.TransactionDate == DateTime.Now.Date
                                                         && t.TransactionTime <= DateTime.Now.TimeOfDay
                                    select t);

            var comparisonSalesQuery = (from t in context.Transactions
                                        where t.IsAuthorised && t.TransactionType == "Sale"
                                                             && t.TransactionDate == comparisonDate
                                                             && t.TransactionTime <= DateTime.Now.TimeOfDay
                                        select t);


            if (productIds.Any())
            {
                todaysSalesQuery = todaysSalesQuery.Where(t => productIds.Contains(t.ContractProductReportingId));
                comparisonSalesQuery = comparisonSalesQuery.Where(t => productIds.Contains(t.ContractProductReportingId));
            }

            TodaysSales response = new TodaysSales
            {
                ComparisonSalesCount = comparisonSalesQuery.Count(),
                ComparisonSalesValue = comparisonSalesQuery.Sum(t => t.TransactionAmount),
                ComparisonAverageSalesValue = comparisonSalesQuery.Sum(t => t.TransactionAmount) / comparisonSalesQuery.Count(),
                TodaysSalesCount = todaysSalesQuery.Count(),
                TodaysSalesValue = todaysSalesQuery.Sum(t => t.TransactionAmount),
                TodaysAverageSalesValue = todaysSalesQuery.Sum(t => t.TransactionAmount) / todaysSalesQuery.Count()
            };
            return response;
        }

        public async Task<TodaysSales> GetOperatorPerformance(Guid estateId, DateTime comparisonDate, List<Int32> operatorIds, CancellationToken cancellationToken){
            EstateManagementGenericContext? context = await this.ContextFactory.GetContext(estateId, ReportingManager.ConnectionStringIdentifier, cancellationToken);

            // First we need to get a value of todays sales
            var todaysSalesQuery = (from t in context.Transactions
                                    where t.IsAuthorised && t.TransactionType == "Sale"
                                                         && t.TransactionDate == DateTime.Now.Date
                                                         && t.TransactionTime <= DateTime.Now.TimeOfDay
                                    select t);

            var comparisonSalesQuery = (from t in context.Transactions
                                        where t.IsAuthorised && t.TransactionType == "Sale"
                                                             && t.TransactionDate == comparisonDate
                                                             && t.TransactionTime <= DateTime.Now.TimeOfDay
                                        select t);


            if (operatorIds.Any())
            {
                todaysSalesQuery = todaysSalesQuery.Where(t => operatorIds.Contains(t.EstateOperatorReportingId));
                comparisonSalesQuery = comparisonSalesQuery.Where(t => operatorIds.Contains(t.EstateOperatorReportingId));
            }

            TodaysSales response = new TodaysSales
                                   {
                                       ComparisonSalesCount = comparisonSalesQuery.Count(),
                                       ComparisonSalesValue = comparisonSalesQuery.Sum(t => t.TransactionAmount),
                                       ComparisonAverageSalesValue = comparisonSalesQuery.Sum(t => t.TransactionAmount) / comparisonSalesQuery.Count(),
                                       TodaysSalesCount = todaysSalesQuery.Count(),
                                       TodaysSalesValue = todaysSalesQuery.Sum(t => t.TransactionAmount),
                                       TodaysAverageSalesValue = todaysSalesQuery.Sum(t => t.TransactionAmount) / todaysSalesQuery.Count()
                                   };
            return response;
        }

        public async Task<List<TransactionResult>> TransactionSearch(Guid estateId, TransactionSearchRequest searchRequest, PagingRequest pagingRequest, SortingRequest sortingRequest, CancellationToken cancellationToken){

            // Base query before any filtering is added
            EstateManagementGenericContext? context = await this.ContextFactory.GetContext(estateId, ReportingManager.ConnectionStringIdentifier, cancellationToken);

            var mainQuery = (from txn in context.Transactions
                             join merchant in context.Merchants on txn.MerchantReportingId equals merchant.MerchantReportingId
                             join estateOperator in context.EstateOperators on txn.EstateOperatorReportingId equals estateOperator.EstateOperatorReportingId
                             join product in context.ContractProducts on txn.ContractProductReportingId equals product.ContractProductReportingId
                             where txn.TransactionDate == searchRequest.QueryDate.Date
                             select new
                                    {
                                        Transaction = txn,
                                        Merchant = merchant,
                                        Operator = estateOperator,
                                        Product = product
                                    }).AsQueryable();

            // Now apply the filtering
            if (searchRequest.Operators != null && searchRequest.Operators.Any()){
                mainQuery = mainQuery.Where(m => searchRequest.Operators.Contains(m.Operator.EstateOperatorReportingId));
            }

            if (searchRequest.Merchants != null && searchRequest.Merchants.Any()){
                mainQuery = mainQuery.Where(m => searchRequest.Merchants.Contains(m.Merchant.MerchantReportingId));
            }

            if (searchRequest.ValueRange != null){
                mainQuery = mainQuery.Where(m => m.Transaction.TransactionAmount >= searchRequest.ValueRange.StartValue &&
                                                 m.Transaction.TransactionAmount <= searchRequest.ValueRange.EndValue);
            }

            if (String.IsNullOrEmpty(searchRequest.AuthCode) == false){
                mainQuery = mainQuery.Where(m => m.Transaction.AuthorisationCode == searchRequest.AuthCode);
            }

            if (String.IsNullOrEmpty(searchRequest.ResponseCode) == false){
                mainQuery = mainQuery.Where(m => m.Transaction.ResponseCode == searchRequest.ResponseCode);
            }

            if (String.IsNullOrEmpty(searchRequest.TransactionNumber) == false){
                mainQuery = mainQuery.Where(m => m.Transaction.TransactionNumber == searchRequest.TransactionNumber);
            }

            Int32 skipCount = 0;
            if (pagingRequest.Page > 1){
                skipCount = (pagingRequest.Page - 1) * pagingRequest.PageSize;
            }

            if (sortingRequest != null){
                // Handle order by here, cant think of a better way of achieving this
                mainQuery = (sortingRequest.SortDirection, sortingRequest.SortField) switch{
                    (SortDirection.Ascending, SortField.MerchantName) => mainQuery.OrderBy(m => m.Merchant.Name),
                    (SortDirection.Ascending, SortField.OperatorName) => mainQuery.OrderBy(m => m.Operator.Name),
                    (SortDirection.Ascending, SortField.TransactionAmount) => mainQuery.OrderBy(m => m.Transaction.TransactionAmount),
                    (SortDirection.Descending, SortField.MerchantName) => mainQuery.OrderByDescending(m => m.Merchant.Name),
                    (SortDirection.Descending, SortField.OperatorName) => mainQuery.OrderByDescending(m => m.Operator.Name),
                    (SortDirection.Descending, SortField.TransactionAmount) => mainQuery.OrderByDescending(m => m.Transaction.TransactionAmount),
                    _ => mainQuery.OrderByDescending(m => m.Transaction.TransactionDateTime)
                };
            }

            var queryResults = await mainQuery.Skip(skipCount).Take(pagingRequest.PageSize)
                                              .ToListAsync(cancellationToken);

            List<TransactionResult> results = new List<TransactionResult>();

            queryResults.ForEach(qr => {
                                     results.Add(new TransactionResult{
                                         MerchantReportingId = qr.Merchant.MerchantReportingId,
                                         ResponseCode = qr.Transaction.ResponseCode,
                                         IsAuthorised = qr.Transaction.IsAuthorised,
                                         MerchantName = qr.Merchant.Name,
                                         OperatorName = qr.Operator.Name,
                                         OperatorReportingId = qr.Operator.EstateOperatorReportingId,
                                         Product = qr.Product.ProductName,
                                         ProductReportingId = qr.Product.ContractProductReportingId,
                                         ResponseMessage = qr.Transaction.ResponseMessage,
                                         TransactionDateTime = qr.Transaction.TransactionDateTime,
                                         TransactionId = qr.Transaction.TransactionId,
                                         TransactionReportingId = qr.Transaction.TransactionReportingId,
                                         TransactionSource = qr.Transaction.TransactionSource.ToString(), // TODO: Name for this
                                         TransactionAmount = qr.Transaction.TransactionAmount
                                     });
                                 });

            return results;
        }
        
        public async Task<List<Merchant>> GetMerchants(Guid estateId, CancellationToken cancellationToken){
            EstateManagementGenericContext? context = await this.ContextFactory.GetContext(estateId, ReportingManager.ConnectionStringIdentifier, cancellationToken);

            var merchants = context.Merchants
                                                    .Select(m => new 
                                                                 {
                                                                     MerchantReportingId = m.MerchantReportingId,
                                                                     EstateReportingId = m.EstateReportingId,
                                                                     Name = m.Name,
                                                                     LastSaleDateTime = m.LastSaleDateTime,
                                                                     LastSale = m.LastSaleDate,
                                                                     CreatedDateTime = m.CreatedDateTime,
                                                                     LastStatement = m.LastStatementGenerated,
                                                                     MerchantId = m.MerchantId,
                                                                     Reference = m.Reference,
                                                                     AddressInfo = context.MerchantAddresses
                                                                                          .Where(ma => ma.MerchantReportingId == m.MerchantReportingId)
                                                                                          .OrderByDescending(ma => ma.CreatedDateTime)
                                                                                          .Select(ma => new 
                                                                                                        {
                                                                                                            PostCode = ma.PostalCode,
                                                                                                            Region = ma.Region,
                                                                                                            Town = ma.Town,
                                                                                                            // Add more properties as needed
                                                                                                        })
                                                                                          .FirstOrDefault() // Get the first matching MerchantAddress or null
                                                                 });

            List<Merchant> merchantList = new List<Merchant>();
            foreach (var result in merchants){
                var model = new Merchant{
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

                if (result.AddressInfo != null){
                    model.PostCode = result.AddressInfo.PostCode;
                    model.Town = result.AddressInfo.Town;
                    model.Region = result.AddressInfo.Region;
                }
                merchantList.Add(model);
            }

            return merchantList;
        }

        public async Task<List<Operator>> GetOperators(Guid estateId, CancellationToken cancellationToken){
            EstateManagementGenericContext? context = await this.ContextFactory.GetContext(estateId, ReportingManager.ConnectionStringIdentifier, cancellationToken);

            var operators = await context.EstateOperators.Select(o => new Operator{
                                                                                      Name = o.Name,
                                                                                      EstateReportingId = o.EstateReportingId,
                                                                                      OperatorId = o.OperatorId
                                                                                  }).ToListAsync(cancellationToken);

            return operators;
        }

        #endregion

        #region Others

        private const String ConnectionStringIdentifier = "EstateReportingReadModel";

        #endregion
    }
}