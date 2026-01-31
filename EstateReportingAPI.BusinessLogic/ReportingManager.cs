using System.Linq.Expressions;
using EstateReportingAPI.BusinessLogic.Queries;
using SimpleResults;
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
using Calendar = Models.Calendar;
using Merchant = Models.Merchant;

public interface IReportingManager
{
    #region Methods
    Task<List<Calendar>> GetCalendarComparisonDates(CalendarQueries.GetComparisonDatesQuery request, CancellationToken cancellationToken);
    Task<List<Calendar>> GetCalendarDates(CalendarQueries.GetAllDatesQuery request, CancellationToken cancellationToken);
    Task<List<Int32>> GetCalendarYears(CalendarQueries.GetYearsQuery request, CancellationToken cancellationToken);
    Task<List<Merchant>> GetRecentMerchants(MerchantQueries.GetRecentMerchantsQuery request, CancellationToken cancellationToken);
    Task<List<Contract>> GetRecentContracts(ContractQueries.GetRecentContractsQuery request, CancellationToken cancellationToken);
    Task<List<Contract>> GetContracts(ContractQueries.GetContractsQuery request, CancellationToken cancellationToken);
    Task<Contract> GetContract(ContractQueries.GetContractQuery request, CancellationToken cancellationToken);
    Task<TodaysSales> GetTodaysFailedSales(TransactionQueries.TodaysFailedSales request, CancellationToken cancellationToken);
    Task<TodaysSales> GetTodaysSales(TransactionQueries.TodaysSalesQuery request, CancellationToken cancellationToken);

    Task<Estate> GetEstate(EstateQueries.GetEstateQuery request, CancellationToken cancellationToken);
    Task<List<EstateOperator>> GetEstateOperators(EstateQueries.GetEstateOperatorsQuery request, CancellationToken cancellationToken);
    Task<MerchantKpi> GetMerchantsTransactionKpis(MerchantQueries.GetTransactionKpisQuery request, CancellationToken cancellationToken);
    Task<List<Operator>> GetOperators(OperatorQueries.GetOperatorsQuery request, CancellationToken cancellationToken);
    Task<List<Merchant>> GetMerchants(MerchantQueries.GetMerchantsQuery request, CancellationToken cancellationToken);
    Task<Merchant> GetMerchant(MerchantQueries.GetMerchantQuery request, CancellationToken cancellationToken);
    Task<List<MerchantOperator>> GetMerchantOperators(MerchantQueries.GetMerchantOperatorsQuery request, CancellationToken cancellationToken);
    Task<List<MerchantContract>> GetMerchantContracts(MerchantQueries.GetMerchantContractsQuery request, CancellationToken cancellationToken);
    Task<List<MerchantDevice>> GetMerchantDevices(MerchantQueries.GetMerchantDevicesQuery request, CancellationToken cancellationToken);

    Task<Operator> GetOperator(OperatorQueries.GetOperatorQuery request, CancellationToken cancellationToken);

    #endregion
}

public class ReportingManager : IReportingManager {
    private readonly IDbContextResolver<EstateManagementContext> Resolver;
    
    private Guid Id;
    private static readonly String EstateManagementDatabaseName = "TransactionProcessorReadModel";

    #region Constructors

    public ReportingManager(IDbContextResolver<EstateManagementContext> resolver) {
        this.Resolver = resolver;
    }

    #endregion

    #region Methods

