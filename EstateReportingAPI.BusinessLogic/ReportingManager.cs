using TransactionProcessor.Database.Contexts;
using TransactionProcessor.Database.Entities;
using TransactionProcessor.Database.Entities.Summary;

namespace EstateReportingAPI.BusinessLogic;

using Microsoft.EntityFrameworkCore;
using Models;
using Shared.EntityFramework;
using System;
using System.Linq;
using System.Threading;
using static EstateReportingAPI.BusinessLogic.DatabaseProjections;
using Calendar = Models.Calendar;
using Merchant = Models.Merchant;
using Operator = Models.Operator;

public partial class ReportingManager : IReportingManager {
    private readonly IDbContextResolver<EstateManagementContext> Resolver;


    private Guid Id;
    private static readonly String EstateManagementDatabaseName = "TransactionProcessorReadModel";

    #region Constructors

    public ReportingManager(IDbContextResolver<EstateManagementContext> resolver) {
        this.Resolver = resolver;
    }

    #endregion

    #region Methods

    public async Task<List<Calendar>> GetCalendarComparisonDates(Guid estateId,
                                                                 CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, estateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        DateTime startOfYear = new(DateTime.Now.Year, 1, 1);

        List<TransactionProcessor.Database.Entities.Calendar> entities = context.Calendar.Where(c => c.Date >= startOfYear && c.Date < DateTime.Now.Date.AddDays(-1)).OrderByDescending(d => d.Date).ToList();

        List<Calendar> result = new();
        foreach (TransactionProcessor.Database.Entities.Calendar calendar in entities)
            result.Add(new Calendar {
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
                YearWeekNumber = calendar.YearWeekNumber
            });

        return result;
    }

    public async Task<List<Calendar>> GetCalendarDates(Guid estateId,
                                                       CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, estateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        List<TransactionProcessor.Database.Entities.Calendar> entities = context.Calendar.Where(c => c.Date <= DateTime.Now.Date).ToList();

        List<Calendar> result = new();
        foreach (TransactionProcessor.Database.Entities.Calendar calendar in entities)
            result.Add(new Calendar {
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
                YearWeekNumber = calendar.YearWeekNumber
            });

        return result;
    }

    public async Task<List<Int32>> GetCalendarYears(Guid estateId,
                                                    CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, estateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        List<Int32> years = context.Calendar.Where(c => c.Date <= DateTime.Now.Date).GroupBy(c => c.Year).Select(y => y.Key).ToList();

        return years;
    }

