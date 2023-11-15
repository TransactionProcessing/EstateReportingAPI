namespace EstateReportingAPI.BusinessLogic{
    using EstateManagement.Database.Contexts;
    using EstateManagement.Database.Entities;
    using Microsoft.EntityFrameworkCore;
    using Models;
    using Calendar = Models.Calendar;
    using Merchant = Models.Merchant;

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

        public async Task<TodaysSales> GetTodaysSales(Guid estateId, DateTime comparisonDate, CancellationToken cancellationToken){
            EstateManagementGenericContext? context = await this.ContextFactory.GetContext(estateId, ReportingManager.ConnectionStringIdentifier, cancellationToken);

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

            TodaysSales response = new TodaysSales{
                                                      ComparisonSalesCount = comparisonSalesCount,
                                                      ComparisonSalesValue = comparisonSalesValue,
                                                      TodaysSalesCount = todaysSalesCount,
                                                      TodaysSalesValue = todaysSalesValue
                                                  };
            return response;
        }

        public async Task<List<TodaysSalesCountByHour>> GetTodaysSalesCountByHour(Guid estateId, DateTime comparisonDate, CancellationToken cancellationToken){
            EstateManagementGenericContext? context = await this.ContextFactory.GetContext(estateId, ReportingManager.ConnectionStringIdentifier, cancellationToken);

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

        public async Task<List<TodaysSalesValueByHour>> GetTodaysSalesValueByHour(Guid estateId, DateTime comparisonDate, CancellationToken cancellationToken){
            EstateManagementGenericContext? context = await this.ContextFactory.GetContext(estateId, ReportingManager.ConnectionStringIdentifier, cancellationToken);

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

        public async Task<TodaysSettlement> GetTodaysSettlement(Guid estateId, DateTime comparisonDate, CancellationToken cancellationToken){
            EstateManagementGenericContext? context = await this.ContextFactory.GetContext(estateId, ReportingManager.ConnectionStringIdentifier, cancellationToken);

            var todaySettlementData = (from s in context.Settlements
                                       join f in context.MerchantSettlementFees on s.SettlementReportingId equals f.SettlementReportingId
                                       where s.SettlementDate == DateTime.Now.Date
                                       group f by f.IsSettled into grouped
                                       select new
                                              {
                                                  IsSettled = grouped.Key,
                                                  CalculatedValueSum = grouped.Sum(item => item.CalculatedValue),
                                                  CalculatedValueCount = grouped.Count()
                                              }).ToList();

            var comparisonSettlementData = (from s in context.Settlements
                                       join f in context.MerchantSettlementFees on s.SettlementReportingId equals f.SettlementReportingId
                                       where s.SettlementDate == comparisonDate
                                            group f by f.IsSettled into grouped
                                       select new
                                              {
                                                  IsSettled = grouped.Key,
                                                  CalculatedValueSum = grouped.Sum(item => item.CalculatedValue),
                                                  CalculatedValueCount = grouped.Count()
                                              }).ToList();


            TodaysSettlement response = new TodaysSettlement{
                                                                ComparisonSettlementCount = comparisonSettlementData.FirstOrDefault(x => x.IsSettled)?.CalculatedValueCount ?? 0,
                                                                ComparisonSettlementValue = comparisonSettlementData.FirstOrDefault(x => x.IsSettled)?.CalculatedValueSum ?? 0,
                                                                ComparisonPendingSettlementCount = comparisonSettlementData.FirstOrDefault(x => x.IsSettled == false)?.CalculatedValueCount ?? 0,
                                                                ComparisonPendingSettlementValue = comparisonSettlementData.FirstOrDefault(x => x.IsSettled == false)?.CalculatedValueSum ?? 0,
                                                                TodaysSettlementCount = todaySettlementData.FirstOrDefault(x => x.IsSettled)?.CalculatedValueCount ?? 0,
                                                                TodaysSettlementValue = todaySettlementData.FirstOrDefault(x => x.IsSettled)?.CalculatedValueSum ?? 0,
                                                                TodaysPendingSettlementCount = todaySettlementData.FirstOrDefault(x => x.IsSettled == false)?.CalculatedValueCount ?? 0,
                                                                TodaysPendingSettlementValue = todaySettlementData.FirstOrDefault(x => x.IsSettled == false)?.CalculatedValueSum ?? 0
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
                                  t => t.OperatorIdentifier,
                                  oper => oper.Name,
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

        public async Task<TodaysSales> GetOperatorPerformance(Guid estateId, DateTime comparisonDate, List<String> operatorIds, CancellationToken cancellationToken){
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
                todaysSalesQuery = todaysSalesQuery.Where(t => operatorIds.Contains(t.OperatorIdentifier));
                comparisonSalesQuery = comparisonSalesQuery.Where(t => operatorIds.Contains(t.OperatorIdentifier));
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