    public async Task<List<Calendar>> GetCalendarComparisonDates(CalendarQueries.GetComparisonDatesQuery request,
                                                                 CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        //DateTime startOfYear = new(DateTime.Now.Year, 1, 1);

        //List<TransactionProcessor.Database.Entities.Calendar> entities = context.Calendar.Where(c => c.Date >= startOfYear && c.Date < DateTime.Now.Date.AddDays(-1)).OrderByDescending(d => d.Date).ToList();

        DateTime today = DateTime.Today;
        DateTime startDate = today.AddYears(-1);
        DateTime endDate = today.AddDays(-1); // yesterday

        List<TransactionProcessor.Database.Entities.Calendar> entities =
            context.Calendar
                .Where(c => c.Date >= startDate &&
                            c.Date < endDate)
                .OrderByDescending(c => c.Date)
                .ToList();

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

    public async Task<List<Calendar>> GetCalendarDates(CalendarQueries.GetAllDatesQuery request,
                                                       CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
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

    public async Task<List<Int32>> GetCalendarYears(CalendarQueries.GetYearsQuery request,
                                                    CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        List<Int32> years = context.Calendar.Where(c => c.Date <= DateTime.Now.Date).GroupBy(c => c.Year).Select(y => y.Key).ToList();

        return years;
    }

    public async Task<List<Contract>> GetContracts(ContractQueries.GetContractsQuery request,
                                                   CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;
        
        // Step 1: load contracts with operator name via a left-join (translatable)
        var baseContracts = await (from c in context.Contracts
                                   join o in context.Operators on c.OperatorId equals o.OperatorId into ops
                                   from o in ops.DefaultIfEmpty()
                                   select new
                                   {
                                       c.ContractId,
                                       c.ContractReportingId,
                                       c.Description,
                                       c.EstateId,
                                       c.OperatorId,
                                       OperatorName = o != null ? o.Name : null
                                   })
                                  .OrderByDescending(x => x.Description)
                                  .ToListAsync(cancellationToken);

        if (!baseContracts.Any())
            return new List<Contract>();

        var contractIds = baseContracts.Select(b => b.ContractId).ToList();

        // Step 2: load related products for all contracts in one query
        var products = await context.ContractProducts
                                    .Where(cp => contractIds.Contains(cp.ContractId))
                                    .Select(cp => new
                                    {
                                        cp.ContractProductId,
                                        cp.ContractId,
                                        cp.DisplayText,
                                        cp.ProductName,
                                        cp.ProductType,
                                        cp.Value
                                    })
                                    .ToListAsync(cancellationToken);

        var productIds = products.Select(p => p.ContractProductId).ToList();

        // Step 3: load fees for those products in one query
        var fees = await context.ContractProductTransactionFees
                                .Where(tf => productIds.Contains(tf.ContractProductId))
                                .Select(tf => new
                                {
                                    tf.CalculationType,
                                    tf.ContractProductTransactionFeeId,
                                    tf.FeeType,
                                    tf.Value,
                                    tf.ContractProductId,
                                    tf.Description,
                                    tf.IsEnabled
                                })
                                .ToListAsync(cancellationToken);

        // Assemble the model in memory
        List<Contract> result = baseContracts.Select(b => new Contract
        {
            ContractId = b.ContractId,
            ContractReportingId = b.ContractReportingId,
            Description = b.Description,
            EstateId = b.EstateId,
            OperatorName = b.OperatorName,
            OperatorId = b.OperatorId,
            Products = products
                .Where(p => p.ContractId == b.ContractId)
                .Select(p => new Models.ContractProduct
                {
                    ContractId = p.ContractId,
                    ProductId = p.ContractProductId,
                    DisplayText = p.DisplayText,
                    ProductName = p.ProductName,
                    ProductType = p.ProductType,
                    Value = p.Value,
                    TransactionFees = fees
                        .Where(f => f.ContractProductId == p.ContractProductId && f.IsEnabled)
                        .Select(f => new ContractProductTransactionFee { Description = f.Description,Value = f.Value, CalculationType = f.CalculationType, FeeType = f.FeeType, TransactionFeeId = f.ContractProductTransactionFeeId})
                        .ToList()
                })
                .ToList()
        }).ToList();

        return result;
    }

    public async Task<Contract> GetContract(ContractQueries.GetContractQuery request,
                                                   CancellationToken cancellationToken)
    {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        // Step 1: load contracts with operator name via a left-join (translatable)
        var baseContract = await (from c in context.Contracts
                                   join o in context.Operators on c.OperatorId equals o.OperatorId into ops
                                   from o in ops.DefaultIfEmpty()
                                   where c.ContractId == request.ContractId
                                   select new
                                   {
                                       c.ContractId,
                                       c.ContractReportingId,
                                       c.Description,
                                       c.EstateId,
                                       c.OperatorId,
                                       OperatorName = o != null ? o.Name : null
                                   })
                                  .OrderByDescending(x => x.Description)
                                  .SingleOrDefaultAsync(cancellationToken);

        if (baseContract == null)
            return null;

        // Step 2: load related products for all contracts in one query
        var products = await context.ContractProducts
                                    .Where(cp => cp.ContractId ==  baseContract.ContractId)
                                    .Select(cp => new
                                    {
                                        cp.ContractProductId,
                                        cp.ContractId,
                                        cp.DisplayText,
                                        cp.ProductName,
                                        cp.ProductType,
                                        cp.Value
                                    })
                                    .ToListAsync(cancellationToken);

        var productIds = products.Select(p => p.ContractProductId).ToList();

        // Step 3: load fees for those products in one query
        var fees = await context.ContractProductTransactionFees
                                .Where(tf => productIds.Contains(tf.ContractProductId))
                                .Select(tf => new
                                {
                                    tf.CalculationType,
                                    tf.ContractProductTransactionFeeId,
                                    tf.FeeType,
                                    tf.Value,
                                    tf.ContractProductId,
                                    tf.Description
                                })
                                .ToListAsync(cancellationToken);

        // Assemble the model in memory
        Contract result = new Contract
        {
            ContractId = baseContract.ContractId,
            ContractReportingId = baseContract.ContractReportingId,
            Description = baseContract.Description,
            EstateId = baseContract.EstateId,
            OperatorName = baseContract.OperatorName,
            OperatorId = baseContract.OperatorId,
            Products = products
                .Where(p => p.ContractId == baseContract.ContractId)
                .Select(p => new Models.ContractProduct
                {
                    ContractId = p.ContractId,
                    ProductId = p.ContractProductId,
                    DisplayText = p.DisplayText,
                    ProductName = p.ProductName,
                    ProductType = p.ProductType,
                    Value = p.Value,
                    TransactionFees = fees
                        .Where(f => f.ContractProductId == p.ContractProductId)
                        .Select(f => new ContractProductTransactionFee { Description = f.Description, Value = f.Value, CalculationType = f.CalculationType, FeeType = f.FeeType, TransactionFeeId = f.ContractProductTransactionFeeId })
                        .ToList()
                })
                .ToList()
        };

        return result;
    }

    public async Task<List<Contract>> GetRecentContracts(ContractQueries.GetRecentContractsQuery request,
                                                         CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        var contracts = context.Contracts.Select(c => new Contract(){
            ContractId = c.ContractId,
            ContractReportingId = c.ContractReportingId,
            Description = c.Description,
            EstateId = c.EstateId,
            OperatorName = context.Operators.Where(o => o.OperatorId == c.OperatorId).Select(o => o.Name).FirstOrDefault(),
            OperatorId = c.OperatorId,
        }).OrderByDescending(c => c.Description).Take(3);

        return await contracts.ToListAsync(cancellationToken);
    }

    

    public async Task<TodaysSales> GetTodaysFailedSales(TransactionQueries.TodaysFailedSales request,
                                                        CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        List<Decimal> todaysSales = await (from t in context.TodayTransactions where t.IsAuthorised == false && t.TransactionType == "Sale" && t.ResponseCode == request.ResponseCode select t.TransactionAmount).ToListAsync(cancellationToken);

        List<Decimal> comparisonSales = await (from t in context.TransactionHistory where t.IsAuthorised == false && t.TransactionType == "Sale" && t.TransactionDate == request.ComparisonDate && t.TransactionTime <= DateTime.Now.TimeOfDay && t.ResponseCode == request.ResponseCode select t.TransactionAmount).ToListAsync(cancellationToken);

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

    public async Task<TodaysSales> GetTodaysSales(TransactionQueries.TodaysSalesQuery request,
                                                  CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        IQueryable<TodayTransaction> todaysSales = this.BuildTodaySalesQuery(context);
        IQueryable<TransactionHistory> comparisonSales = this.BuildComparisonSalesQuery(context, request.ComparisonDate);

        todaysSales = todaysSales.ApplyMerchantFilter(request.MerchantReportingId).ApplyOperatorFilter(request.OperatorReportingId);
        comparisonSales = comparisonSales.ApplyMerchantFilter(request.MerchantReportingId).ApplyOperatorFilter(request.OperatorReportingId);

        Decimal todaysSalesValue = await todaysSales.SumAsync(t => t.TransactionAmount, cancellationToken);
        Int32 todaysSalesCount = await todaysSales.CountAsync(cancellationToken);
        Decimal comparisonSalesValue = await comparisonSales.SumAsync(t => t.TransactionAmount, cancellationToken);
        Int32 comparisonSalesCount = await comparisonSales.CountAsync(cancellationToken);

        TodaysSales response = new() {
            ComparisonSalesCount = comparisonSalesCount,
            ComparisonSalesValue = comparisonSalesValue,
            TodaysSalesCount = todaysSalesCount,
            TodaysSalesValue = todaysSalesValue,
            TodaysAverageSalesValue = SafeDivide(todaysSalesValue, todaysSalesCount),
            ComparisonAverageSalesValue = SafeDivide(comparisonSalesValue,comparisonSalesCount)
        };
        return response;
    }

    public async Task<List<EstateOperator>> GetEstateOperators(EstateQueries.GetEstateOperatorsQuery request,
                                                               CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        //var operatorEntities = context.EstateOperators.Where(e => e.EstateId == estateId).AsQueryable();
        //var x = operatorEntities.Join()
        var operatorEntities = await context.EstateOperators
            .Join(
                context.Operators,
                eo => new { eo.OperatorId, eo.EstateId },
                op => new { op.OperatorId, op.EstateId },
                (eo, op) => new
                {
                    EstateOperator = eo,
                    Operator = op
                })
            .Where(e => e.EstateOperator.EstateId == request.EstateId && (e.EstateOperator.IsDeleted ?? false) == false)
            .ToListAsync(cancellationToken);

        List<EstateOperator> operators = new();

        foreach (var operatorEntity in operatorEntities) {
            operators.Add(new EstateOperator() {
                Name = operatorEntity.Operator.Name,
                OperatorId = operatorEntity.EstateOperator.OperatorId
            });
        }

        return operators;
    }

    public async Task<Estate> GetEstate(EstateQueries.GetEstateQuery request,
                                        CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        var estate = await context.Estates.Where(e => e.EstateId == request.EstateId).SingleOrDefaultAsync(cancellationToken);

        // Operators
        var operators = await context.Operators.Where(e => e.EstateId == request.EstateId).ToListAsync(cancellationToken);
        // Users
        var users = await context.EstateSecurityUsers.Where(e => e.EstateId == request.EstateId).ToListAsync(cancellationToken);
        // Merchants
        var merchants = await context.Merchants.Where(e => e.EstateId == request.EstateId).ToListAsync(cancellationToken);
        // Contracts
        var contracts = await context.Contracts.Where(e => e.EstateId == request.EstateId).ToListAsync(cancellationToken);

        Estate result = new()
        {
            EstateId = estate.EstateId,
            EstateName = estate.Name,
            Reference = estate.Reference,
            Operators = operators.Select(o => new Models.EstateOperator
            {
                OperatorId = o.OperatorId,
                Name = o.Name,
            }).ToList(),
            Users = users.Select(u => new Models.EstateUser
            {
                UserId = u.SecurityUserId,
                EmailAddress= u.EmailAddress,
                CreatedDateTime = u.CreatedDateTime
            }).ToList(),
            Merchants = merchants.Select(m => new Models.EstateMerchant
            {
                MerchantId = m.MerchantId,
                Name = m.Name,
                Reference = m.Reference
            }).ToList(),
            Contracts = contracts.Select(c => new Models.EstateContract
            {
                ContractId = c.ContractId,
                Name = c.Description,
            }).ToList()
        };

        return result;
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

    

    public async Task<List<Merchant>> GetRecentMerchants(MerchantQueries.GetRecentMerchantsQuery request,
                                                         CancellationToken cancellationToken)
    {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
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
                ma.AddressLine1, 
                ma.AddressLine2,
                ma.Country,
                ma.PostalCode,
                ma.Region,
                ma.Town
                // Add more properties as needed
            }).FirstOrDefault(), // Get the first matching MerchantAddress or null
            ContactInfo = context.MerchantContacts.Where(mc => mc.MerchantId == m.MerchantId).OrderByDescending(mc => mc.CreatedDateTime).Select(mc => new {
                mc.Name,
                mc.EmailAddress,
                mc.PhoneNumber
            }).FirstOrDefault(), // Get the first matching MerchantContact or null
            EstateReportingId = context.Estates.Single(e => e.EstateId == m.EstateId).EstateReportingId
        }).OrderByDescending(m => m.CreatedDateTime).Take(3);

        List<Merchant> merchantList = new();
        foreach (var result in merchants)
        {
            Merchant model = new()
            {
                MerchantId = result.MerchantId,
                Name = result.Name,
                Reference = result.Reference,
                MerchantReportingId = result.MerchantReportingId,
                CreatedDateTime = result.CreatedDateTime,
            };

            if (result.AddressInfo != null) {
                model.AddressLine1 = result.AddressInfo.AddressLine1;
                model.AddressLine2 = result.AddressInfo.AddressLine2;
                model.Country = result.AddressInfo.Country;
                model.PostCode = result.AddressInfo.PostalCode;
                model.Town = result.AddressInfo.Town;
                model.Region = result.AddressInfo.Region;
            }

            if (result.ContactInfo != null) {
                model.ContactName = result.ContactInfo.Name;
                model.ContactEmail = result.ContactInfo.EmailAddress;
                model.ContactPhone = result.ContactInfo.PhoneNumber;
            }

            merchantList.Add(model);
        }

        return merchantList;
    }

