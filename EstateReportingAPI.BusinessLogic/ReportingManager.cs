namespace EstateReportingAPI.BusinessLogic{
    using EstateManagement.Database.Contexts;
    using EstateManagement.Database.Entities;
    using Microsoft.EntityFrameworkCore;
    using Models;
    using Calendar = Models.Calendar;

    public class ReportingManager : IReportingManager{
        #region Fields

        private readonly Shared.EntityFramework.IDbContextFactory<EstateManagementGenericContext> ContextFactory;

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
                                                      TodaysSalesCount = todaysSalesCount,
                                                      TodaysSalesValue = todaysSalesValue
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

            TodaysSettlement response = new TodaysSettlement{
                                                                ComparisonSettlementCount = comparisonSettlementCount,
                                                                ComparisonSettlementValue = comparisonSettlementValue,
                                                                TodaysSettlementCount = todaysSettlementCount,
                                                                TodaysSettlementValue = todaysSettlementValue
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

        #endregion

        #region Others

        private const String ConnectionStringIdentifier = "EstateReportingReadModel";

        #endregion
    }
}