    public async Task<LastSettlement> GetLastSettlement(Guid estateId,
                                                        CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, estateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        DateTime settlementDate = await context.SettlementSummary.Where(s => s.IsCompleted).OrderByDescending(s => s.SettlementDate).Select(s => s.SettlementDate).FirstOrDefaultAsync(cancellationToken);

        IQueryable<LastSettlement> settlements = from settlement in context.SettlementSummary where settlement.SettlementDate == settlementDate group new { settlement.SettlementDate, FeeValue = settlement.FeeValue.GetValueOrDefault(), SalesValue = settlement.SalesValue.GetValueOrDefault(), SalesCount = settlement.SalesCount.GetValueOrDefault() } by settlement.SettlementDate into grouped select new LastSettlement { FeesValue = grouped.Sum(g => g.FeeValue), SalesCount = grouped.Sum(g => g.SalesCount), SalesValue = grouped.Sum(g => g.SalesValue), SettlementDate = grouped.Key };

        return await settlements.SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<MerchantKpi> GetMerchantsTransactionKpis(Guid estateId,
                                                               CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, estateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        Int32 merchantsWithSaleInLastHour = (from m in context.Merchants where m.LastSaleDate == DateTime.Now.Date && m.LastSaleDateTime >= DateTime.Now.AddHours(-1) select m.MerchantReportingId).Count();

        Int32 merchantsWithNoSaleToday = (from m in context.Merchants where m.LastSaleDate == DateTime.Now.Date.AddDays(-1) select m.MerchantReportingId).Count();

        Int32 merchantsWithNoSaleInLast7Days = (from m in context.Merchants where m.LastSaleDate <= DateTime.Now.Date.AddDays(-7) select m.MerchantReportingId).Count();

        MerchantKpi response = new() { MerchantsWithSaleInLastHour = merchantsWithSaleInLastHour, MerchantsWithNoSaleToday = merchantsWithNoSaleToday, MerchantsWithNoSaleInLast7Days = merchantsWithNoSaleInLast7Days };

        return response;
    }

    public async Task<List<Merchant>> GetMerchantsByLastSale(Guid estateId,
                                                             DateTime startDateTime,
                                                             DateTime endDateTime,
                                                             CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, estateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        List<Merchant> response = new();

        var merchants = await context.Merchants.Where(m => m.LastSaleDateTime >= startDateTime && m.LastSaleDateTime <= endDateTime).Select(m => new {
            MerchantReportingId = m.MerchantReportingId,
            EstateReportingId = context.Estates.Single(e => e.EstateId == m.EstateId).EstateReportingId,
            Name = m.Name,
            LastSaleDateTime = m.LastSaleDateTime,
            LastSale = m.LastSaleDate,
            CreatedDateTime = m.CreatedDateTime,
            LastStatement = m.LastStatementGenerated,
            MerchantId = m.MerchantId,
            Reference = m.Reference,
            AddressInfo = context.MerchantAddresses.Where(ma => ma.MerchantId == m.MerchantId).OrderByDescending(ma => ma.CreatedDateTime).Select(ma => new {
                PostCode = ma.PostalCode, Region = ma.Region, Town = ma.Town
                // Add more properties as needed
            }).FirstOrDefault() // Get the first matching MerchantAddress or null
        }).ToListAsync(cancellationToken);

        merchants.ForEach(m => response.Add(new Merchant {
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

    public async Task<List<ResponseCode>> GetResponseCodes(Guid estateId,
                                                           CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, estateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;
        List<ResponseCode> response = new();

        List<ResponseCodes> responseCodes = await context.ResponseCodes.ToListAsync(cancellationToken);

        responseCodes.ForEach(r => response.Add(new ResponseCode { Code = r.ResponseCode, Description = r.Description }));

        return response;
    }

    public async Task<TodaysSales> GetTodaysFailedSales(Guid estateId,
                                                        DateTime comparisonDate,
                                                        String responseCode,
                                                        CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, estateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        List<Decimal> todaysSales = await (from t in context.TodayTransactions where t.IsAuthorised == false && t.TransactionType == "Sale" && t.ResponseCode == responseCode select t.TransactionAmount).ToListAsync(cancellationToken);

        List<Decimal> comparisonSales = await (from t in context.TransactionHistory where t.IsAuthorised == false && t.TransactionType == "Sale" && t.TransactionDate == comparisonDate && t.TransactionTime <= DateTime.Now.TimeOfDay && t.ResponseCode == responseCode select t.TransactionAmount).ToListAsync(cancellationToken);

        TodaysSales response = new() {
            ComparisonSalesCount = comparisonSales.Count,
            ComparisonSalesValue = comparisonSales.Sum(),
            ComparisonAverageSalesValue = this.SafeDivide(comparisonSales.Sum(), comparisonSales.Count),
            TodaysSalesCount = todaysSales.Count,
            TodaysSalesValue = todaysSales.Sum(),
            TodaysAverageSalesValue = this.SafeDivide(todaysSales.Sum(), todaysSales.Count)
        };
        return response;
    }

    public async Task<TodaysSales> GetTodaysSales(Guid estateId,
                                                  Int32 merchantReportingId,
                                                  Int32 operatorReportingId,
                                                  DateTime comparisonDate,
                                                  CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, estateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        IQueryable<TodayTransaction> todaysSales = this.BuildTodaySalesQuery(context);
        IQueryable<TransactionHistory> comparisonSales = this.BuildComparisonSalesQuery(context, comparisonDate);

        todaysSales = todaysSales.ApplyMerchantFilter(merchantReportingId).ApplyOperatorFilter(operatorReportingId);
        comparisonSales = comparisonSales.ApplyMerchantFilter(merchantReportingId).ApplyOperatorFilter(operatorReportingId);

        Decimal todaysSalesValue = await todaysSales.SumAsync(t => t.TransactionAmount, cancellationToken);
        Int32 todaysSalesCount = await todaysSales.CountAsync(cancellationToken);
        Decimal comparisonSalesValue = await comparisonSales.SumAsync(t => t.TransactionAmount, cancellationToken);
        Int32 comparisonSalesCount = await comparisonSales.CountAsync(cancellationToken);

        TodaysSales response = new() {
            ComparisonSalesCount = comparisonSalesCount,
            ComparisonSalesValue = comparisonSalesValue,
            TodaysSalesCount = todaysSalesCount,
            TodaysSalesValue = todaysSalesValue,
            TodaysAverageSalesValue = todaysSalesValue / todaysSalesCount,
            ComparisonAverageSalesValue = comparisonSalesValue / comparisonSalesCount
        };
        return response;
    }

    public async Task<List<TodaysSalesCountByHour>> GetTodaysSalesCountByHour(Guid estateId,
                                                                              Int32 merchantReportingId,
                                                                              Int32 operatorReportingId,
                                                                              DateTime comparisonDate,
                                                                              CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, estateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        IQueryable<TodayTransaction> todaysSales = this.BuildTodaySalesQuery(context);
        IQueryable<TransactionHistory> comparisonSales = this.BuildComparisonSalesQuery(context, comparisonDate);

        todaysSales = todaysSales.ApplyMerchantFilter(merchantReportingId).ApplyOperatorFilter(operatorReportingId);
        comparisonSales = comparisonSales.ApplyMerchantFilter(merchantReportingId).ApplyOperatorFilter(operatorReportingId);

        // First we need to get a value of todays sales
        var todaysSalesByHour = await (from t in todaysSales group t.TransactionAmount by t.Hour into g select new { Hour = g.Key, TotalSalesCount = g.Count() }).ToListAsync(cancellationToken);

        var comparisonSalesByHour = await (from t in comparisonSales group t.TransactionAmount by t.Hour into g select new { Hour = g.Key, TotalSalesCount = g.Count() }).ToListAsync(cancellationToken);

        List<TodaysSalesCountByHour> response = (from today in todaysSalesByHour join comparison in comparisonSalesByHour on today.Hour equals comparison.Hour select new TodaysSalesCountByHour { Hour = today.Hour.Value, TodaysSalesCount = today.TotalSalesCount, ComparisonSalesCount = comparison.TotalSalesCount }).ToList();

        return response;
    }

    public async Task<List<TodaysSalesValueByHour>> GetTodaysSalesValueByHour(Guid estateId,
                                                                              Int32 merchantReportingId,
                                                                              Int32 operatorReportingId,
                                                                              DateTime comparisonDate,
                                                                              CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, estateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        IQueryable<TodayTransaction> todaysSales = this.BuildTodaySalesQuery(context);
        IQueryable<TransactionHistory> comparisonSales = this.BuildComparisonSalesQuery(context, comparisonDate);

        todaysSales = todaysSales.ApplyMerchantFilter(merchantReportingId).ApplyOperatorFilter(operatorReportingId);
        comparisonSales = comparisonSales.ApplyMerchantFilter(merchantReportingId).ApplyOperatorFilter(operatorReportingId);

        // First we need to get a value of todays sales
        var todaysSalesByHour = await (from t in todaysSales group t.TransactionAmount by t.Hour into g select new { Hour = g.Key, TotalSalesValue = g.Sum() }).ToListAsync(cancellationToken);

        var comparisonSalesByHour = await (from t in comparisonSales group t.TransactionAmount by t.Hour into g select new { Hour = g.Key, TotalSalesValue = g.Sum() }).ToListAsync(cancellationToken);

        List<TodaysSalesValueByHour> response = (from today in todaysSalesByHour join comparison in comparisonSalesByHour on today.Hour equals comparison.Hour select new TodaysSalesValueByHour { Hour = today.Hour.Value, TodaysSalesValue = today.TotalSalesValue, ComparisonSalesValue = comparison.TotalSalesValue }).ToList();

        return response;
    }

    private Int32 SafeDivide(Int32 number,
                             Int32 divisor) {
        if (divisor == 0) return number;

        return number / divisor;
    }

    private Decimal SafeDivide(Decimal number,
                               Int32 divisor) {
        if (divisor == 0) return number;

        return number / divisor;
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
                PostCode = ma.PostalCode, Region = ma.Region, Town = ma.Town
                // Add more properties as needed
            }).FirstOrDefault(), // Get the first matching MerchantAddress or null
            EstateReportingId = context.Estates.Single(e => e.EstateId == m.EstateId).EstateReportingId
        });

        List<Merchant> merchantList = new();
        foreach (var result in merchants) {
            Merchant model = new() {
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

    private IQueryable<DatabaseProjections.FeeTransactionProjection> BuildUnsettledFeesQuery(EstateManagementContext context,
                                                                                             DateTime startDate,
                                                                                             DateTime endDate) {
        return from merchantSettlementFee in context.MerchantSettlementFees join transaction in context.Transactions on merchantSettlementFee.TransactionId equals transaction.TransactionId where merchantSettlementFee.FeeCalculatedDateTime.Date >= startDate && merchantSettlementFee.FeeCalculatedDateTime.Date <= endDate select new DatabaseProjections.FeeTransactionProjection { Fee = merchantSettlementFee, Txn = transaction };
    }

    private IQueryable<DatabaseProjections.TransactionSearchProjection> BuildTransactionSearchQuery(EstateManagementContext context,
                                                                                                    DateTime queryDate) {
        return from txn in context.Transactions join merchant in context.Merchants on txn.MerchantId equals merchant.MerchantId join @operator in context.Operators on txn.OperatorId equals @operator.OperatorId join product in context.ContractProducts on txn.ContractProductId equals product.ContractProductId where txn.TransactionDate == queryDate select new DatabaseProjections.TransactionSearchProjection { Transaction = txn, Merchant = merchant, Operator = @operator, Product = product };
    }

    private IQueryable<TodayTransaction> BuildTodaySalesQuery(EstateManagementContext context) {
        return from t in context.TodayTransactions where t.IsAuthorised && t.TransactionType == "Sale" && t.TransactionDate == DateTime.Now.Date && t.TransactionTime <= DateTime.Now.TimeOfDay select t;
    }

    private IQueryable<TransactionHistory> BuildComparisonSalesQuery(EstateManagementContext context,
                                                                     DateTime comparisonDate) {
        return from t in context.TransactionHistory where t.IsAuthorised && t.TransactionType == "Sale" && t.TransactionDate == comparisonDate && t.TransactionTime <= DateTime.Now.TimeOfDay select t;
    }

    private IQueryable<DatabaseProjections.TodaySettlementTransactionProjection> BuildTodaySettlementQuery(EstateManagementContext context,
                                                                                                           DateTime settlementDate) {
        IQueryable<DatabaseProjections.TodaySettlementTransactionProjection> settlementData = from s in context.Settlements join f in context.MerchantSettlementFees on s.SettlementId equals f.SettlementId join t in context.TodayTransactions on f.TransactionId equals t.TransactionId where s.SettlementDate == settlementDate select new DatabaseProjections.TodaySettlementTransactionProjection { Fee = f, Txn = t };
        return settlementData;
    }

    private IQueryable<DatabaseProjections.ComparisonSettlementTransactionProjection> BuildComparisonSettlementQuery(EstateManagementContext context,
                                                                                                                     DateTime settlementDate) {
        IQueryable<DatabaseProjections.ComparisonSettlementTransactionProjection> settlementData = from s in context.Settlements join f in context.MerchantSettlementFees on s.SettlementId equals f.SettlementId join t in context.TransactionHistory on f.TransactionId equals t.TransactionId where s.SettlementDate == settlementDate.Date select new DatabaseProjections.ComparisonSettlementTransactionProjection { Fee = f, Txn = t };
        return settlementData;
    }


    public async Task<List<UnsettledFee>> GetUnsettledFees(Guid estateId,
                                                           DateTime startDate,
                                                           DateTime endDate,
                                                           List<Int32> merchantIds,
                                                           List<Int32> operatorIds,
                                                           List<Int32> productIds,
                                                           GroupByOption? groupByOption,
                                                           CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, estateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        IQueryable<DatabaseProjections.FeeTransactionProjection> query = this.BuildUnsettledFeesQuery(context, startDate, endDate).ApplyMerchantFilter(context, merchantIds).ApplyOperatorFilter(context, operatorIds).ApplyProductFilter(context, productIds);

        // Perform grouping
        IQueryable<UnsettledFee> groupedQuery = groupByOption switch {
            GroupByOption.Merchant => query.ApplyMerchantGrouping(context),
            GroupByOption.Operator => query.ApplyOperatorGrouping(context),
            GroupByOption.Product => query.ApplyProductGrouping(context)
        };
        return await groupedQuery.ToListAsync(cancellationToken);
    }

    public async Task<List<TransactionResult>> TransactionSearch(Guid estateId,
                                                                 TransactionSearchRequest searchRequest,
                                                                 PagingRequest pagingRequest,
                                                                 SortingRequest sortingRequest,
                                                                 CancellationToken cancellationToken) {
        // Base query before any filtering is added
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, estateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        IQueryable<DatabaseProjections.TransactionSearchProjection> mainQuery = this.BuildTransactionSearchQuery(context, searchRequest.QueryDate);

        mainQuery = mainQuery.ApplyFilters(searchRequest);
        mainQuery = mainQuery.ApplySorting(sortingRequest);
        mainQuery = mainQuery.ApplyPagination(pagingRequest);

        // Now build the results
        List<DatabaseProjections.TransactionSearchProjection> queryResults = await mainQuery.ToListAsync(cancellationToken);

        List<TransactionResult> results = new();

        queryResults.ForEach(qr => {
            results.Add(new TransactionResult {
                MerchantReportingId = qr.Merchant.MerchantReportingId,
                ResponseCode = qr.Transaction.ResponseCode,
                IsAuthorised = qr.Transaction.IsAuthorised,
                MerchantName = qr.Merchant.Name,
                OperatorName = qr.Operator.Name,
                OperatorReportingId = qr.Operator.OperatorReportingId,
                Product = qr.Product.ProductName,
                ProductReportingId = qr.Product.ContractProductReportingId,
                ResponseMessage = qr.Transaction.ResponseMessage,
                TransactionDateTime = qr.Transaction.TransactionDateTime,
                TransactionId = qr.Transaction.TransactionId,
                TransactionReportingId = qr.Transaction.TransactionReportingId,
                TransactionSource = qr.Transaction.TransactionSource.ToString(),
                TransactionAmount = qr.Transaction.TransactionAmount
            });
        });

        return results;
    }

    public async Task<TodaysSales> GetMerchantPerformance(Guid estateId,
                                                          DateTime comparisonDate,
                                                          List<Int32> merchantReportingIds,
                                                          CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, estateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        // First we need to get a value of todays sales
        IQueryable<TodayTransaction> todaysSalesQuery = this.BuildTodaySalesQuery(context);
        IQueryable<TransactionHistory> comparisonSalesQuery = this.BuildComparisonSalesQuery(context, comparisonDate);

        todaysSalesQuery = todaysSalesQuery.ApplyMerchantFilter(merchantReportingIds);
        comparisonSalesQuery = comparisonSalesQuery.ApplyMerchantFilter(merchantReportingIds);

        // Build the response
        TodaysSales response = new() { ComparisonSalesCount = await comparisonSalesQuery.CountAsync(cancellationToken), ComparisonSalesValue = await comparisonSalesQuery.SumAsync(t => t.TransactionAmount, cancellationToken), TodaysSalesCount = await todaysSalesQuery.CountAsync(cancellationToken), TodaysSalesValue = await todaysSalesQuery.SumAsync(t => t.TransactionAmount, cancellationToken) };
        response.ComparisonAverageSalesValue = this.SafeDivide(response.ComparisonSalesValue, response.ComparisonSalesCount);
        response.TodaysAverageSalesValue = this.SafeDivide(response.TodaysSalesValue, response.TodaysSalesCount);

        return response;
    }

    public async Task<TodaysSales> GetProductPerformance(Guid estateId,
                                                         DateTime comparisonDate,
                                                         List<Int32> productReportingIds,
                                                         CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, estateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        // First we need to get a value of todays sales
        IQueryable<TodayTransaction> todaysSalesQuery = this.BuildTodaySalesQuery(context);
        IQueryable<TransactionHistory> comparisonSalesQuery = this.BuildComparisonSalesQuery(context, comparisonDate);

        todaysSalesQuery = todaysSalesQuery.ApplyProductFilter(productReportingIds);
        comparisonSalesQuery = comparisonSalesQuery.ApplyProductFilter(productReportingIds);

        TodaysSales response = new() { ComparisonSalesCount = await comparisonSalesQuery.CountAsync(cancellationToken), ComparisonSalesValue = await comparisonSalesQuery.SumAsync(t => t.TransactionAmount, cancellationToken), TodaysSalesCount = await todaysSalesQuery.CountAsync(cancellationToken), TodaysSalesValue = await todaysSalesQuery.SumAsync(t => t.TransactionAmount, cancellationToken) };
        response.ComparisonAverageSalesValue = this.SafeDivide(response.ComparisonSalesValue, response.ComparisonSalesCount);
        response.TodaysAverageSalesValue = this.SafeDivide(response.TodaysSalesValue, response.TodaysSalesCount);

        return response;
    }

    public async Task<TodaysSales> GetOperatorPerformance(Guid estateId,
                                                          DateTime comparisonDate,
                                                          List<Int32> operatorReportingIds,
                                                          CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, estateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        // First we need to get a value of todays sales
        IQueryable<TodayTransaction> todaysSalesQuery = this.BuildTodaySalesQuery(context);
        IQueryable<TransactionHistory> comparisonSalesQuery = this.BuildComparisonSalesQuery(context, comparisonDate);

        todaysSalesQuery = todaysSalesQuery.ApplyOperatorFilter(operatorReportingIds);
        comparisonSalesQuery = comparisonSalesQuery.ApplyOperatorFilter(operatorReportingIds);

        TodaysSales response = new() { ComparisonSalesCount = await comparisonSalesQuery.CountAsync(cancellationToken), ComparisonSalesValue = await comparisonSalesQuery.SumAsync(t => t.TransactionAmount, cancellationToken), TodaysSalesCount = await todaysSalesQuery.CountAsync(cancellationToken), TodaysSalesValue = await todaysSalesQuery.SumAsync(t => t.TransactionAmount, cancellationToken) };
        response.ComparisonAverageSalesValue = this.SafeDivide(response.ComparisonSalesValue, response.ComparisonSalesCount);
        response.TodaysAverageSalesValue = this.SafeDivide(response.TodaysSalesValue, response.TodaysSalesCount);

        return response;
    }

    public async Task<List<Models.TopBottomData>> GetTopBottomData(Guid estateId,
                                                                                TopBottom direction,
                                                                                Int32 resultCount,
                                                                                Dimension dimension,
                                                                                CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, estateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        IQueryable<TodayTransaction> mainQuery = this.BuildTodaySalesQuery(context);

        IQueryable<DatabaseProjections.TopBottomData> topBottomData = dimension switch {
            Dimension.Merchant => mainQuery.ApplyMerchantGrouping(context),
            Dimension.Operator => mainQuery.ApplyOperatorGrouping(context),
            Dimension.Product => mainQuery.ApplyProductGrouping(context)
        };

        topBottomData = direction switch {
            TopBottom.Top => topBottomData.OrderByDescending(g => g.SalesValue),
            _ => topBottomData.OrderBy(g => g.SalesValue)
        };

        List<DatabaseProjections.TopBottomData> queryResult = await topBottomData.Take(resultCount).ToListAsync(cancellationToken);

        List<Models.TopBottomData> results = new();
        queryResult.ForEach(qr => results.Add(new Models.TopBottomData { DimensionName = qr.DimensionName, SalesValue = qr.SalesValue }));
        return results;
    }

    public async Task<TodaysSettlement> GetTodaysSettlement(Guid estateId,
                                                            Int32 merchantReportingId,
                                                            Int32 operatorReportingId,
                                                            DateTime comparisonDate,
                                                            CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, estateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        IQueryable<DatabaseProjections.TodaySettlementTransactionProjection> todaySettlementData = this.BuildTodaySettlementQuery(context, DateTime.Now);
        IQueryable<DatabaseProjections.ComparisonSettlementTransactionProjection> comparisonSettlementData = this.BuildComparisonSettlementQuery(context, comparisonDate);

        todaySettlementData = todaySettlementData.ApplyMerchantFilter(merchantReportingId).ApplyOperatorFilter(operatorReportingId);
        comparisonSettlementData = comparisonSettlementData.ApplyMerchantFilter(merchantReportingId).ApplyOperatorFilter(operatorReportingId);

        DatabaseProjections.SettlementGroupProjection todaySettlement = await this.GetSettlementSummary(todaySettlementData, cancellationToken);
        DatabaseProjections.SettlementGroupProjection comparisonSettlement = await this.GetSettlementSummary(comparisonSettlementData, cancellationToken);

        TodaysSettlement response = new() {
            ComparisonSettlementCount = comparisonSettlement.SettledCount,
            ComparisonSettlementValue = comparisonSettlement.SettledValue,
            ComparisonPendingSettlementCount = comparisonSettlement.UnSettledCount,
            ComparisonPendingSettlementValue = comparisonSettlement.UnSettledValue,
            TodaysSettlementCount = todaySettlement.SettledCount,
            TodaysSettlementValue = todaySettlement.SettledValue,
            TodaysPendingSettlementCount = todaySettlement.UnSettledCount,
            TodaysPendingSettlementValue = todaySettlement.UnSettledValue
        };

        return response;
    }

    private async Task<DatabaseProjections.SettlementGroupProjection> GetSettlementSummary(IQueryable<DatabaseProjections.ComparisonSettlementTransactionProjection> query,
                                                                                           CancellationToken cancellationToken) {
        // Get the settleed fees summary
        SettlementGroupProjection summary = await BuildSettlementSummaryQuery(query).SingleOrDefaultAsync(cancellationToken);

        return new DatabaseProjections.SettlementGroupProjection { SettledCount = summary.SettledCount,
            SettledValue = summary.SettledValue,
            UnSettledCount = summary.UnSettledCount,
            UnSettledValue = summary.UnSettledValue };
    }

    private static IQueryable<SettlementGroupProjection> BuildSettlementSummaryQuery(
        IQueryable<DatabaseProjections.ComparisonSettlementTransactionProjection> query)
    {
        return query
            .GroupBy(_ => 1)
            .Select(g => new SettlementGroupProjection
            {
                SettledCount = g.Count(x => x.Fee.IsSettled),
                SettledValue = g.Where(x => x.Fee.IsSettled).Sum(x => x.Fee.CalculatedValue),
                UnSettledCount = g.Count(x => !x.Fee.IsSettled),
                UnSettledValue = g.Where(x => !x.Fee.IsSettled).Sum(x => x.Fee.CalculatedValue)
            });
    }

    private static IQueryable<SettlementGroupProjection> BuildSettlementSummaryQuery(
        IQueryable<DatabaseProjections.TodaySettlementTransactionProjection> query)
    {
        return query
            .GroupBy(_ => 1)
            .Select(g => new SettlementGroupProjection
            {
                SettledCount = g.Count(x => x.Fee.IsSettled),
                SettledValue = g.Where(x => x.Fee.IsSettled).Sum(x => x.Fee.CalculatedValue),
                UnSettledCount = g.Count(x => !x.Fee.IsSettled),
                UnSettledValue = g.Where(x => !x.Fee.IsSettled).Sum(x => x.Fee.CalculatedValue)
            });
    }

    private async Task<DatabaseProjections.SettlementGroupProjection> GetSettlementSummary(
        IQueryable<DatabaseProjections.TodaySettlementTransactionProjection> query,
        CancellationToken cancellationToken) {

        // Get the settleed fees summary
        SettlementGroupProjection summary = await BuildSettlementSummaryQuery(query).SingleOrDefaultAsync(cancellationToken);

        return new DatabaseProjections.SettlementGroupProjection {
            SettledCount = summary.SettledCount,
            SettledValue = summary.SettledValue,
            UnSettledCount = summary.UnSettledCount,
            UnSettledValue = summary.UnSettledValue
        };
    }

    public async Task<List<Operator>> GetOperators(Guid estateId,
                                                   CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, estateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        List<Operator> operators = await (from o in context.Operators select new Operator { Name = o.Name, EstateReportingId = context.Estates.Single(e => e.EstateId == o.EstateId).EstateReportingId, OperatorId = o.OperatorId, OperatorReportingId = o.OperatorReportingId }).ToListAsync(cancellationToken);

        return operators;
    }

    #endregion

    #region Others

    private const String ConnectionStringIdentifier = "EstateReportingReadModel";

    #endregion
}