    public async Task<MerchantKpi> GetMerchantsTransactionKpis(MerchantQueries.GetTransactionKpisQuery request,
                                                               CancellationToken cancellationToken)
    {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;
        var merchants = await context.Merchants.Select(m => new { m.Name, m.LastSaleDate, m.LastSaleDateTime }).ToListAsync();
        Int32 merchantsWithSaleInLastHour = (from m in context.Merchants where m.LastSaleDate == DateTime.Now.Date && m.LastSaleDateTime >= DateTime.Now.AddHours(-1) select m.MerchantReportingId).Count();

        Int32 merchantsWithNoSaleToday = (from m in context.Merchants where m.LastSaleDate >= DateTime.Now.Date.AddDays(-7) && m.LastSaleDate <= DateTime.Now.Date.AddDays(-1) select m.MerchantReportingId).Count();

        Int32 merchantsWithNoSaleInLast7Days = (from m in context.Merchants where m.LastSaleDate <= DateTime.Now.Date.AddDays(-7) select m.MerchantReportingId).Count();

        MerchantKpi response = new() { MerchantsWithSaleInLastHour = merchantsWithSaleInLastHour, MerchantsWithNoSaleToday = merchantsWithNoSaleToday, MerchantsWithNoSaleInLast7Days = merchantsWithNoSaleInLast7Days };

        return response;
    }

