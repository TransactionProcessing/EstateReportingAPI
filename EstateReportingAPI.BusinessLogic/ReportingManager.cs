using EstateReportingAPI.BusinessLogic.Queries;
using SimpleResults;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.DynamicLinq;
using TransactionProcessor.Database.Contexts;
using TransactionProcessor.Database.Entities;
using TransactionProcessor.Database.Entities.Summary;

namespace EstateReportingAPI.BusinessLogic;

using Azure;
using Microsoft.EntityFrameworkCore;
using Models;
using Shared.EntityFramework;
using Shared.Results;
using System;
using System.ClientModel.Primitives;
using System.Linq;
using System.Threading;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using Calendar = Models.Calendar;
using Merchant = Models.Merchant;

public interface IReportingManager
{
    #region Methods
    Task<Result<List<Calendar>>> GetCalendarComparisonDates(CalendarQueries.GetComparisonDatesQuery request, CancellationToken cancellationToken);
    Task<Result<List<Calendar>>> GetCalendarDates(CalendarQueries.GetAllDatesQuery request, CancellationToken cancellationToken);
    Task<Result<List<Int32>>> GetCalendarYears(CalendarQueries.GetYearsQuery request, CancellationToken cancellationToken);
    Task<Result<List<Merchant>>> GetRecentMerchants(MerchantQueries.GetRecentMerchantsQuery request, CancellationToken cancellationToken);
    Task<Result<List<Contract>>> GetRecentContracts(ContractQueries.GetRecentContractsQuery request, CancellationToken cancellationToken);
    Task<Result<List<Contract>>> GetContracts(ContractQueries.GetContractsQuery request, CancellationToken cancellationToken);
    Task<Result<Contract>> GetContract(ContractQueries.GetContractQuery request, CancellationToken cancellationToken);
    Task<Result<TodaysSales>> GetTodaysFailedSales(TransactionQueries.TodaysFailedSales request, CancellationToken cancellationToken);
    Task<Result<TodaysSales>> GetTodaysSales(TransactionQueries.TodaysSalesQuery request, CancellationToken cancellationToken);
    Task<Result<Estate>> GetEstate(EstateQueries.GetEstateQuery request, CancellationToken cancellationToken);
    Task<Result<List<EstateOperator>>> GetEstateOperators(EstateQueries.GetEstateOperatorsQuery request, CancellationToken cancellationToken);
    Task<Result<MerchantKpi>> GetMerchantsTransactionKpis(MerchantQueries.GetTransactionKpisQuery request, CancellationToken cancellationToken);
    Task<Result<List<Operator>>> GetOperators(OperatorQueries.GetOperatorsQuery request, CancellationToken cancellationToken);
    Task<Result<List<Merchant>>> GetMerchants(MerchantQueries.GetMerchantsQuery request, CancellationToken cancellationToken);
    Task<Result<Merchant>> GetMerchant(MerchantQueries.GetMerchantQuery request, CancellationToken cancellationToken);
    Task<Result<List<MerchantOperator>>> GetMerchantOperators(MerchantQueries.GetMerchantOperatorsQuery request, CancellationToken cancellationToken);
    Task<Result<List<MerchantContract>>> GetMerchantContracts(MerchantQueries.GetMerchantContractsQuery request, CancellationToken cancellationToken);
    Task<Result<List<MerchantDevice>>> GetMerchantDevices(MerchantQueries.GetMerchantDevicesQuery request, CancellationToken cancellationToken);
    Task<Result<Operator>> GetOperator(OperatorQueries.GetOperatorQuery request, CancellationToken cancellationToken);
    Task<Result<TransactionDetailReportResponse>> GetTransactionDetailReport(TransactionQueries.TransactionDetailReportQuery request,
                                                                    CancellationToken cancellationToken);
    Task<Result<TransactionSummaryByMerchantResponse>> GetTransactionSummaryByMerchantReport(TransactionQueries.TransactionSummaryByMerchantQuery request,
                                                                             CancellationToken cancellationToken);
    Task<Result<TransactionSummaryByOperatorResponse>> GetTransactionSummaryByOperatorReport(TransactionQueries.TransactionSummaryByOperatorQuery request,
                                                                                 CancellationToken cancellationToken);
    Task<Result<ProductPerformanceResponse>> GetProductPerformanceReport(TransactionQueries.ProductPerformanceQuery request,
                                                                         CancellationToken cancellationToken);

    Task<Result<List<TodaysSalesByHour>>> GetTodaysSalesByHour(TransactionQueries.TodaysSalesByHour request,
                                                               CancellationToken cancellationToken);
    Task<Result<TodaysSettlement>> GetTodaysSettlement(SettlementQueries.TodaysSettlementQuery request,
                                                       CancellationToken cancellationToken);
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

    private static async Task<Result<T>> ExecuteQuerySafeSum<T>(IQueryable query,
                                                                CancellationToken cancellationToken,
                                                                string contextMessage = null) {
        try {
            T item = await query.SumAsync(cancellationToken);
            return Result.Success(item);
        }
        catch (Exception ex) {
            string msg = contextMessage == null ? $"Error executing query: {ex.Message}" : $"{contextMessage}: {ex.Message}";
            return Result.Failure(msg);
        }
    }

    private static async Task<Result<List<T>>> ExecuteQuerySafeToList<T>(IQueryable<T> query,
                                                                         CancellationToken cancellationToken,
                                                                         string contextMessage = null) {
        try {
            List<T> items = await query.ToListAsync(cancellationToken);
            return Result.Success(items);
        }
        catch (Exception ex) {
            string msg = contextMessage == null ? $"Error executing query: {ex.Message}" : $"{contextMessage}: {ex.Message}";
            return Result.Failure(msg);
        }
    }

    private static async Task<Result<T>> ExecuteQuerySafeSingleOrDefault<T>(IQueryable<T> query,
                                                                            CancellationToken cancellationToken,
                                                                            string contextMessage = null) {
        try {
            T item = await query.SingleOrDefaultAsync(cancellationToken);

            if (item == null)
                return Result.NotFound(contextMessage);

            return Result.Success(item);
        }
        catch (Exception ex) {
            string msg = contextMessage == null ? $"Error executing query: {ex.Message}" : $"{contextMessage}: {ex.Message}";
            return Result.Failure(msg);
        }
    }