    public async Task<List<Operator>> GetOperators(OperatorQueries.GetOperatorsQuery request,
                                                   CancellationToken cancellationToken)
    {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        List<Operator> operators = await (from o in context.Operators
            select new Operator
            {
                Name = o.Name,
                EstateReportingId = context.Estates.Single(e => e.EstateId == o.EstateId).EstateReportingId,
                OperatorId = o.OperatorId,
                OperatorReportingId = o.OperatorReportingId,
                RequireCustomMerchantNumber = o.RequireCustomMerchantNumber,
                RequireCustomTerminalNumber = o.RequireCustomTerminalNumber
            }).ToListAsync(cancellationToken);

        return operators;
    }

    public async Task<Operator> GetOperator(OperatorQueries.GetOperatorQuery request,
                                                   CancellationToken cancellationToken)
    {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        Operator @operator = await (from o in context.Operators
                                          where o.OperatorId == request.OperatorId
                                          select new Operator {
                                              Name = o.Name, 
                                              EstateReportingId = context.Estates.Single(e => e.EstateId == o.EstateId).EstateReportingId, 
                                              OperatorId = o.OperatorId, 
                                              OperatorReportingId = o.OperatorReportingId,
                                              RequireCustomMerchantNumber = o.RequireCustomMerchantNumber,
                                              RequireCustomTerminalNumber = o.RequireCustomTerminalNumber
                                          }).SingleOrDefaultAsync(cancellationToken);

        return @operator;
    }

    public async Task<List<Merchant>> GetMerchants(MerchantQueries.GetMerchantsQuery request,
                                                   CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;
        

        var merchantWithAddresses = context.Merchants
         .Where(m => m.EstateId == request.EstateId)
         .GroupJoin(
             context.MerchantAddresses,
             m => m.MerchantId,
             a => a.MerchantId,
             (m, addresses) => new
             {
                 Merchant = m,
                 Address = addresses.First()
             })
         .AsQueryable();

        // Now apply the other filters from request.QueryOptions
        if (String.IsNullOrEmpty(request.QueryOptions.Name) == false) {
            merchantWithAddresses = merchantWithAddresses.Where(m => m.Merchant.Name.Contains(request.QueryOptions.Name)).AsQueryable();
        }

        if (String.IsNullOrEmpty(request.QueryOptions.Reference) == false)
        {
            merchantWithAddresses = merchantWithAddresses.Where(m => m.Merchant.Reference == request.QueryOptions.Reference).AsQueryable();
        }

        if (request.QueryOptions.SettlementSchedule > 0)
        {
            merchantWithAddresses = merchantWithAddresses.Where(m => m.Merchant.SettlementSchedule == request.QueryOptions.SettlementSchedule).AsQueryable();
        }

        if (String.IsNullOrEmpty(request.QueryOptions.Region) == false)
        {
            merchantWithAddresses = merchantWithAddresses.Where(m => m.Address.Region.Contains(request.QueryOptions.Region)).AsQueryable();
        }

        if (String.IsNullOrEmpty(request.QueryOptions.PostCode) == false)
        {
            merchantWithAddresses = merchantWithAddresses.Where(m => m.Address.PostalCode == request.QueryOptions.PostCode).AsQueryable();
        }

        // Ok now enumerate the results
        var queryResults = await merchantWithAddresses.ToListAsync(cancellationToken);
        List<Merchant> merchants = new();
        foreach (var queryResult in queryResults) {
            merchants.Add(new Merchant {
                Balance = 0,
                CreatedDateTime = queryResult.Merchant.CreatedDateTime,
                Name = queryResult.Merchant.Name,
                Region = queryResult.Address.Region,
                Reference = queryResult.Merchant.Reference,
                PostCode = queryResult.Address.PostalCode,
                SettlementSchedule = queryResult.Merchant.SettlementSchedule,
                MerchantId = queryResult.Merchant.MerchantId,
                AddressId = queryResult.Address.AddressId,
                AddressLine1 = queryResult.Address.AddressLine1,
                AddressLine2 = queryResult.Address.AddressLine2,
                Town = queryResult.Address.Town,
                Country = queryResult.Address.Country
            });
        }

        return merchants;
    }