    public async Task<Result<List<Calendar>>> GetCalendarComparisonDates(CalendarQueries.GetComparisonDatesQuery request,
                                                                         CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        DateTime today = DateTime.Today;
        DateTime startDate = today.AddYears(-1);
        DateTime endDate = today.AddDays(-1); // yesterday

        var result = await ExecuteQuerySafeToList(context.Calendar.Where(c => c.Date >= startDate && c.Date < endDate).OrderByDescending(c => c.Date), cancellationToken, "Error retrieving calendar comparison dates");

        if (result.IsFailed)
            return ResultHelpers.CreateFailure(result);

        var entities = result.Data;

        if (entities.Any() == false)
            return Result.NotFound("No calendar dates found");

        List<Calendar> response = new();
        foreach (TransactionProcessor.Database.Entities.Calendar calendar in entities)
            response.Add(new Calendar {
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

        return Result.Success(response);
    }

    public async Task<Result<List<Calendar>>> GetCalendarDates(CalendarQueries.GetAllDatesQuery request,
                                                               CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;


        var result = await ExecuteQuerySafeToList(context.Calendar.Where(c => c.Date <= DateTime.Now.Date), cancellationToken, "Error retrieving calendar dates");

        if (result.IsFailed)
            return ResultHelpers.CreateFailure(result);

        var entities = result.Data;

        List<Calendar> response = new();
        foreach (TransactionProcessor.Database.Entities.Calendar calendar in entities)
            response.Add(new Calendar {
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

        return Result.Success(response);
    }

    public async Task<Result<List<Int32>>> GetCalendarYears(CalendarQueries.GetYearsQuery request,
                                                            CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        var result = await ExecuteQuerySafeToList(context.Calendar.Where(c => c.Date <= DateTime.Now.Date).GroupBy(c => c.Year).Select(y => y.Key), cancellationToken, "Error retrieving calendar years");

        if (result.IsFailed)
            return ResultHelpers.CreateFailure(result);

        return result;
    }

    public async Task<Result<List<Contract>>> GetContracts(ContractQueries.GetContractsQuery request,
                                                           CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        var baseContractsQuery = (from c in context.Contracts
            join o in context.Operators on c.OperatorId equals o.OperatorId into ops
            from o in ops.DefaultIfEmpty()
            select new ContractBaseData {
                ContractId = c.ContractId,
                ContractReportingId = c.ContractReportingId,
                Description = c.Description,
                EstateId = c.EstateId,
                OperatorId = c.OperatorId,
                OperatorName = o != null ? o.Name : null
            }).OrderByDescending(x => x.Description);

        var baseContractsResult = await ExecuteQuerySafeToList(baseContractsQuery, cancellationToken, "Error retrieving contracts - Step 1");
        if (baseContractsResult.IsFailed)
            return ResultHelpers.CreateFailure(baseContractsResult);

        var baseContracts = baseContractsResult.Data;
        if (!baseContracts.Any())
            return new List<Contract>();

        var contractIds = baseContracts.Select(b => b.ContractId).ToList();
        var productsResult = await LoadProductsAsync(context, contractIds, cancellationToken, "Error retrieving contracts - Step 2");
        if (productsResult.IsFailed)
            return ResultHelpers.CreateFailure(productsResult);

        var productIds = productsResult.Data.Select(p => p.ContractProductId).ToList();
        var feesResult = await LoadFeesAsync(context, productIds, cancellationToken, "Error retrieving contracts - Step 3");
        if (feesResult.IsFailed)
            return ResultHelpers.CreateFailure(feesResult);

        return Result.Success(BuildContracts(baseContracts, productsResult.Data, feesResult.Data));
    }

    public async Task<Result<Contract>> GetContract(ContractQueries.GetContractQuery request,
                                                    CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        // Step 1: load contract with operator name via a left-join (translatable)
        var baseContractQuery = (from c in context.Contracts
        join o in context.Operators on c.OperatorId equals o.OperatorId into ops
        from o in ops.DefaultIfEmpty()
        where c.ContractId == request.ContractId
        select new {
            c.ContractId,
            c.ContractReportingId,
            c.Description,
            c.EstateId,
            c.OperatorId,
            OperatorName = o != null ? o.Name : null
        }).OrderByDescending(x => x.Description);

        var baseContractQueryResult = await ExecuteQuerySafeSingleOrDefault(baseContractQuery, cancellationToken, "Error retrieving contract - Step 1");

        if (baseContractQueryResult.IsFailed)
            return ResultHelpers.CreateFailure(baseContractQueryResult);

        var baseContract = baseContractQueryResult.Data;

        // Steps 2 & 3: load products and fees, then assemble
        var productsResult = await this.LoadProductsWithFees(context, baseContract.ContractId, cancellationToken);

        if (productsResult.IsFailed)
            return ResultHelpers.CreateFailure(productsResult);

        return Result.Success(new Contract {
            ContractId = baseContract.ContractId,
            ContractReportingId = baseContract.ContractReportingId,
            Description = baseContract.Description,
            EstateId = baseContract.EstateId,
            OperatorName = baseContract.OperatorName,
            OperatorId = baseContract.OperatorId,
            Products = productsResult.Data
        });
    }

    private async Task<Result<List<Models.ContractProduct>>> LoadProductsWithFees(EstateManagementContext context,
                                                                                  Guid contractId,
                                                                                  CancellationToken cancellationToken) {
        var productsQuery = context.ContractProducts.Where(cp => cp.ContractId == contractId).Select(cp => new {
            cp.ContractProductId,
            cp.ContractId,
            cp.DisplayText,
            cp.ProductName,
            cp.ProductType,
            cp.Value
        });

        var productsQueryResult = await ExecuteQuerySafeToList(productsQuery, cancellationToken, "Error retrieving contract - Step 2");

        if (productsQueryResult.IsFailed)
            return ResultHelpers.CreateFailure(productsQueryResult);

        var products = productsQueryResult.Data;
        var productIds = products.Select(p => p.ContractProductId).ToList();

        var feesQuery = context.ContractProductTransactionFees.Where(tf => productIds.Contains(tf.ContractProductId)).Select(tf => new {
            tf.CalculationType,
            tf.ContractProductTransactionFeeId,
            tf.FeeType,
            tf.Value,
            tf.ContractProductId,
            tf.Description
        });

        var feesQueryResult = await ExecuteQuerySafeToList(feesQuery, cancellationToken, "Error retrieving contract - Step 3");

        if (feesQueryResult.IsFailed)
            return ResultHelpers.CreateFailure(feesQueryResult);

        var fees = feesQueryResult.Data;
        var feesLookup = fees.ToLookup(f => f.ContractProductId);

        return Result.Success(products.Select(p => new Models.ContractProduct {
            ContractId = p.ContractId,
            ProductId = p.ContractProductId,
            DisplayText = p.DisplayText,
            ProductName = p.ProductName,
            ProductType = p.ProductType,
            Value = p.Value,
            TransactionFees = feesLookup[p.ContractProductId].Select(f => new ContractProductTransactionFee {
                Description = f.Description,
                Value = f.Value,
                CalculationType = f.CalculationType,
                FeeType = f.FeeType,
                TransactionFeeId = f.ContractProductTransactionFeeId
            }).ToList()
        }).ToList());
    }

    public async Task<Result<List<Contract>>> GetRecentContracts(ContractQueries.GetRecentContractsQuery request,
                                                                 CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        IQueryable<Contract> contractsQuery = context.Contracts.Select(c => new Contract() {
            ContractId = c.ContractId,
            ContractReportingId = c.ContractReportingId,
            Description = c.Description,
            EstateId = c.EstateId,
            OperatorName = context.Operators.Where(o => o.OperatorId == c.OperatorId).Select(o => o.Name).FirstOrDefault(),
            OperatorId = c.OperatorId,
        }).OrderByDescending(c => c.Description).Take(3);

        Result<List<Contract>> result = await ExecuteQuerySafeToList(contractsQuery, cancellationToken, "Error retrieving recent contracts");

        if (result.IsFailed)
            return ResultHelpers.CreateFailure(result);

        return result;
    }
    
    public async Task<Result<TodaysSales>> GetTodaysFailedSales(TransactionQueries.TodaysFailedSales request,
                                                                CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        var todaysSalesQuery = (from t in context.TodayTransactions where t.IsAuthorised == false && t.TransactionType == "Sale" && t.ResponseCode == request.ResponseCode select t.TransactionAmount);
        var comparisonSalesQuery = (from t in context.TransactionHistory where t.IsAuthorised == false && t.TransactionType == "Sale" && t.TransactionDate == request.ComparisonDate && t.TransactionTime <= DateTime.Now.TimeOfDay && t.ResponseCode == request.ResponseCode select t.TransactionAmount);

        var todaysSalesQueryResult = await ExecuteQuerySafeToList(todaysSalesQuery, cancellationToken, "Error retrieving todays failed sales");
        if (todaysSalesQueryResult.IsFailed)
            return ResultHelpers.CreateFailure(todaysSalesQueryResult);
        var comparisonSalesQueryResult = await ExecuteQuerySafeToList(comparisonSalesQuery, cancellationToken, "Error retrieving comparison failed sales");
        if (comparisonSalesQueryResult.IsFailed)
            return ResultHelpers.CreateFailure(comparisonSalesQueryResult);
        var todaysSales = todaysSalesQueryResult.Data;
        var comparisonSales = comparisonSalesQueryResult.Data;

        TodaysSales response = new() {
            ComparisonSalesCount = comparisonSales.Count,
            ComparisonSalesValue = comparisonSales.Sum(),
            ComparisonAverageSalesValue = this.SafeDivide(comparisonSales.Sum(), comparisonSales.Count),
            TodaysSalesCount = todaysSales.Count,
            TodaysSalesValue = todaysSales.Sum(),
            TodaysAverageSalesValue = this.SafeDivide(todaysSales.Sum(), todaysSales.Count)
        };
        return Result.Success(response);
    }

    public async Task<Result<TodaysSales>> GetTodaysSales(TransactionQueries.TodaysSalesQuery request,
                                                          CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        IQueryable<TodayTransaction> todaysSales = this.BuildTodaySalesQuery(context);
        IQueryable<TransactionHistory> comparisonSales = this.BuildComparisonSalesQuery(context, request.ComparisonDate);

        todaysSales = todaysSales.ApplyMerchantFilter(request.MerchantReportingId).ApplyOperatorFilter(request.OperatorReportingId);
        comparisonSales = comparisonSales.ApplyMerchantFilter(request.MerchantReportingId).ApplyOperatorFilter(request.OperatorReportingId);

        var todaysSalesQueryResult = await ExecuteQuerySafeToList(todaysSales, cancellationToken, "Error retrieving todays failed sales");
        if (todaysSalesQueryResult.IsFailed)
            return ResultHelpers.CreateFailure(todaysSalesQueryResult);
        var comparisonSalesQueryResult = await ExecuteQuerySafeToList(comparisonSales, cancellationToken, "Error retrieving comparison failed sales");
        if (comparisonSalesQueryResult.IsFailed)
            return ResultHelpers.CreateFailure(comparisonSalesQueryResult);


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
            ComparisonAverageSalesValue = SafeDivide(comparisonSalesValue, comparisonSalesCount)
        };
        return Result.Success(response);
    }

    public async Task<Result<List<EstateOperator>>> GetEstateOperators(EstateQueries.GetEstateOperatorsQuery request,
                                                                       CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;



        var operatorQuery = context.EstateOperators.Join(context.Operators, eo => new { eo.OperatorId, eo.EstateId }, op => new { op.OperatorId, op.EstateId }, (eo,
                                                                                                                                                                 op) => new { EstateOperator = eo, Operator = op }).Where(e => e.EstateOperator.EstateId == request.EstateId && (e.EstateOperator.IsDeleted ?? false) == false);

        var operatorQueryResult = await ExecuteQuerySafeToList(operatorQuery, cancellationToken, "Error retrieving estate operators");
        if (operatorQueryResult.IsFailed)
            return ResultHelpers.CreateFailure(operatorQueryResult);

        var operatorEntities = operatorQueryResult.Data;
        List<EstateOperator> operators = new();

        foreach (var operatorEntity in operatorEntities) {
            operators.Add(new EstateOperator() { Name = operatorEntity.Operator.Name, OperatorId = operatorEntity.EstateOperator.OperatorId });
        }

        return Result.Success(operators);
    }

    public async Task<Result<Estate>> GetEstate(EstateQueries.GetEstateQuery request,
                                                CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        var estateQueryResult = await ExecuteQuerySafeSingleOrDefault(context.Estates.Where(e => e.EstateId == request.EstateId), cancellationToken, "Error retrieving estate");

        if (estateQueryResult.IsFailed)
            return ResultHelpers.CreateFailure(estateQueryResult);
        var estate = estateQueryResult.Data;

        // Operators
        var operatorsListQueryResult = await ExecuteQuerySafeToList(context.Operators.Where(e => e.EstateId == request.EstateId), cancellationToken, "Error retrieving estate operators");
        if (operatorsListQueryResult.IsFailed)
            return ResultHelpers.CreateFailure(operatorsListQueryResult);
        var operators = operatorsListQueryResult.Data;

        // Users
        var usersListQueryResult = await ExecuteQuerySafeToList(context.EstateSecurityUsers.Where(e => e.EstateId == request.EstateId), cancellationToken, "Error retrieving estate users");
        if (usersListQueryResult.IsFailed)
            return ResultHelpers.CreateFailure(usersListQueryResult);
        var users = usersListQueryResult.Data;

        // Merchants
        var merchantsListQueryResult = await ExecuteQuerySafeToList(context.Merchants.Where(e => e.EstateId == request.EstateId), cancellationToken, "Error retrieving merchants");
        if (merchantsListQueryResult.IsFailed)
            return ResultHelpers.CreateFailure(merchantsListQueryResult);
        var merchants = merchantsListQueryResult.Data;

        // Contracts
        var contractsListQueryResult = await ExecuteQuerySafeToList(context.Contracts.Where(e => e.EstateId == request.EstateId), cancellationToken, "Error retrieving contracts");
        if (contractsListQueryResult.IsFailed)
            return ResultHelpers.CreateFailure(contractsListQueryResult);
        var contracts = contractsListQueryResult.Data;

        Estate result = new() {
            EstateId = estate.EstateId,
            EstateName = estate.Name,
            Reference = estate.Reference,
            Operators = operators.Select(o => new Models.EstateOperator { OperatorId = o.OperatorId, Name = o.Name, }).ToList(),
            Users = users.Select(u => new Models.EstateUser { UserId = u.SecurityUserId, EmailAddress = u.EmailAddress, CreatedDateTime = u.CreatedDateTime }).ToList(),
            Merchants = merchants.Select(m => new Models.EstateMerchant { MerchantId = m.MerchantId, Name = m.Name, Reference = m.Reference }).ToList(),
            Contracts = contracts.Select(c => new Models.EstateContract { ContractId = c.ContractId, Name = c.Description, }).ToList()
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
    
    public async Task<Result<List<Merchant>>> GetRecentMerchants(MerchantQueries.GetRecentMerchantsQuery request,
                                                                 CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        var merchantsQuery = context.Merchants.Select(m => new {
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
            ContactInfo = context.MerchantContacts.Where(mc => mc.MerchantId == m.MerchantId).OrderByDescending(mc => mc.CreatedDateTime).Select(mc => new { mc.Name, mc.EmailAddress, mc.PhoneNumber }).FirstOrDefault(), // Get the first matching MerchantContact or null
            EstateReportingId = context.Estates.Single(e => e.EstateId == m.EstateId).EstateReportingId
        }).OrderByDescending(m => m.CreatedDateTime).Take(3);

        var recentMerchantsResult = await ExecuteQuerySafeToList(merchantsQuery, cancellationToken, "Error retrieving recent merchants");
        if (recentMerchantsResult.IsFailed)
            return ResultHelpers.CreateFailure(recentMerchantsResult);

        var recentMerchants = recentMerchantsResult.Data;

        List<Merchant> merchantList = new();
        foreach (var merchant in recentMerchants) {
            Merchant model = new() {
                MerchantId = merchant.MerchantId,
                Name = merchant.Name,
                Reference = merchant.Reference,
                MerchantReportingId = merchant.MerchantReportingId,
                CreatedDateTime = merchant.CreatedDateTime,
            };

            if (merchant.AddressInfo != null) {
                model.AddressLine1 = merchant.AddressInfo.AddressLine1;
                model.AddressLine2 = merchant.AddressInfo.AddressLine2;
                model.Country = merchant.AddressInfo.Country;
                model.PostCode = merchant.AddressInfo.PostalCode;
                model.Town = merchant.AddressInfo.Town;
                model.Region = merchant.AddressInfo.Region;
            }

            if (merchant.ContactInfo != null) {
                model.ContactName = merchant.ContactInfo.Name;
                model.ContactEmail = merchant.ContactInfo.EmailAddress;
                model.ContactPhone = merchant.ContactInfo.PhoneNumber;
            }

            merchantList.Add(model);
        }

        return Result.Success(merchantList);
    }

    public async Task<Result<MerchantKpi>> GetMerchantsTransactionKpis(MerchantQueries.GetTransactionKpisQuery request,
                                                                       CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        var merchantsQuery = context.Merchants.Select(m => new { m.Name, m.LastSaleDate, m.LastSaleDateTime });
        var merchantsQueryResult = await ExecuteQuerySafeToList(merchantsQuery, cancellationToken, "Error retrieving merchants for KPI's");
        if (merchantsQueryResult.IsFailed)
            return ResultHelpers.CreateFailure(merchantsQueryResult);

        var merchants = merchantsQueryResult.Data;

        Int32 merchantsWithSaleInLastHour = (from m in merchants where m.LastSaleDate == DateTime.Now.Date && m.LastSaleDateTime >= DateTime.Now.AddHours(-1) select m).Count();

        Int32 merchantsWithNoSaleToday = (from m in merchants where m.LastSaleDate >= DateTime.Now.Date.AddDays(-7) && m.LastSaleDate <= DateTime.Now.Date.AddDays(-1) select m).Count();

        Int32 merchantsWithNoSaleInLast7Days = (from m in merchants where m.LastSaleDate <= DateTime.Now.Date.AddDays(-7) select m).Count();

        MerchantKpi response = new() { MerchantsWithSaleInLastHour = merchantsWithSaleInLastHour, MerchantsWithNoSaleToday = merchantsWithNoSaleToday, MerchantsWithNoSaleInLast7Days = merchantsWithNoSaleInLast7Days };

        return Result.Success(response);
    }

    public async Task<Result<List<Operator>>> GetOperators(OperatorQueries.GetOperatorsQuery request,
                                                           CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        var operatorQuery = (from o in context.Operators
        select new {
            Name = o.Name,
            EstateReportingId = context.Estates.Single(e => e.EstateId == o.EstateId).EstateReportingId,
            OperatorId = o.OperatorId,
            OperatorReportingId = o.OperatorReportingId,
            RequireCustomMerchantNumber = o.RequireCustomMerchantNumber,
            RequireCustomTerminalNumber = o.RequireCustomTerminalNumber
        });

        var operatorResult = await ExecuteQuerySafeToList(operatorQuery, cancellationToken, "Error retrieving operator");

        if (operatorResult.IsFailed)
            return ResultHelpers.CreateFailure(operatorResult);

        List<Operator> operators = new List<Operator>();
        foreach (var op in operatorResult.Data) {
            operators.Add(new Operator {
                Name = op.Name,
                EstateReportingId = op.EstateReportingId,
                OperatorId = op.OperatorId,
                OperatorReportingId = op.OperatorReportingId,
                RequireCustomMerchantNumber = op.RequireCustomMerchantNumber,
                RequireCustomTerminalNumber = op.RequireCustomTerminalNumber
            });
        }

        return Result.Success(operators);
    }

    public async Task<Result<Operator>> GetOperator(OperatorQueries.GetOperatorQuery request,
                                                    CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        var operatorQuery = (from o in context.Operators
        where o.OperatorId == request.OperatorId
        select new {
            Name = o.Name,
            EstateReportingId = context.Estates.Single(e => e.EstateId == o.EstateId).EstateReportingId,
            OperatorId = o.OperatorId,
            OperatorReportingId = o.OperatorReportingId,
            RequireCustomMerchantNumber = o.RequireCustomMerchantNumber,
            RequireCustomTerminalNumber = o.RequireCustomTerminalNumber
        });

        var operatorResult = await ExecuteQuerySafeSingleOrDefault(operatorQuery, cancellationToken, "Error retrieving operator");

        if (operatorResult.IsFailed)
            return ResultHelpers.CreateFailure(operatorResult);

        var @operator = new Operator {
            Name = operatorResult.Data.Name,
            EstateReportingId = operatorResult.Data.EstateReportingId,
            OperatorId = operatorResult.Data.OperatorId,
            OperatorReportingId = operatorResult.Data.OperatorReportingId,
            RequireCustomMerchantNumber = operatorResult.Data.RequireCustomMerchantNumber,
            RequireCustomTerminalNumber = operatorResult.Data.RequireCustomTerminalNumber
        };

        return Result.Success(@operator);
    }

    public async Task<Result<TransactionDetailReportResponse>> GetTransactionDetailReport(TransactionQueries.TransactionDetailReportQuery request,
                                                                                          CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        var query = ApplyTransactionDetailFilters(BuildTransactionDetailBaseQuery(context, request.Request), request.Request);
        var queryResult = await ExecuteQuerySafeToList(query, cancellationToken, "Error retrieving transaction details report");

        if (queryResult.IsFailed)
            return ResultHelpers.CreateFailure(queryResult);

        var queryResults = queryResult.Data;

        if (queryResults.Any() == false)
            return new TransactionDetailReportResponse { Summary = new TransactionDetailSummary(), Transactions = new List<TransactionDetail>() };

        return Result.Success(MapToTransactionDetailResponse(queryResults));
    }

    private static IQueryable<TransactionDetailQueryResult> BuildTransactionDetailBaseQuery(EstateManagementContext context,
                                                                                             TransactionDetailReportRequest request) {
        return from t in context.Transactions
            join cp in context.ContractProducts on new { t.ContractProductId, t.ContractId } equals new { cp.ContractProductId, cp.ContractId }
            join m in context.Merchants on t.MerchantId equals m.MerchantId
            join o in context.Operators on t.OperatorId equals o.OperatorId
            join msf in context.MerchantSettlementFees on t.TransactionId equals msf.TransactionId into msfJoin
            from msf in msfJoin.DefaultIfEmpty()
            // left join Settlements (msf may be null)
            join s in context.Settlements on msf.SettlementId equals s.SettlementId into sJoin
            from s in sJoin.DefaultIfEmpty()
            where t.TransactionType != "Logon" && t.TransactionDate >= request.StartDate && t.TransactionDate <= request.EndDate
            select new TransactionDetailQueryResult {
                TransactionId = t.TransactionId,
                TransactionDateTime = t.TransactionDateTime,
                MerchantId = m.MerchantId,
                MerchantReportingId = m.MerchantReportingId,
                MerchantName = m.Name,
                OperatorId = o.OperatorId,
                OperatorReportingId = o.OperatorReportingId,
                OperatorName = o.Name,
                ProductName = cp.ProductName,
                ContractProductId = cp.ContractProductId,
                ContractProductReportingId = cp.ContractProductReportingId,
                TransactionType = t.TransactionType,
                Status = t.IsAuthorised ? "Authorised" : "Declined",
                Value = t.TransactionAmount,
                FeeValue = msf != null ? msf.FeeValue : 0m,
                SettlementId = s != null ? s.SettlementId : Guid.Empty,
            };
    }

    private static IQueryable<TransactionDetailQueryResult> ApplyTransactionDetailFilters(IQueryable<TransactionDetailQueryResult> query,
                                                                                           TransactionDetailReportRequest request) {
        if (request.Merchants != null && request.Merchants.Any())
            query = query.Where(q => request.Merchants.Contains(q.MerchantReportingId));
        if (request.Products != null && request.Products.Any())
            query = query.Where(q => request.Products.Contains(q.ContractProductReportingId));
        if (request.Operators != null && request.Operators.Any())
            query = query.Where(q => request.Operators.Contains(q.OperatorReportingId));
        return query;
    }

    private static TransactionDetailReportResponse MapToTransactionDetailResponse(List<TransactionDetailQueryResult> queryResults) {
        return new TransactionDetailReportResponse {
            Transactions = queryResults.Select(q => new TransactionDetail {
                Id = q.TransactionId,
                DateTime = q.TransactionDateTime,
                MerchantId = q.MerchantId,
                MerchantReportingId = q.MerchantReportingId,
                Merchant = q.MerchantName,
                OperatorId = q.OperatorId,
                OperatorReportingId = q.OperatorReportingId,
                Operator = q.OperatorName,
                Product = q.ProductName,
                ProductId = q.ContractProductId,
                ProductReportingId = q.ContractProductReportingId,
                Type = q.TransactionType,
                Status = q.Status,
                Value = q.Value,
                TotalFees = q.FeeValue,
                SettlementReference = q.SettlementId.ToString()
            }).ToList(),
            Summary = new TransactionDetailSummary { TransactionCount = queryResults.Count(), TotalValue = queryResults.Sum(q => q.Value), TotalFees = queryResults.Sum(q => q.FeeValue) }
        };
    }

    public async Task<Result<TransactionSummaryByMerchantResponse>> GetTransactionSummaryByMerchantReport(TransactionQueries.TransactionSummaryByMerchantQuery request,
                                                                                                          CancellationToken cancellationToken) {

        TransactionSummaryByMerchantResponse response = null;
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        var query = from t in context.Transactions
            join m in context.Merchants on t.MerchantId equals m.MerchantId
            join o in context.Operators on t.OperatorId equals o.OperatorId
            where t.TransactionType == "Sale" && t.TransactionDate >= request.Request.StartDate && t.TransactionDate <= request.Request.EndDate
            group t by new {
                t.MerchantId,
                m.MerchantReportingId,
                MerchantName = m.Name,
                t.OperatorId,
                o.OperatorReportingId
            }
            into g
            select new {
                g.Key.MerchantId,
                g.Key.MerchantReportingId,
                g.Key.MerchantName,
                g.Key.OperatorId,
                g.Key.OperatorReportingId,
                TotalCount = g.Count(),
                TotalValue = g.Sum(x => x.TransactionAmount),
                AuthorisedCount = g.Sum(x => x.IsAuthorised ? 1 : 0),
                DeclinedCount = g.Sum(x => x.IsAuthorised ? 0 : 1)
            };

        // Now apply the filters
        if (request.Request.Merchants != null && request.Request.Merchants.Any()) {
            query = query.Where(q => request.Request.Merchants.Contains(q.MerchantReportingId));
        }

        if (request.Request.Operators != null && request.Request.Operators.Any()) {
            query = query.Where(q => request.Request.Operators.Contains(q.OperatorReportingId));
        }

        var finalQuery = from x in query
        group x by new { x.MerchantId, x.MerchantReportingId, MerchantName = x.MerchantName, }
        into g
        select new {
            g.Key.MerchantId,
            g.Key.MerchantReportingId,
            g.Key.MerchantName,
            TotalCount = g.Sum(x => x.TotalCount),
            TotalValue = g.Sum(x => x.TotalValue),
            AverageValue = g.Count() > 0 ? g.Sum(x => x.TotalValue) / g.Count() : 0m,
            AuthorisedCount = g.Sum(x => x.AuthorisedCount),
            DeclinedCount = g.Sum(x => x.DeclinedCount),
            AuthorisedPercentage = g.Sum(x => x.TotalCount) > 0 ? (decimal)g.Sum(x => x.AuthorisedCount) / (decimal)g.Sum(x => x.TotalCount) : 0m
        };

        var queryResult = await ExecuteQuerySafeToList(finalQuery, cancellationToken, "Error retrieving transaction summary by merchant report");

        if (queryResult.IsFailed)
            return ResultHelpers.CreateFailure(queryResult);

        // Ok now enumerate the results
        var queryResults = queryResult.Data;

        if (queryResults.Any() == false)
            return new TransactionSummaryByMerchantResponse { Summary = new MerchantDetailSummary(), Merchants = new List<MerchantDetail>() };

        // Now to translate the results
        response = new TransactionSummaryByMerchantResponse {
            Merchants = queryResults.Select(q => new MerchantDetail {
                MerchantId = q.MerchantId,
                MerchantReportingId = q.MerchantReportingId,
                MerchantName = q.MerchantName,
                AuthorisedCount = q.AuthorisedCount,
                AuthorisedPercentage = q.AuthorisedPercentage,
                AverageValue = q.AverageValue,
                DeclinedCount = q.DeclinedCount,
                TotalCount = q.TotalCount,
                TotalValue = q.TotalValue
            }).ToList(),
            Summary = new MerchantDetailSummary { TotalCount = queryResults.Sum(q => q.TotalCount), TotalValue = queryResults.Sum(q => q.TotalValue), AverageValue = this.SafeDivide(queryResults.Sum(q => q.TotalValue), queryResults.Sum(q => q.TotalCount)), TotalMerchants = queryResults.Count() }
        };

        return response;
    }

    public async Task<Result<TransactionSummaryByOperatorResponse>> GetTransactionSummaryByOperatorReport(TransactionQueries.TransactionSummaryByOperatorQuery request,
                                                                                                          CancellationToken cancellationToken) {
        TransactionSummaryByOperatorResponse response = null;
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        var query = from t in context.Transactions
            join m in context.Merchants on t.MerchantId equals m.MerchantId
            join o in context.Operators on t.OperatorId equals o.OperatorId
            where t.TransactionType == "Sale" && t.TransactionDate >= request.Request.StartDate && t.TransactionDate <= request.Request.EndDate
            group t by new {
                t.MerchantId,
                m.MerchantReportingId,
                MerchantName = m.Name,
                t.OperatorId,
                o.OperatorReportingId,
                OperatorName = o.Name
            }
            into g
            select new {
                g.Key.MerchantId,
                g.Key.MerchantReportingId,
                g.Key.MerchantName,
                g.Key.OperatorId,
                g.Key.OperatorReportingId,
                g.Key.OperatorName,
                TotalCount = g.Count(),
                TotalValue = g.Sum(x => x.TransactionAmount),
                AuthorisedCount = g.Sum(x => x.IsAuthorised ? 1 : 0),
                DeclinedCount = g.Sum(x => x.IsAuthorised ? 0 : 1)
            };

        // Now apply the filters
        if (request.Request.Merchants != null && request.Request.Merchants.Any()) {
            query = query.Where(q => request.Request.Merchants.Contains(q.MerchantReportingId));
        }

        if (request.Request.Operators != null && request.Request.Operators.Any()) {
            query = query.Where(q => request.Request.Operators.Contains(q.OperatorReportingId));
        }

        var finalQuery = from x in query
        group x by new { x.OperatorId, x.OperatorReportingId, OperatorName = x.OperatorName, }
        into g
        select new {
            g.Key.OperatorId,
            g.Key.OperatorReportingId,
            g.Key.OperatorName,
            TotalCount = g.Sum(x => x.TotalCount),
            TotalValue = g.Sum(x => x.TotalValue),
            AverageValue = g.Count() > 0 ? g.Sum(x => x.TotalValue) / g.Count() : 0m,
            AuthorisedCount = g.Sum(x => x.AuthorisedCount),
            DeclinedCount = g.Sum(x => x.DeclinedCount),
            AuthorisedPercentage = g.Sum(x => x.TotalCount) > 0 ? (decimal)g.Sum(x => x.AuthorisedCount) / (decimal)g.Sum(x => x.TotalCount) : 0m
        };

        var queryResult = await ExecuteQuerySafeToList(finalQuery, cancellationToken, "Error retrieving transaction summary by operator report");

        if (queryResult.IsFailed)
            return ResultHelpers.CreateFailure(queryResult);

        // Ok now enumerate the results
        var queryResults = queryResult.Data;

        if (queryResults.Any() == false)
            return new TransactionSummaryByOperatorResponse { Summary = new OperatorDetailSummary(), Operators = new List<OperatorDetail>() };

        // Now to translate the results
        response = new TransactionSummaryByOperatorResponse {
            Operators = queryResults.Select(q => new OperatorDetail {
                OperatorId = q.OperatorId,
                OperatorReportingId = q.OperatorReportingId,
                OperatorName = q.OperatorName,
                AuthorisedCount = q.AuthorisedCount,
                AuthorisedPercentage = q.AuthorisedPercentage,
                AverageValue = q.AverageValue,
                DeclinedCount = q.DeclinedCount,
                TotalCount = q.TotalCount,
                TotalValue = q.TotalValue
            }).ToList(),
            Summary = new OperatorDetailSummary { TotalCount = queryResults.Sum(q => q.TotalCount), TotalValue = queryResults.Sum(q => q.TotalValue), AverageValue = this.SafeDivide(queryResults.Sum(q => q.TotalValue), queryResults.Sum(q => q.TotalCount)), TotalOperators = queryResults.Count() }
        };

        return response;
    }

    public async Task<Result<List<Merchant>>> GetMerchants(MerchantQueries.GetMerchantsQuery request,
                                                           CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;


        var merchantWithAddresses = context.Merchants.Where(m => m.EstateId == request.EstateId).GroupJoin(context.MerchantAddresses, m => m.MerchantId, a => a.MerchantId, (m,
                                                                                                                                                                             addresses) => new { Merchant = m, Address = addresses.First() }).AsQueryable();

        // Now apply the other filters from request.QueryOptions
        if (String.IsNullOrEmpty(request.QueryOptions.Name) == false) {
            merchantWithAddresses = merchantWithAddresses.Where(m => m.Merchant.Name.Contains(request.QueryOptions.Name)).AsQueryable();
        }

        if (String.IsNullOrEmpty(request.QueryOptions.Reference) == false) {
            merchantWithAddresses = merchantWithAddresses.Where(m => m.Merchant.Reference == request.QueryOptions.Reference).AsQueryable();
        }

        if (request.QueryOptions.SettlementSchedule > 0) {
            merchantWithAddresses = merchantWithAddresses.Where(m => m.Merchant.SettlementSchedule == request.QueryOptions.SettlementSchedule).AsQueryable();
        }

        if (String.IsNullOrEmpty(request.QueryOptions.Region) == false) {
            merchantWithAddresses = merchantWithAddresses.Where(m => m.Address.Region.Contains(request.QueryOptions.Region)).AsQueryable();
        }

        if (String.IsNullOrEmpty(request.QueryOptions.PostCode) == false) {
            merchantWithAddresses = merchantWithAddresses.Where(m => m.Address.PostalCode == request.QueryOptions.PostCode).AsQueryable();
        }

        var queryResults = await ExecuteQuerySafeToList(merchantWithAddresses, cancellationToken, "Error retrieving merchants");

        if (queryResults.IsFailed)
            return ResultHelpers.CreateFailure(queryResults);

        var merchants = queryResults.Data;

        // Ok now enumerate the results
        List<Merchant> response = new();
        foreach (var queryResult in merchants) {
            response.Add(new Merchant {
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
                Country = queryResult.Address.Country,
                MerchantReportingId = queryResult.Merchant.MerchantReportingId
            });
        }

        return Result.Success(response);
    }

    public async Task<Result<Merchant>> GetMerchant(MerchantQueries.GetMerchantQuery request,
                                                    CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;


        var merchantQuery = context.Merchants.Select(m => new {
            MerchantReportingId = m.MerchantReportingId,
            Name = m.Name,
            CreatedDateTime = m.CreatedDateTime,
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
            }).FirstOrDefault(),
            ContactInfo = context.MerchantContacts.Where(mc => mc.MerchantId == m.MerchantId).OrderByDescending(mc => mc.CreatedDateTime).Select(mc => new { mc.ContactId, mc.Name, mc.EmailAddress, mc.PhoneNumber }).FirstOrDefault()
        }).Where(m => m.MerchantId == request.MerchantId);

        var merchantQueryResult = await ExecuteQuerySafeSingleOrDefault(merchantQuery, cancellationToken, "Error getting merchant");

        if (merchantQueryResult.IsFailed)
            return ResultHelpers.CreateFailure(merchantQueryResult);

        var merchant = merchantQueryResult.Data;

        // Ok now enumerate the results
        Merchant result = new Merchant {
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

        return Result.Success(result);
    }

    public async Task<Result<List<MerchantOperator>>> GetMerchantOperators(MerchantQueries.GetMerchantOperatorsQuery request,
                                                                           CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        var merchantOperatorsQuery = context.MerchantOperators.Where(mo => mo.MerchantId == request.MerchantId && mo.IsDeleted == false);

        var merchantOperatorsQueryResult = await ExecuteQuerySafeToList(merchantOperatorsQuery, cancellationToken, "Error getting merchant devices");

        if (merchantOperatorsQueryResult.IsFailed)
            return ResultHelpers.CreateFailure(merchantOperatorsQueryResult);

        var merchantOperators = merchantOperatorsQueryResult.Data;

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

        return Result.Success(result);
    }

    public async Task<Result<List<MerchantContract>>> GetMerchantContracts(MerchantQueries.GetMerchantContractsQuery request,
                                                                           CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        var merchantContractsQuery = context.MerchantContracts.Where(mo => mo.MerchantId == request.MerchantId && mo.IsDeleted == false).Select(mc => new {
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
        });

        var merchantContractsQueryResult = await ExecuteQuerySafeToList(merchantContractsQuery, cancellationToken, "Error getting merchant devices");

        if (merchantContractsQueryResult.IsFailed)
            return ResultHelpers.CreateFailure(merchantContractsQueryResult);

        var merchantContracts = merchantContractsQueryResult.Data;

        List<MerchantContract> result = new();
        foreach (var merchantContract in merchantContracts) {
            var c = new MerchantContract {
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

        return Result.Success(result);
    }

    public async Task<Result<List<MerchantDevice>>> GetMerchantDevices(MerchantQueries.GetMerchantDevicesQuery request,
                                                                       CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        var merchantDevicesQuery = context.MerchantDevices.Where(mo => mo.MerchantId == request.MerchantId);
        var merchantDevicesQueryResult = await ExecuteQuerySafeToList(merchantDevicesQuery, cancellationToken, "Error getting merchant devices");

        if (merchantDevicesQueryResult.IsFailed)
            return ResultHelpers.CreateFailure(merchantDevicesQueryResult);

        var merchantDevices = merchantDevicesQueryResult.Data;

        List<MerchantDevice> result = new();
        foreach (TransactionProcessor.Database.Entities.MerchantDevice merchantDevice in merchantDevices) {
            result.Add(new MerchantDevice { DeviceId = merchantDevice.DeviceId, DeviceIdentifier = merchantDevice.DeviceIdentifier, IsDeleted = false, MerchantId = merchantDevice.MerchantId });
        }

        return Result.Success(result);
    }

    public async Task<Result<ProductPerformanceResponse>> GetProductPerformanceReport(TransactionQueries.ProductPerformanceQuery request,
                                                                                      CancellationToken cancellationToken) {

        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;
        var grandTotalAmountQuery = (from t in context.Transactions where t.TransactionType == "Sale" && t.TransactionDate >= new DateTime(2025, 12, 21) && t.TransactionDate <= new DateTime(2025, 12, 25) select t.TransactionAmount);

        var grandTotalAmountResult = await ExecuteQuerySafeSum<decimal>(grandTotalAmountQuery, cancellationToken);
        if (grandTotalAmountResult.IsFailed)
            return ResultHelpers.CreateFailure(grandTotalAmountResult);
        var grandTotalAmount = grandTotalAmountResult.Data;

        var query = from t in context.Transactions
            join cp in context.ContractProducts on new { t.ContractProductId, t.ContractId } equals new { cp.ContractProductId, cp.ContractId }
            join c in context.Contracts on t.ContractId equals c.ContractId
            where t.TransactionType == "Sale" && t.TransactionDate >= request.StartDate && t.TransactionDate <= request.EndDate
            group t by new {
                cp.ProductName,
                cp.ContractProductId,
                cp.ContractProductReportingId,
                c.ContractId,
                c.ContractReportingId
            }
            into g
            select new {
                g.Key.ProductName,
                g.Key.ContractProductId,
                g.Key.ContractProductReportingId,
                g.Key.ContractId,
                g.Key.ContractReportingId,
                TransactionCount = g.Count(),
                TotalAmount = g.Sum(x => x.TransactionAmount),
                PercentOfTotalAmount = grandTotalAmount == 0 ? 0 : 100.0m * g.Sum(x => x.TransactionAmount) / grandTotalAmount
            };

        var queryResult = await ExecuteQuerySafeToList(query, cancellationToken);
        if (queryResult.IsFailed)
            return ResultHelpers.CreateFailure(queryResult);
        var queryResults = queryResult.Data;
        if (queryResults.Any() == false)
            return Result.Success(new ProductPerformanceResponse() { Summary = new ProductPerformanceSummary(), ProductDetails = new List<ProductPerformanceDetail>() });

        ProductPerformanceResponse response = new ProductPerformanceResponse() {
            ProductDetails = queryResults.Select(q => new ProductPerformanceDetail {
                ProductName = q.ProductName,
                ProductId = q.ContractProductId,
                ProductReportingId = q.ContractProductReportingId,
                ContractId = q.ContractId,
                ContractReportingId = q.ContractReportingId,
                TransactionCount = q.TransactionCount,
                TransactionValue = q.TotalAmount,
                PercentageOfTotal = q.PercentOfTotalAmount
            }).ToList(),
            Summary = new ProductPerformanceSummary { TotalCount = queryResults.Sum(q => q.TransactionCount), TotalValue = queryResults.Sum(q => q.TotalAmount), AveragePerProduct = this.SafeDivide(queryResults.Sum(q => q.TotalAmount), queryResults.Count), TotalProducts = queryResults.Count() }
        };

        return Result.Success(response);
    }

    public async Task<Result<List<TodaysSalesByHour>>> GetTodaysSalesByHour(TransactionQueries.TodaysSalesByHour request,
                                                                            CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.estateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        IQueryable<TodayTransaction> todaysSales = this.BuildTodaySalesQuery(context);
        IQueryable<TransactionHistory> comparisonSales = this.BuildComparisonSalesQuery(context, request.comparisonDate);

        // First we need to get a value of todays sales
        var todaysSalesByHourQuery = (from t in todaysSales group t.TransactionAmount by t.Hour into g select new { Hour = g.Key, TotalSalesCount = g.Count(), TotalSalesValue = g.Sum() });
        var todaysSalesByHourQueryResult = await ExecuteQuerySafeToList(todaysSalesByHourQuery, cancellationToken);
        if (todaysSalesByHourQueryResult.IsFailed)
            return ResultHelpers.CreateFailure(todaysSalesByHourQueryResult);
        var todaysSalesByHour = todaysSalesByHourQueryResult.Data;

        var comparisonSalesByHourQuery = (from t in comparisonSales group t.TransactionAmount by t.Hour into g select new { Hour = g.Key, TotalSalesCount = g.Count(), TotalSalesValue = g.Sum() });
        var comparisonSalesByHourQueryResult = await ExecuteQuerySafeToList(comparisonSalesByHourQuery, cancellationToken);
        if (comparisonSalesByHourQueryResult.IsFailed)
            return ResultHelpers.CreateFailure(comparisonSalesByHourQueryResult);
        var comparisonSalesByHour = comparisonSalesByHourQueryResult.Data;

        var response = (from today in todaysSalesByHour
        join comparison in comparisonSalesByHour on today.Hour equals comparison.Hour into compGroup
        from comparison in compGroup.DefaultIfEmpty()
        select new TodaysSalesByHour {
            Hour = today.Hour.Value,
            TodaysSalesCount = today.TotalSalesCount,
            TodaysSalesValue = today.TotalSalesValue,
            ComparisonSalesCount = comparison?.TotalSalesCount ?? 0,
            ComparisonSalesValue = comparison?.TotalSalesValue ?? 0
        }).Union(from comparison in comparisonSalesByHour
        join today in todaysSalesByHour on comparison.Hour equals today.Hour into todayGroup
        from today in todayGroup.DefaultIfEmpty()
        where today == null
        select new TodaysSalesByHour {
            Hour = comparison.Hour.Value,
            TodaysSalesCount = 0,
            TodaysSalesValue = 0,
            ComparisonSalesCount = comparison.TotalSalesCount,
            ComparisonSalesValue = comparison.TotalSalesValue
        }).ToList();


        return Result.Success(response);
    }

    public async Task<Result<TodaysSettlement>> GetTodaysSettlement(SettlementQueries.TodaysSettlementQuery request,
                                                                    CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        IQueryable<DatabaseProjections.TodaySettlementTransactionProjection> todaySettlementData = this.BuildTodaySettlementQuery(context, DateTime.Now);
        IQueryable<DatabaseProjections.ComparisonSettlementTransactionProjection> comparisonSettlementData = this.BuildComparisonSettlementQuery(context, request.ComparisonDate);
        
        DatabaseProjections.SettlementGroupProjection todaySettlement = await this.GetSettlementSummary(todaySettlementData, cancellationToken);
        DatabaseProjections.SettlementGroupProjection comparisonSettlement = await this.GetSettlementSummary(comparisonSettlementData, cancellationToken);

        TodaysSettlement response = new()
        {
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
            DatabaseProjections.SettlementGroupProjection summary = await BuildSettlementSummaryQuery(query).SingleOrDefaultAsync(cancellationToken);

            if (summary == null)
                return new DatabaseProjections.SettlementGroupProjection();

        return new DatabaseProjections.SettlementGroupProjection { SettledCount = summary.SettledCount, SettledValue = summary.SettledValue, UnSettledCount = summary.UnSettledCount, UnSettledValue = summary.UnSettledValue };
        }

        private async Task<DatabaseProjections.SettlementGroupProjection> GetSettlementSummary(
            IQueryable<DatabaseProjections.TodaySettlementTransactionProjection> query,
            CancellationToken cancellationToken)
        {
            // Get the settleed fees summary
            DatabaseProjections.SettlementGroupProjection summary = await BuildSettlementSummaryQuery(query).SingleOrDefaultAsync(cancellationToken);

            if (summary == null)
                return new DatabaseProjections.SettlementGroupProjection();

            return new DatabaseProjections.SettlementGroupProjection
            {
                SettledCount = summary.SettledCount,
                SettledValue = summary.SettledValue,
                UnSettledCount = summary.UnSettledCount,
                UnSettledValue = summary.UnSettledValue
            };
        }

    private static IQueryable<DatabaseProjections.SettlementGroupProjection> BuildSettlementSummaryQuery(IQueryable<DatabaseProjections.ComparisonSettlementTransactionProjection> query) {
            return query.GroupBy(_ => 1).Select(g => new DatabaseProjections.SettlementGroupProjection { SettledCount = g.Count(x => x.Fee.IsSettled), SettledValue = g.Where(x => x.Fee.IsSettled).Sum(x => x.Fee.CalculatedValue), UnSettledCount = g.Count(x => !x.Fee.IsSettled), UnSettledValue = g.Where(x => !x.Fee.IsSettled).Sum(x => x.Fee.CalculatedValue) });
        }

        private static IQueryable<DatabaseProjections.SettlementGroupProjection> BuildSettlementSummaryQuery(IQueryable<DatabaseProjections.TodaySettlementTransactionProjection> query) {
            return query.GroupBy(_ => 1).Select(g => new DatabaseProjections.SettlementGroupProjection { SettledCount = g.Count(x => x.Fee.IsSettled), SettledValue = g.Where(x => x.Fee.IsSettled).Sum(x => x.Fee.CalculatedValue), UnSettledCount = g.Count(x => !x.Fee.IsSettled), UnSettledValue = g.Where(x => !x.Fee.IsSettled).Sum(x => x.Fee.CalculatedValue) });
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

        private IQueryable<TodayTransaction> BuildTodaySalesQuery(EstateManagementContext context) {
            return from t in context.TodayTransactions where t.IsAuthorised && t.TransactionType == "Sale" && t.TransactionDate == DateTime.Now.Date && t.TransactionTime <= DateTime.Now.TimeOfDay select t;
        }

        private IQueryable<TransactionHistory> BuildComparisonSalesQuery(EstateManagementContext context,
                                                                         DateTime comparisonDate) {
            return from t in context.TransactionHistory where t.IsAuthorised && t.TransactionType == "Sale" && t.TransactionDate == comparisonDate && t.TransactionTime <= DateTime.Now.TimeOfDay select t;
        }

        private static async Task<Result<List<ContractProductData>>> LoadProductsAsync(EstateManagementContext context,
                                                                                       List<Guid> contractIds,
                                                                                       CancellationToken cancellationToken,
                                                                                       string stepName) {
            var query = context.ContractProducts.Where(cp => contractIds.Contains(cp.ContractId)).Select(cp => new ContractProductData {
                ContractProductId = cp.ContractProductId,
                ContractId = cp.ContractId,
                DisplayText = cp.DisplayText,
                ProductName = cp.ProductName,
                ProductType = cp.ProductType,
                Value = cp.Value
            });
            return await ExecuteQuerySafeToList(query, cancellationToken, stepName);
        }

        private static async Task<Result<List<ContractFeeData>>> LoadFeesAsync(EstateManagementContext context,
                                                                               List<Guid> productIds,
                                                                               CancellationToken cancellationToken,
                                                                               string stepName) {
            var query = context.ContractProductTransactionFees.Where(tf => productIds.Contains(tf.ContractProductId)).Select(tf => new ContractFeeData {
                ContractProductTransactionFeeId = tf.ContractProductTransactionFeeId,
                ContractProductId = tf.ContractProductId,
                Description = tf.Description,
                CalculationType = tf.CalculationType,
                FeeType = tf.FeeType,
                Value = tf.Value,
                IsEnabled = tf.IsEnabled
            });
            return await ExecuteQuerySafeToList(query, cancellationToken, stepName);
        }

        private static List<Contract> BuildContracts(List<ContractBaseData> baseContracts,
                                                     List<ContractProductData> products,
                                                     List<ContractFeeData> fees) {
            return baseContracts.Select(b => new Contract {
                ContractId = b.ContractId,
                ContractReportingId = b.ContractReportingId,
                Description = b.Description,
                EstateId = b.EstateId,
                OperatorName = b.OperatorName,
                OperatorId = b.OperatorId,
                Products = products.Where(p => p.ContractId == b.ContractId).Select(p => new Models.ContractProduct {
                    ContractId = p.ContractId,
                    ProductId = p.ContractProductId,
                    DisplayText = p.DisplayText,
                    ProductName = p.ProductName,
                    ProductType = p.ProductType,
                    Value = p.Value,
                    TransactionFees = fees.Where(f => f.ContractProductId == p.ContractProductId && f.IsEnabled).Select(f => new ContractProductTransactionFee {
                        Description = f.Description,
                        Value = f.Value,
                        CalculationType = f.CalculationType,
                        FeeType = f.FeeType,
                        TransactionFeeId = f.ContractProductTransactionFeeId
                    }).ToList()
                }).ToList()
            }).ToList();
        }

        private sealed class TransactionDetailQueryResult {
            public Guid TransactionId { get; init; }
            public DateTime TransactionDateTime { get; init; }
            public Guid MerchantId { get; init; }
            public int MerchantReportingId { get; init; }
            public string? MerchantName { get; init; }
            public Guid OperatorId { get; init; }
            public int OperatorReportingId { get; init; }
            public string? OperatorName { get; init; }
            public string? ProductName { get; init; }
            public Guid ContractProductId { get; init; }
            public int ContractProductReportingId { get; init; }
            public string? TransactionType { get; init; }
            public string? Status { get; init; }
            public decimal Value { get; init; }
            public decimal FeeValue { get; init; }
            public Guid SettlementId { get; init; }
        }

        private sealed class ContractBaseData {
            public Guid ContractId { get; init; }
            public int ContractReportingId { get; init; }
            public string? Description { get; init; }
            public Guid EstateId { get; init; }
            public Guid OperatorId { get; init; }
            public string? OperatorName { get; init; }
        }

        private sealed class ContractProductData {
            public Guid ContractProductId { get; init; }
            public Guid ContractId { get; init; }
            public string? DisplayText { get; init; }
            public string? ProductName { get; init; }
            public int ProductType { get; init; }
            public decimal? Value { get; init; }
        }

        private sealed class ContractFeeData {
            public Guid ContractProductTransactionFeeId { get; init; }
            public Guid ContractProductId { get; init; }
            public string? Description { get; init; }
            public int CalculationType { get; init; }
            public int FeeType { get; init; }
            public decimal Value { get; init; }
            public bool IsEnabled { get; init; }
        }

        #endregion
    }