    public async Task<Merchant> GetMerchant(MerchantQueries.GetMerchantQuery request,
                                                  CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;


        var merchant = await context.Merchants.Select(m => new {
            MerchantReportingId = m.MerchantReportingId,
            Name = m.Name,
            LastSaleDateTime = m.LastSaleDateTime,
            LastSale = m.LastSaleDate,
            CreatedDateTime = m.CreatedDateTime,
            LastStatement = m.LastStatementGenerated,
            MerchantId = m.MerchantId,
            Reference = m.Reference,
            m.SettlementSchedule,
            AddressInfo = context.MerchantAddresses.Where(ma => ma.MerchantId == m.MerchantId).OrderByDescending(ma => ma.CreatedDateTime).Select(ma => new {
                ma.AddressId,
                ma.AddressLine1,
                ma.AddressLine2,
                ma.Country,
                ma.PostalCode,
                ma.Region,
                ma.Town
                // Add more properties as needed
            }).FirstOrDefault(), // Get the first matching MerchantAddress or null
            ContactInfo = context.MerchantContacts.Where(mc => mc.MerchantId == m.MerchantId).OrderByDescending(mc => mc.CreatedDateTime).Select(mc => new {
                mc.ContactId,
                mc.Name,
                mc.EmailAddress,
                mc.PhoneNumber
            }).FirstOrDefault(), // Get the first matching MerchantContact or null
            EstateReportingId = context.Estates.Single(e => e.EstateId == m.EstateId).EstateReportingId
        }).Where(m => m.MerchantId == request.MerchantId).SingleOrDefaultAsync(cancellationToken);

        if (merchant == null)
            return null;

        // Ok now enumerate the results
        Merchant result = new Merchant
            {
                Balance = 0,
                CreatedDateTime = merchant.CreatedDateTime,
                Name = merchant.Name,
                Reference = merchant.Reference,
                MerchantId = merchant.MerchantId,
                MerchantReportingId = merchant.MerchantReportingId,
                SettlementSchedule = merchant.SettlementSchedule,
                AddressId = merchant.AddressInfo.AddressId,
                AddressLine1 = merchant.AddressInfo.AddressLine1,
                AddressLine2 = merchant.AddressInfo.AddressLine2,
                Town = merchant.AddressInfo.Town,
                Region = merchant.AddressInfo.Region,
                PostCode = merchant.AddressInfo.PostalCode,
                Country = merchant.AddressInfo.Country,
                ContactId = merchant.ContactInfo.ContactId,
                ContactName = merchant.ContactInfo.Name,
                ContactEmail = merchant.ContactInfo.EmailAddress,
                ContactPhone = merchant.ContactInfo.PhoneNumber
            };
        
        return result;
    }

    public async Task<List<MerchantOperator>> GetMerchantOperators(MerchantQueries.GetMerchantOperatorsQuery request,
                                                                   CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        List<TransactionProcessor.Database.Entities.MerchantOperator> merchantOperators = await context.MerchantOperators.Where(mo => mo.MerchantId == request.MerchantId && mo.IsDeleted == false)
            .ToListAsync(cancellationToken);

        List<MerchantOperator> result = new();
        foreach (TransactionProcessor.Database.Entities.MerchantOperator merchantOperator in merchantOperators) {
            result.Add(new MerchantOperator {
                OperatorId = merchantOperator.OperatorId,
                IsDeleted = merchantOperator.IsDeleted,
                MerchantId = merchantOperator.MerchantId,
                MerchantNumber = merchantOperator.MerchantNumber,
                OperatorName = merchantOperator.Name,
                TerminalNumber = merchantOperator.TerminalNumber
            });
        }

        return result;
    }

    public async Task<List<MerchantContract>> GetMerchantContracts(MerchantQueries.GetMerchantContractsQuery request,
                                                                   CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        var merchantContracts = await context.MerchantContracts.Where(mo => mo.MerchantId == request.MerchantId && mo.IsDeleted == false)
            .Select(mc => new {
                mc.ContractId,
                mc.IsDeleted,
                mc.MerchantId,
                    ContractInfo = context.Contracts.Where(c => c.ContractId == mc.ContractId).Select(ma => new {
                    ma.Description,
                    OperatorName = context.Operators.Where(o => o.OperatorId == ma.OperatorId).Select(p => p.Name).Single(),
                            Products = context.ContractProducts.Where(cp => cp.ContractId == mc.ContractId).Select(cp => new {
                            cp.DisplayText,
                            cp.ProductName,
                            cp.ContractProductId,
                            cp.ProductType,
                            cp.Value
                    }).ToList()
                    // Add more properties as needed
                }).SingleOrDefault()
            })
            .ToListAsync(cancellationToken);

        List<MerchantContract> result = new();
        foreach (var merchantContract in merchantContracts)
        {
            var c = new MerchantContract
            {
                ContractId = merchantContract.ContractId,
                ContractName = merchantContract.ContractInfo.Description,
                IsDeleted = merchantContract.IsDeleted,
                MerchantId = merchantContract.MerchantId,
                OperatorName = merchantContract.ContractInfo.OperatorName,
                ContractProducts = new List<MerchantContractProduct>()
            };

            foreach (var product in merchantContract.ContractInfo.Products) {
                c.ContractProducts.Add(new MerchantContractProduct {
                    ContractId = merchantContract.ContractId,
                    DisplayText = product.DisplayText,
                    MerchantId = merchantContract.MerchantId,
                    ProductName = product.ProductName,
                    ProductId = product.ContractProductId,
                    ProductType = product.ProductType,
                    Value = product.Value
                });
            }

            result.Add(c);
        }

        return result;
    }

    public async Task<List<MerchantDevice>> GetMerchantDevices(MerchantQueries.GetMerchantDevicesQuery request,
                                                               CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        List<TransactionProcessor.Database.Entities.MerchantDevice> merchantDevices = await context.MerchantDevices.Where(mo => mo.MerchantId == request.MerchantId).ToListAsync(cancellationToken);

        List<MerchantDevice> result = new();
        foreach (TransactionProcessor.Database.Entities.MerchantDevice merchantDevice in merchantDevices)
        {
            result.Add(new MerchantDevice
            {
                DeviceId = merchantDevice.DeviceId,
                DeviceIdentifier = merchantDevice.DeviceIdentifier,
                IsDeleted = false,
                MerchantId = merchantDevice.MerchantId
            });
        }

        return result;
    }

    private IQueryable<TodayTransaction> BuildTodaySalesQuery(EstateManagementContext context) {
        return from t in context.TodayTransactions where t.IsAuthorised && t.TransactionType == "Sale" && t.TransactionDate == DateTime.Now.Date && t.TransactionTime <= DateTime.Now.TimeOfDay select t;
    }

    private IQueryable<TransactionHistory> BuildComparisonSalesQuery(EstateManagementContext context,
                                                                     DateTime comparisonDate) {
        return from t in context.TransactionHistory where t.IsAuthorised && t.TransactionType == "Sale" && t.TransactionDate == comparisonDate && t.TransactionTime <= DateTime.Now.TimeOfDay select t;
    }

    


    
    
   

    #endregion
}
