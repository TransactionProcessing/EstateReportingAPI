using Azure.Core;
using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.Models;
using Microsoft.EntityFrameworkCore;
using Shared.EntityFramework;
using Shared.Results;
using SimpleResults;
using TransactionProcessor.Database.Contexts;
using TransactionProcessor.Database.Entities.Summary;

namespace EstateReportingAPI.BusinessLogic.Services;

public interface ITransactionReportingService
{
    Task<Result<TodaysSales>> GetTodaysFailedSales(TransactionQueries.TodaysFailedSales request, CancellationToken cancellationToken);
    Task<Result<TodaysSales>> GetTodaysSales(TransactionQueries.TodaysSalesQuery request, CancellationToken cancellationToken);
    Task<Result<TransactionDetailReportResponse>> GetTransactionDetailReport(TransactionQueries.TransactionDetailReportQuery request, CancellationToken cancellationToken);
    Task<Result<TransactionSummaryByMerchantResponse>> GetTransactionSummaryByMerchantReport(TransactionQueries.TransactionSummaryByMerchantQuery request, CancellationToken cancellationToken);
    Task<Result<TransactionSummaryByOperatorResponse>> GetTransactionSummaryByOperatorReport(TransactionQueries.TransactionSummaryByOperatorQuery request, CancellationToken cancellationToken);
    Task<Result<ProductPerformanceResponse>> GetProductPerformanceReport(TransactionQueries.ProductPerformanceQuery request, CancellationToken cancellationToken);
    Task<Result<TransactionMixSummaryResponse>> GetTransactionMixSummary(TransactionQueries.TransactionMixSummaryQuery request, CancellationToken cancellationToken);
    Task<Result<GetRecentActivityReceiptReportResponse>> GetRecentActivityReceiptReport(TransactionQueries.GetRecentActivityReceiptReportQuery request, CancellationToken cancellationToken);
    Task<Result<List<TodaysSalesByHour>>> GetTodaysSalesByHour(TransactionQueries.TodaysSalesByHour request, CancellationToken cancellationToken);
    Task<Result<MerchantDailyPerformanceSummaryResponse>> GetMerchantDailyPerformanceSummary(TransactionQueries.MerchantDailyPerformanceSummaryQuery request, CancellationToken cancellationToken);
}

public sealed class TransactionReportingService : ITransactionReportingService
{
    private readonly IDbContextResolver<EstateManagementContext> Resolver;
    private readonly ReportingDatePolicy DatePolicy;
    private const string EstateManagementDatabaseName = "TransactionProcessorReadModel";

    public TransactionReportingService(IDbContextResolver<EstateManagementContext> resolver, ReportingDatePolicy? datePolicy = null)
    {
        Resolver = resolver;
        DatePolicy = datePolicy ?? new ReportingDatePolicy(TimeProvider.System);
}

    public async Task<Result<TodaysSales>> GetTodaysFailedSales(TransactionQueries.TodaysFailedSales request,
                                                                CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        DateTime now = DatePolicy.LocalNow;
        IQueryable<Decimal> todaysSalesQuery = this.BuildTodaysFailedSalesQuery(context, request.ResponseCode, now);
        IQueryable<Decimal> comparisonSalesQuery = this.BuildComparisonFailedSalesQuery(context, request.ComparisonDate, request.ResponseCode, now);

        Result<List<Decimal>> todaysSalesQueryResult = await ReportingQueryExecutor.ToListAsync(todaysSalesQuery, cancellationToken, "Error retrieving todays failed sales");
        if (todaysSalesQueryResult.IsFailed)
            return ResultHelpers.CreateFailure(todaysSalesQueryResult);
        Result<List<Decimal>> comparisonSalesQueryResult = await ReportingQueryExecutor.ToListAsync(comparisonSalesQuery, cancellationToken, "Error retrieving comparison failed sales");
        if (comparisonSalesQueryResult.IsFailed)
            return ResultHelpers.CreateFailure(comparisonSalesQueryResult);
        List<Decimal> todaysSales = todaysSalesQueryResult.Data;
        List<Decimal> comparisonSales = comparisonSalesQueryResult.Data;

        TodaysSales response = new() {
            ComparisonSalesCount = comparisonSales.Count,
            ComparisonSalesValue = comparisonSales.Sum(),
            ComparisonAverageSalesValue = SafeDivide(comparisonSales.Sum(), comparisonSales.Count),
            TodaysSalesCount = todaysSales.Count,
            TodaysSalesValue = todaysSales.Sum(),
            TodaysAverageSalesValue = SafeDivide(todaysSales.Sum(), todaysSales.Count)
        };
        return Result.Success(response);
    }

    public async Task<Result<TodaysSales>> GetTodaysSales(TransactionQueries.TodaysSalesQuery request,
                                                          CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        DateTime now = DatePolicy.LocalNow;
        IQueryable<TodayTransaction> todaysSales = this.BuildTodaySalesQuery(context, now);
        IQueryable<TransactionHistory> comparisonSales = this.BuildComparisonSalesQuery(context, request.ComparisonDate, now);

        todaysSales = todaysSales.ApplyMerchantFilter(request.MerchantReportingId).ApplyOperatorFilter(request.OperatorReportingId);
        comparisonSales = comparisonSales.ApplyMerchantFilter(request.MerchantReportingId).ApplyOperatorFilter(request.OperatorReportingId);

        Result<List<TodayTransaction>> todaysSalesQueryResult = await ReportingQueryExecutor.ToListAsync(todaysSales, cancellationToken, "Error retrieving todays failed sales");
        if (todaysSalesQueryResult.IsFailed)
            return ResultHelpers.CreateFailure(todaysSalesQueryResult);
        Result<List<TransactionHistory>> comparisonSalesQueryResult = await ReportingQueryExecutor.ToListAsync(comparisonSales, cancellationToken, "Error retrieving comparison failed sales");
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

    private Decimal SafeDivide(Decimal number,
                               Int32 divisor) {
        if (divisor == 0) return number;

        return number / divisor;
    }
    public async Task<Result<TransactionDetailReportResponse>> GetTransactionDetailReport(TransactionQueries.TransactionDetailReportQuery request,
                                                                                          CancellationToken cancellationToken) {
        string? validationError = ReportQueryValidation.ValidateDateRange(request.Request.StartDate, request.Request.EndDate)
                                  ?? ReportQueryValidation.ValidatePaging(request.Request.PageNumber, request.Request.PageSize)
                                  ?? ReportQueryValidation.ValidateFilterList("merchants", request.Request.Merchants)
                                  ?? ReportQueryValidation.ValidateFilterList("operators", request.Request.Operators)
                                  ?? ReportQueryValidation.ValidateFilterList("products", request.Request.Products);
        if (validationError != null)
            return Result.Failure(validationError);

        using ResolvedDbContext<EstateManagementContext>? resolvedContext = Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        IQueryable<TransactionDetailQueryResult> query = ApplyTransactionDetailFilters(BuildTransactionDetailBaseQuery(context, request.Request), request.Request);
        IQueryable<TransactionDetailSummaryQueryResult> summaryQuery = query
                                                                       .GroupBy(_ => 1)
                                                                       .Select(g => new TransactionDetailSummaryQueryResult {
                                                                                                                                TransactionCount = g.Count(),
                                                                                                                                TotalValue = g.Sum(q => q.Value),
                                                                                                                                TotalFees = g.Sum(q => q.FeeValue)
                                                                                                                            });
        Result<List<TransactionDetailSummaryQueryResult>> summaryResult = await ReportingQueryExecutor.ToListAsync(summaryQuery, cancellationToken, "Error calculating transaction details report summary");
        if (summaryResult.IsFailed)
            return ResultHelpers.CreateFailure(summaryResult);

        TransactionDetailSummaryQueryResult summary = summaryResult.Data.SingleOrDefault() ?? new TransactionDetailSummaryQueryResult();
        IQueryable<TransactionDetailQueryResult> pageQuery = query
                                                             .OrderByDescending(q => q.TransactionDateTime)
                                                             .ThenByDescending(q => q.TransactionId)
                                                             .Skip((int)((long)(request.Request.PageNumber - 1) * request.Request.PageSize))
                                                             .Take(request.Request.PageSize);
        Result<List<TransactionDetailQueryResult>> queryResult = await ReportingQueryExecutor.ToListAsync(pageQuery, cancellationToken, "Error retrieving transaction details report");

        if (queryResult.IsFailed)
            return ResultHelpers.CreateFailure(queryResult);

        int totalPages = summary.TransactionCount == 0
            ? 0
            : (int)Math.Ceiling((double)summary.TransactionCount / request.Request.PageSize);
        return Result.Success(MapToTransactionDetailResponse(
            queryResult.Data,
            summary,
            new PaginationMetadata {
                PageNumber = request.Request.PageNumber,
                PageSize = request.Request.PageSize,
                TotalItems = summary.TransactionCount,
                TotalPages = totalPages
            }));
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
            where t.TransactionType != "Logon" && t.TransactionDate >= request.StartDate.Date && t.TransactionDate < request.EndDate.Date.AddDays(1)
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
                TransactionNumber = Int32.Parse(t.TransactionNumber)
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

    private static TransactionDetailReportResponse MapToTransactionDetailResponse(List<TransactionDetailQueryResult> queryResults,
                                                                                   TransactionDetailSummaryQueryResult summary,
                                                                                   PaginationMetadata pagination) {
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
                SettlementReference = q.SettlementId.ToString(),
                TransactionNumber = q.TransactionNumber
            }).ToList(),
            Summary = new TransactionDetailSummary { TransactionCount = summary.TransactionCount, TotalValue = summary.TotalValue, TotalFees = summary.TotalFees },
            Pagination = pagination
        };
    }

    internal static Result<String> ValidateRequest(TransactionQueries.TransactionSummaryByMerchantQuery request) {
        string? validationError = ReportQueryValidation.ValidateDateRange(request.Request.StartDate, request.Request.EndDate)
                                  ?? ReportQueryValidation.ValidatePaging(request.Request.PageNumber, request.Request.PageSize)
                                  ?? ReportQueryValidation.ValidateFilterList("merchants", request.Request.Merchants)
                                  ?? ReportQueryValidation.ValidateFilterList("operators", request.Request.Operators);
        if (validationError != null)
            return Result.Failure(validationError);

        return Result.Success();
    }

    public async Task<Result<TransactionSummaryByMerchantResponse>> GetTransactionSummaryByMerchantReport(TransactionQueries.TransactionSummaryByMerchantQuery request,
                                                                                                          CancellationToken cancellationToken) {
        Result<String>? validationError = ValidateRequest(request);
        if (validationError.IsFailed)
            return ResultHelpers.CreateFailure(validationError);

        using ResolvedDbContext<EstateManagementContext>? resolvedContext = Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        IQueryable<MerchantTransactionGroupProjection> query = BuildMerchantTransactionBaseQuery(context, request.Request.StartDate, request.Request.EndDate);

        if (request.Request.Merchants?.Any() == true)
            query = query.Where(q => request.Request.Merchants.Contains(q.MerchantReportingId));

        if (request.Request.Operators?.Any() == true)
            query = query.Where(q => request.Request.Operators.Contains(q.OperatorReportingId));

        IQueryable<MerchantTransactionFinalProjection> finalQuery = BuildMerchantTransactionFinalQuery(query);
        Result<MerchantSummaryReportData> reportDataResult = await LoadMerchantSummaryReportDataAsync(finalQuery, request.Request, cancellationToken);
        if (reportDataResult.IsFailed)
            return ResultHelpers.CreateFailure(reportDataResult);
        MerchantSummaryReportData reportData = reportDataResult.Data;

        TransactionSummaryByMerchantResponse response = MapMerchantSummaryResponse(reportData, request.Request);
        return Result.Success(response);
    }

    private TransactionSummaryByMerchantResponse MapMerchantSummaryResponse(
        MerchantSummaryReportData reportData,
        Models.TransactionSummaryByMerchantRequest request) {
        int totalPages = reportData.TotalMerchants == 0
            ? 0
            : (int)Math.Ceiling((double)reportData.TotalMerchants / request.PageSize);

        return new TransactionSummaryByMerchantResponse {
            Merchants = reportData.Merchants.Select(q => new MerchantDetail {
                MerchantId = q.MerchantId,
                MerchantReportingId = q.MerchantReportingId,
                MerchantName = string.IsNullOrWhiteSpace(q.MerchantName) ? $"Unknown Merchant ({q.MerchantReportingId})" : q.MerchantName,
                AuthorisedCount = q.AuthorisedCount,
                AuthorisedPercentage = q.AuthorisedPercentage,
                AverageValue = q.AverageValue,
                DeclinedCount = q.DeclinedCount,
                TotalCount = q.TotalCount,
                TotalValue = q.TotalValue
            }).ToList(),
            Summary = new MerchantDetailSummary {
                TotalCount = reportData.TotalCount,
                TotalValue = reportData.TotalValue,
                AverageValue = SafeDivide(reportData.TotalValue, reportData.TotalCount),
                TotalMerchants = reportData.TotalMerchants
            },
            Pagination = new PaginationMetadata {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalItems = reportData.TotalMerchants,
                TotalPages = totalPages
            }
        };
    }

    private static async Task<Result<MerchantSummaryReportData>> LoadMerchantSummaryReportDataAsync(
        IQueryable<MerchantTransactionFinalProjection> query,
        Models.TransactionSummaryByMerchantRequest request,
        CancellationToken cancellationToken) {
        Result<Int32> merchantCountResult = await ReportingQueryExecutor.CountAsync(query, cancellationToken, "Error counting transaction summary by merchant report rows");
        if (merchantCountResult.IsFailed)
            return ResultHelpers.CreateFailure(merchantCountResult);

        Result<Int32?> totalCountResult = await ReportingQueryExecutor.SumAsync(query.Select(q => (int?)q.TotalCount), cancellationToken, "Error summing transaction summary by merchant counts");
        if (totalCountResult.IsFailed)
            return ResultHelpers.CreateFailure(totalCountResult);

        Result<Decimal?> totalValueResult = await ReportingQueryExecutor.SumAsync(query.Select(q => (decimal?)q.TotalValue), cancellationToken, "Error summing transaction summary by merchant values");
        if (totalValueResult.IsFailed)
            return ResultHelpers.CreateFailure(totalValueResult);

        Result<List<MerchantTransactionFinalProjection>> merchantResult = await ReportingQueryExecutor.ToListAsync(
            query.OrderBy(q => string.IsNullOrWhiteSpace(q.MerchantName) ? "Unknown Merchant" : q.MerchantName)
                .ThenBy(q => q.MerchantReportingId)
                .ThenBy(q => q.MerchantId)
                .Skip((int)((long)(request.PageNumber - 1) * request.PageSize))
                .Take(request.PageSize),
            cancellationToken,
            "Error retrieving transaction summary by merchant report");
        if (merchantResult.IsFailed)
            return ResultHelpers.CreateFailure(merchantResult);

        return Result.Success(new MerchantSummaryReportData(
            merchantResult.Data,
            merchantCountResult.Data,
            totalCountResult.Data ?? 0,
            totalValueResult.Data ?? 0m));
    }

    private static IQueryable<MerchantTransactionGroupProjection> BuildMerchantTransactionBaseQuery(EstateManagementContext context,
                                                                                                    DateTime startDate,
                                                                                                    DateTime endDate) {
        return from t in context.Transactions
            join m in context.Merchants on t.MerchantId equals m.MerchantId
            join o in context.Operators on t.OperatorId equals o.OperatorId
            where t.TransactionType == "Sale" && t.TransactionDate >= startDate.Date && t.TransactionDate < endDate.Date.AddDays(1)
            group t by new { t.MerchantId, m.MerchantReportingId, MerchantName = m.Name, t.OperatorId, o.OperatorReportingId }
            into g
            select new MerchantTransactionGroupProjection {
                MerchantId = g.Key.MerchantId,
                MerchantReportingId = g.Key.MerchantReportingId,
                MerchantName = g.Key.MerchantName,
                OperatorId = g.Key.OperatorId,
                OperatorReportingId = g.Key.OperatorReportingId,
                TotalCount = g.Count(),
                TotalValue = g.Sum(x => x.TransactionAmount),
                AuthorisedCount = g.Sum(x => x.IsAuthorised ? 1 : 0),
                DeclinedCount = g.Sum(x => x.IsAuthorised ? 0 : 1)
            };
    }

    private static IQueryable<MerchantTransactionFinalProjection> BuildMerchantTransactionFinalQuery(IQueryable<MerchantTransactionGroupProjection> query) {
        return from x in query
            group x by new { x.MerchantId, x.MerchantReportingId, x.MerchantName }
            into g
            select new MerchantTransactionFinalProjection {
                MerchantId = g.Key.MerchantId,
                MerchantReportingId = g.Key.MerchantReportingId,
                MerchantName = g.Key.MerchantName,
                TotalCount = g.Sum(x => x.TotalCount),
                TotalValue = g.Sum(x => x.TotalValue),
                AverageValue = g.Count() > 0 ? g.Sum(x => x.TotalValue) / g.Count() : 0m,
                AuthorisedCount = g.Sum(x => x.AuthorisedCount),
                DeclinedCount = g.Sum(x => x.DeclinedCount),
                AuthorisedPercentage = g.Sum(x => x.TotalCount) > 0 ? (decimal)g.Sum(x => x.AuthorisedCount) / (decimal)g.Sum(x => x.TotalCount) : 0m
            };
    }

    public async Task<Result<TransactionSummaryByOperatorResponse>> GetTransactionSummaryByOperatorReport(TransactionQueries.TransactionSummaryByOperatorQuery request,
                                                                                                          CancellationToken cancellationToken) {
        string? validationError = ReportQueryValidation.ValidateDateRange(request.Request.StartDate, request.Request.EndDate)
                                  ?? ReportQueryValidation.ValidatePaging(request.Request.PageNumber, request.Request.PageSize)
                                  ?? ReportQueryValidation.ValidateFilterList("merchants", request.Request.Merchants)
                                  ?? ReportQueryValidation.ValidateFilterList("operators", request.Request.Operators);
        if (validationError != null)
            return Result.Failure(validationError);

        using ResolvedDbContext<EstateManagementContext>? resolvedContext = Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        IQueryable<OperatorTransactionData> baseQuery = BuildOperatorTransactionBaseQuery(context, request);
        IQueryable<OperatorSummaryData> finalQuery = BuildOperatorFinalSummaryQuery(baseQuery);

        Result<OperatorSummaryReportData> reportDataResult = await LoadOperatorSummaryReportDataAsync(finalQuery, request.Request, cancellationToken);
        if (reportDataResult.IsFailed)
            return ResultHelpers.CreateFailure(reportDataResult);
        OperatorSummaryReportData reportData = reportDataResult.Data;
        int totalOperators = reportData.TotalOperators;
        int totalPages = totalOperators == 0 ? 0 : (int)Math.Ceiling((double)totalOperators / request.Request.PageSize);
        return BuildOperatorSummaryResponse(reportData.Operators, reportData.TotalCount, reportData.TotalValue, totalOperators,
            new PaginationMetadata {
                PageNumber = request.Request.PageNumber,
                PageSize = request.Request.PageSize,
                TotalItems = totalOperators,
                TotalPages = totalPages
            });
    }

    private static async Task<Result<OperatorSummaryReportData>> LoadOperatorSummaryReportDataAsync(
        IQueryable<OperatorSummaryData> query,
        Models.TransactionSummaryByOperatorRequest request,
        CancellationToken cancellationToken) {
        Result<Int32> operatorCountResult = await ReportingQueryExecutor.CountAsync(query, cancellationToken, "Error counting transaction summary by operator report rows");
        if (operatorCountResult.IsFailed)
            return ResultHelpers.CreateFailure(operatorCountResult);

        Result<Int32?> totalCountResult = await ReportingQueryExecutor.SumAsync(query.Select(q => (int?)q.TotalCount), cancellationToken, "Error summing transaction summary by operator counts");
        if (totalCountResult.IsFailed)
            return ResultHelpers.CreateFailure(totalCountResult);

        Result<Decimal?> totalValueResult = await ReportingQueryExecutor.SumAsync(query.Select(q => (decimal?)q.TotalValue), cancellationToken, "Error summing transaction summary by operator values");
        if (totalValueResult.IsFailed)
            return ResultHelpers.CreateFailure(totalValueResult);

        Result<List<OperatorSummaryData>> operatorResult = await ReportingQueryExecutor.ToListAsync(
            query.OrderBy(q => string.IsNullOrWhiteSpace(q.OperatorName) ? "Unknown Operator" : q.OperatorName)
                .ThenBy(q => q.OperatorReportingId)
                .ThenBy(q => q.OperatorId)
                .Skip((int)((long)(request.PageNumber - 1) * request.PageSize))
                .Take(request.PageSize),
            cancellationToken,
            "Error retrieving transaction summary by operator report");
        if (operatorResult.IsFailed)
            return ResultHelpers.CreateFailure(operatorResult);

        return Result.Success(new OperatorSummaryReportData(
            operatorResult.Data,
            operatorCountResult.Data,
            totalCountResult.Data ?? 0,
            totalValueResult.Data ?? 0m));
    }

    private static IQueryable<OperatorTransactionData> BuildOperatorTransactionBaseQuery(EstateManagementContext context,
                                                                                         TransactionQueries.TransactionSummaryByOperatorQuery request) {
        IQueryable<OperatorTransactionData> query = from t in context.Transactions
                                                    join m in context.Merchants on t.MerchantId equals m.MerchantId
                                                    join o in context.Operators on t.OperatorId equals o.OperatorId
                                                    where t.TransactionType == "Sale" && t.TransactionDate >= request.Request.StartDate.Date && t.TransactionDate < request.Request.EndDate.Date.AddDays(1)
                                                    group t by new {
                                                                       t.MerchantId,
                                                                       m.MerchantReportingId,
                                                                       MerchantName = m.Name,
                                                                       t.OperatorId,
                                                                       o.OperatorReportingId,
                                                                       OperatorName = o.Name
                                                                   }
                                                    into g
                                                    select new OperatorTransactionData {
                                                                                           MerchantId = g.Key.MerchantId,
                                                                                           MerchantReportingId = g.Key.MerchantReportingId,
                                                                                           MerchantName = g.Key.MerchantName,
                                                                                           OperatorId = g.Key.OperatorId,
                                                                                           OperatorReportingId = g.Key.OperatorReportingId,
                                                                                           OperatorName = g.Key.OperatorName,
                                                                                           TotalCount = g.Count(),
                                                                                           TotalValue = g.Sum(x => x.TransactionAmount),
                                                                                           AuthorisedCount = g.Sum(x => x.IsAuthorised ? 1 : 0),
                                                                                           DeclinedCount = g.Sum(x => x.IsAuthorised ? 0 : 1)
                                                                                       };

        return ApplyOperatorTransactionFilters(query, request.Request);
    }

    private static IQueryable<OperatorTransactionData> ApplyOperatorTransactionFilters(IQueryable<OperatorTransactionData> query,
                                                                                        Models.TransactionSummaryByOperatorRequest request) {
        if (request.Merchants != null && request.Merchants.Any())
            query = query.Where(q => request.Merchants.Contains(q.MerchantReportingId));

        if (request.Operators != null && request.Operators.Any())
            query = query.Where(q => request.Operators.Contains(q.OperatorReportingId));

        return query;
    }

    private static IQueryable<OperatorSummaryData> BuildOperatorFinalSummaryQuery(IQueryable<OperatorTransactionData> query) {
        return from x in query
            group x by new { x.OperatorId, x.OperatorReportingId, x.OperatorName }
            into g
            select new OperatorSummaryData {
                OperatorId = g.Key.OperatorId,
                OperatorReportingId = g.Key.OperatorReportingId,
                OperatorName = g.Key.OperatorName,
                TotalCount = g.Sum(x => x.TotalCount),
                TotalValue = g.Sum(x => x.TotalValue),
                AverageValue = g.Count() > 0 ? g.Sum(x => x.TotalValue) / g.Count() : 0m,
                AuthorisedCount = g.Sum(x => x.AuthorisedCount),
                DeclinedCount = g.Sum(x => x.DeclinedCount),
                AuthorisedPercentage = g.Sum(x => x.TotalCount) > 0 ? (decimal)g.Sum(x => x.AuthorisedCount) / (decimal)g.Sum(x => x.TotalCount) : 0m
            };
    }

    private TransactionSummaryByOperatorResponse BuildOperatorSummaryResponse(List<OperatorSummaryData> queryResults,
                                                                               int totalCount,
                                                                               decimal totalValue,
                                                                               int totalOperators,
                                                                               PaginationMetadata pagination) {
        return new TransactionSummaryByOperatorResponse {
            Operators = queryResults.Select(q => new OperatorDetail {
                OperatorId = q.OperatorId,
                OperatorReportingId = q.OperatorReportingId,
                OperatorName = string.IsNullOrWhiteSpace(q.OperatorName) ? $"Unknown Operator ({q.OperatorReportingId})" : q.OperatorName,
                AuthorisedCount = q.AuthorisedCount,
                AuthorisedPercentage = q.AuthorisedPercentage,
                AverageValue = q.AverageValue,
                DeclinedCount = q.DeclinedCount,
                TotalCount = q.TotalCount,
                TotalValue = q.TotalValue
            }).ToList(),
            Summary = new OperatorDetailSummary {
                TotalCount = totalCount,
                TotalValue = totalValue,
                AverageValue = SafeDivide(totalValue, totalCount),
                TotalOperators = totalOperators
            },
            Pagination = pagination
        };
    }
    public async Task<Result<ProductPerformanceResponse>> GetProductPerformanceReport(TransactionQueries.ProductPerformanceQuery request,
                                                                                      CancellationToken cancellationToken) {
        string? validationError = ReportQueryValidation.ValidateDateRange(request.StartDate, request.EndDate)
                                  ?? ReportQueryValidation.ValidatePaging(request.PageNumber, request.PageSize);
        if (validationError != null)
            return Result.Failure(validationError);

        using ResolvedDbContext<EstateManagementContext>? resolvedContext = Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        Result<ProductPerformanceReportData> reportDataResult = await LoadProductPerformanceReportDataAsync(context, request, cancellationToken);
        if (reportDataResult.IsFailed)
            return ResultHelpers.CreateFailure(reportDataResult);
        ProductPerformanceReportData reportData = reportDataResult.Data;
        int totalProducts = reportData.TotalProducts;
        int totalCount = reportData.TotalCount;
        int totalPages = totalProducts == 0 ? 0 : (int)Math.Ceiling((double)totalProducts / request.PageSize);
        return Result.Success(BuildProductPerformanceResponse(
            reportData.Products,
            totalProducts,
            totalCount,
            reportData.TotalValue,
            new PaginationMetadata {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalItems = totalProducts,
                TotalPages = totalPages
            }));
    }

    private static async Task<Result<ProductPerformanceReportData>> LoadProductPerformanceReportDataAsync(
        EstateManagementContext context,
        TransactionQueries.ProductPerformanceQuery request,
        CancellationToken cancellationToken) {
        IQueryable<Decimal> grandTotalAmountQuery = from t in context.Transactions
                                                    where t.TransactionType == "Sale" && t.TransactionDate >= request.StartDate.Date && t.TransactionDate < request.EndDate.Date.AddDays(1)
                                                    select t.TransactionAmount;
        Result<Decimal> grandTotalAmountResult = await ReportingQueryExecutor.SumAsync<decimal>(grandTotalAmountQuery, cancellationToken);
        if (grandTotalAmountResult.IsFailed)
            return ResultHelpers.CreateFailure(grandTotalAmountResult);

        IQueryable<ProductPerformanceItemData> query = BuildProductPerformanceQuery(context, request.StartDate, request.EndDate, grandTotalAmountResult.Data);
        Result<ProductPerformanceTotals> totalsResult = await LoadProductPerformanceTotalsAsync(query, cancellationToken);
        if (totalsResult.IsFailed)
            return ResultHelpers.CreateFailure(totalsResult);

        Result<List<ProductPerformanceItemData>> productsResult = await ReportingQueryExecutor.ToListAsync(
            query.OrderBy(q => string.IsNullOrWhiteSpace(q.ProductName) ? "Unknown Product" : q.ProductName)
                .ThenBy(q => q.ContractReportingId)
                .ThenBy(q => q.ContractProductReportingId)
                .ThenBy(q => q.ContractProductId)
                .Skip((int)((long)(request.PageNumber - 1) * request.PageSize))
                .Take(request.PageSize),
            cancellationToken,
            "Error retrieving product performance report");
        if (productsResult.IsFailed)
            return ResultHelpers.CreateFailure(productsResult);

        return Result.Success(new ProductPerformanceReportData(
            productsResult.Data,
            totalsResult.Data.TotalProducts,
            totalsResult.Data.TotalCount,
            totalsResult.Data.TotalValue));
    }

    private static async Task<Result<ProductPerformanceTotals>> LoadProductPerformanceTotalsAsync(
        IQueryable<ProductPerformanceItemData> query,
        CancellationToken cancellationToken) {
        Result<Int32> productCountResult = await ReportingQueryExecutor.CountAsync(query, cancellationToken, "Error counting product performance rows");
        if (productCountResult.IsFailed)
            return ResultHelpers.CreateFailure(productCountResult);

        Result<Int32?> totalCountResult = await ReportingQueryExecutor.SumAsync(query.Select(q => (int?)q.TransactionCount), cancellationToken, "Error summing product performance counts");
        if (totalCountResult.IsFailed)
            return ResultHelpers.CreateFailure(totalCountResult);

        Result<Decimal?> totalValueResult = await ReportingQueryExecutor.SumAsync(query.Select(q => (decimal?)q.TotalAmount), cancellationToken, "Error summing product performance values");
        if (totalValueResult.IsFailed)
            return ResultHelpers.CreateFailure(totalValueResult);

        return Result.Success(new ProductPerformanceTotals(
            productCountResult.Data,
            totalCountResult.Data ?? 0,
            totalValueResult.Data ?? 0m));
    }

    public async Task<Result<TransactionMixSummaryResponse>> GetTransactionMixSummary(TransactionQueries.TransactionMixSummaryQuery request,
                                                                                      CancellationToken cancellationToken)
    {
        string? validationError = ValidateTransactionMixRequest(request.Request);
        if (validationError != null)
            return Result.Failure(validationError);

        using ResolvedDbContext<EstateManagementContext>? resolvedContext = Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        TransactionDetailReportRequest detailRequest = new TransactionDetailReportRequest
                                                       {
                                                           Merchants = request.Request.MerchantReportingId.HasValue ? new List<int> { request.Request.MerchantReportingId.Value } : [],
                                                           Operators = [],
                                                           Products = [],
                                                           StartDate = request.Request.StartDate,
                                                           EndDate = request.Request.EndDate
                                                       };

        IQueryable<TransactionDetailQueryResult> query = ApplyTransactionDetailFilters(BuildTransactionDetailBaseQuery(context, detailRequest), detailRequest);
        Result<TransactionMixReportData> reportDataResult = await LoadTransactionMixReportDataAsync(query, request.Request, cancellationToken);
        if (reportDataResult.IsFailed)
            return ResultHelpers.CreateFailure(reportDataResult);
        TransactionMixReportData reportData = reportDataResult.Data;
        int totalCount = reportData.TotalCount;
        int totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling((double)totalCount / request.Request.PageSize);
        return Result.Success(BuildTransactionMixSummaryResponse(
            reportData.Transactions,
            reportData.Groups,
            request.Request,
            totalCount,
            reportData.TotalValue,
            new PaginationMetadata {
                PageNumber = request.Request.PageNumber,
                PageSize = request.Request.PageSize,
                TotalItems = totalCount,
                TotalPages = totalPages
            }));
    }

    private static string? ValidateTransactionMixRequest(TransactionMixSummaryRequest request) {
        return ReportQueryValidation.ValidateDateRange(request.StartDate, request.EndDate)
               ?? ReportQueryValidation.ValidatePaging(request.PageNumber, request.PageSize)
               ?? ReportQueryValidation.ValidateTopN(request.TopN)
               ?? (Enum.IsDefined(typeof(TransactionMixBreakdown), request.Breakdown) ? null : "Unsupported transaction mix breakdown.")
               ?? (Enum.IsDefined(typeof(TransactionMixMeasure), request.Measure) ? null : "Unsupported transaction mix measure.");
    }

    private static async Task<Result<TransactionMixReportData>> LoadTransactionMixReportDataAsync(
        IQueryable<TransactionDetailQueryResult> query,
        TransactionMixSummaryRequest request,
        CancellationToken cancellationToken) {
        Result<Int32> totalCountResult = await ReportingQueryExecutor.CountAsync(query, cancellationToken, "Error counting transaction mix summary rows");
        if (totalCountResult.IsFailed)
            return ResultHelpers.CreateFailure(totalCountResult);

        Result<Decimal?> totalValueResult = await ReportingQueryExecutor.SumAsync(query.Select(q => (decimal?)q.Value), cancellationToken, "Error summing transaction mix summary values");
        if (totalValueResult.IsFailed)
            return ResultHelpers.CreateFailure(totalValueResult);

        IQueryable<TransactionMixGroupQueryResult> groupQuery = BuildTransactionMixGroupQuery(query, request.Breakdown);
        Result<List<TransactionMixGroupQueryResult>> groupResult = await ReportingQueryExecutor.ToListAsync(BuildOrderedTransactionMixGroupQuery(groupQuery, request).Take(request.TopN), cancellationToken,
            "Error retrieving transaction mix groups");
        if (groupResult.IsFailed)
            return ResultHelpers.CreateFailure(groupResult);

        Result<List<TransactionDetailQueryResult>> pageResult = await ReportingQueryExecutor.ToListAsync(BuildTransactionMixPageQuery(query, request), cancellationToken,
            "Error retrieving transaction mix summary transactions");
        if (pageResult.IsFailed)
            return ResultHelpers.CreateFailure(pageResult);

        return Result.Success(new TransactionMixReportData(
            pageResult.Data,
            groupResult.Data,
            totalCountResult.Data,
            totalValueResult.Data ?? 0m));
    }

    private static IOrderedQueryable<TransactionMixGroupQueryResult> BuildOrderedTransactionMixGroupQuery(
        IQueryable<TransactionMixGroupQueryResult> groupQuery,
        TransactionMixSummaryRequest request) {
        return request.Breakdown switch {
            TransactionMixBreakdown.Product or TransactionMixBreakdown.Operator => request.Measure == TransactionMixMeasure.Count
                ? groupQuery.OrderByDescending(g => g.TransactionCount).ThenBy(g => g.GroupName).ThenBy(g => g.NumericKey)
                : groupQuery.OrderByDescending(g => g.TransactionValue).ThenBy(g => g.GroupName).ThenBy(g => g.NumericKey),
            TransactionMixBreakdown.TransactionType or TransactionMixBreakdown.Status => request.Measure == TransactionMixMeasure.Count
                ? groupQuery.OrderByDescending(g => g.TransactionCount).ThenBy(g => g.GroupName).ThenBy(g => g.TextKey)
                : groupQuery.OrderByDescending(g => g.TransactionValue).ThenBy(g => g.GroupName).ThenBy(g => g.TextKey),
            _ => throw new ArgumentOutOfRangeException(nameof(request.Breakdown), request.Breakdown, null)
        };
    }

    private static IQueryable<TransactionDetailQueryResult> BuildTransactionMixPageQuery(
        IQueryable<TransactionDetailQueryResult> query,
        TransactionMixSummaryRequest request) {
        return query
            .OrderByDescending(q => q.TransactionDateTime)
            .ThenByDescending(q => q.TransactionId)
            .Skip((int)((long)(request.PageNumber - 1) * request.PageSize))
            .Take(request.PageSize);
    }

    public async Task<Result<GetRecentActivityReceiptReportResponse>> GetRecentActivityReceiptReport(TransactionQueries.GetRecentActivityReceiptReportQuery request,
                                                                                                     CancellationToken cancellationToken) {
        string? validationError = ReportQueryValidation.ValidatePaging(request.Request.PageNumber, request.Request.PageSize);
        if (validationError != null)
            return Result.Failure(validationError);

        using ResolvedDbContext<EstateManagementContext>? resolvedContext = Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        RecentActivityReceiptRequestParameters parameters = BuildRecentActivityReceiptRequestParameters(request.Request);
        IQueryable<RecentActivityReceiptQueryResult> query = BuildRecentActivityReceiptQuery(context, parameters);

        Result<Int32> totalCountResult = await ReportingQueryExecutor.CountAsync(query, cancellationToken, "Error counting recent activity receipt report rows");
        if (totalCountResult.IsFailed)
            return ResultHelpers.CreateFailure(totalCountResult);

        int totalCount = totalCountResult.Data;
        if (totalCount == 0)
            return Result.Success(BuildEmptyRecentActivityReceiptReportResponse(parameters));

        IQueryable<RecentActivityReceiptQueryResult> pageQuery = query.Skip((parameters.PageNumber - 1) * parameters.PageSize).Take(parameters.PageSize);
        Result<List<RecentActivityReceiptQueryResult>> pageResult = await ReportingQueryExecutor.ToListAsync(pageQuery, cancellationToken, "Error retrieving recent activity receipt report");
        if (pageResult.IsFailed)
            return ResultHelpers.CreateFailure(pageResult);

        return Result.Success(BuildRecentActivityReceiptReportResponse(parameters, totalCount, pageResult.Data));
    }

    private static RecentActivityReceiptRequestParameters BuildRecentActivityReceiptRequestParameters(GetRecentActivityReceiptReportRequest request) {
        return new RecentActivityReceiptRequestParameters {
            ReportDate = DateOnly.FromDateTime(request.ReportDate.Date),
            MerchantReportingId = request.MerchantReportingId,
            SearchText = request.SearchText?.Trim(),
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    private static IQueryable<RecentActivityReceiptQueryResult> BuildRecentActivityReceiptQuery(EstateManagementContext context,
                                                                                                RecentActivityReceiptRequestParameters parameters) {
        IQueryable<RecentActivityReceiptQueryResult> query = from t in context.Transactions
            join m in context.Merchants on t.MerchantId equals m.MerchantId
            join o in context.Operators on t.OperatorId equals o.OperatorId
            join cp in context.ContractProducts on new { t.ContractProductId, t.ContractId } equals new { cp.ContractProductId, cp.ContractId }
            where t.TransactionDate == parameters.ReportDate.ToDateTime(TimeOnly.MinValue)
            select new RecentActivityReceiptQueryResult {
                TransactionId = t.TransactionId,
                TransactionDateTime = t.TransactionDateTime,
                MerchantReportingId = m.MerchantReportingId,
                Reference = t.TransactionNumber,
                TransactionType = t.TransactionType,
                Product = cp.ProductName,
                Operator = o.Name,
                Status = t.IsAuthorised ? "Successful" : "Failed",
                Amount = t.TransactionAmount,
                ReceiptReference = t.TransactionReference
            };

        return ApplyRecentActivityReceiptFilters(query, parameters)
            .OrderByDescending(q => q.TransactionDateTime)
            .ThenByDescending(q => q.TransactionId);
    }

    private static IQueryable<RecentActivityReceiptQueryResult> ApplyRecentActivityReceiptFilters(IQueryable<RecentActivityReceiptQueryResult> query,
                                                                                                  RecentActivityReceiptRequestParameters parameters) {
        if (parameters.MerchantReportingId.HasValue)
            query = query.Where(q => q.MerchantReportingId == parameters.MerchantReportingId.Value);

        if (string.IsNullOrWhiteSpace(parameters.SearchText) == false)
            query = query.Where(q =>
                q.Reference.Contains(parameters.SearchText) ||
                q.TransactionType.Contains(parameters.SearchText) ||
                q.Product.Contains(parameters.SearchText) ||
                q.Operator.Contains(parameters.SearchText) ||
                q.Status.Contains(parameters.SearchText) ||
                q.ReceiptReference.Contains(parameters.SearchText));

        return query;
    }

    private static GetRecentActivityReceiptReportResponse BuildEmptyRecentActivityReceiptReportResponse(RecentActivityReceiptRequestParameters parameters) {
        return new GetRecentActivityReceiptReportResponse {
            ReportDate = parameters.ReportDate,
            Items = [],
            Pagination = new PaginationMetadata {
                PageNumber = parameters.PageNumber,
                PageSize = parameters.PageSize,
                TotalItems = 0,
                TotalPages = 0
            }
        };
    }

    private static GetRecentActivityReceiptReportResponse BuildRecentActivityReceiptReportResponse(RecentActivityReceiptRequestParameters parameters,
                                                                                                    int totalCount,
                                                                                                    List<RecentActivityReceiptQueryResult> queryResults) {
        return new GetRecentActivityReceiptReportResponse {
            ReportDate = parameters.ReportDate,
            Items = queryResults.Select(MapRecentActivityReceiptItem).ToList(),
            Pagination = new PaginationMetadata {
                PageNumber = parameters.PageNumber,
                PageSize = parameters.PageSize,
                TotalItems = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / parameters.PageSize)
            }
        };
    }

    private static RecentActivityReceiptItemDto MapRecentActivityReceiptItem(RecentActivityReceiptQueryResult item) {
        return new RecentActivityReceiptItemDto {
            Reference = item.Reference,
            TransactionType = item.TransactionType,
            Product = item.Product,
            Operator = item.Operator,
            Status = item.Status,
            Amount = item.Amount,
            TransactionDateTime = item.TransactionDateTime,
            ReceiptReference = item.ReceiptReference
        };
    }

    private static IQueryable<ProductPerformanceItemData> BuildProductPerformanceQuery(EstateManagementContext context,
                                                                                        DateTime startDate,
                                                                                        DateTime endDate,
                                                                                       decimal grandTotalAmount) {
        return from t in context.Transactions
            join cp in context.ContractProducts on new { t.ContractProductId, t.ContractId } equals new { cp.ContractProductId, cp.ContractId }
            join c in context.Contracts on t.ContractId equals c.ContractId
            where t.TransactionType == "Sale" && t.TransactionDate >= startDate.Date && t.TransactionDate < endDate.Date.AddDays(1)
            group t by new { cp.ProductName, cp.ContractProductId, cp.ContractProductReportingId, c.ContractId, c.ContractReportingId }
            into g
            select new ProductPerformanceItemData {
                ProductName = g.Key.ProductName,
                ContractProductId = g.Key.ContractProductId,
                ContractProductReportingId = g.Key.ContractProductReportingId,
                ContractId = g.Key.ContractId,
                ContractReportingId = g.Key.ContractReportingId,
                TransactionCount = g.Count(),
                TotalAmount = g.Sum(x => x.TransactionAmount),
                PercentOfTotalAmount = grandTotalAmount == 0 ? 0 : 100.0m * g.Sum(x => x.TransactionAmount) / grandTotalAmount
            };
    }

    private ProductPerformanceResponse BuildProductPerformanceResponse(List<ProductPerformanceItemData> queryResults,
                                                                        int totalProducts,
                                                                        int totalCount,
                                                                        decimal totalValue,
                                                                        PaginationMetadata pagination) {
        return new ProductPerformanceResponse {
            ProductDetails = queryResults.Select(q => new ProductPerformanceDetail {
                ProductName = string.IsNullOrWhiteSpace(q.ProductName) ? $"Unknown Product ({q.ContractProductReportingId})" : q.ProductName,
                ProductId = q.ContractProductId,
                ProductReportingId = q.ContractProductReportingId,
                ContractId = q.ContractId,
                ContractReportingId = q.ContractReportingId,
                TransactionCount = q.TransactionCount,
                TransactionValue = q.TotalAmount,
                PercentageOfTotal = q.PercentOfTotalAmount
            }).ToList(),
            Summary = new ProductPerformanceSummary { TotalCount = totalCount, TotalValue = totalValue, AveragePerProduct = SafeDivide(totalValue, totalProducts), TotalProducts = totalProducts },
            Pagination = pagination
        };
    }

    private static IQueryable<TransactionMixGroupQueryResult> BuildTransactionMixGroupQuery(IQueryable<TransactionDetailQueryResult> query,
                                                                                              TransactionMixBreakdown breakdown)
    {
        return breakdown switch
        {
            TransactionMixBreakdown.Product => from q in query
                                                group q by new { q.ContractProductReportingId, q.ProductName }
                                                into g
                                                select new TransactionMixGroupQueryResult {
                                                    NumericKey = g.Key.ContractProductReportingId,
                                                    GroupName = g.Key.ProductName,
                                                    TransactionCount = g.Count(),
                                                    TransactionValue = g.Sum(x => x.Value)
                                                },
            TransactionMixBreakdown.TransactionType => from q in query
                                                        group q by q.TransactionType
                                                        into g
                                                        select new TransactionMixGroupQueryResult {
                                                            TextKey = g.Key,
                                                            GroupName = g.Key,
                                                            TransactionCount = g.Count(),
                                                            TransactionValue = g.Sum(x => x.Value)
                                                        },
            TransactionMixBreakdown.Operator => from q in query
                                                group q by new { q.OperatorReportingId, q.OperatorName }
                                                into g
                                                select new TransactionMixGroupQueryResult {
                                                    NumericKey = g.Key.OperatorReportingId,
                                                    GroupName = g.Key.OperatorName,
                                                    TransactionCount = g.Count(),
                                                    TransactionValue = g.Sum(x => x.Value)
                                                },
            TransactionMixBreakdown.Status => from q in query
                                              group q by q.Status
                                              into g
                                              select new TransactionMixGroupQueryResult {
                                                  TextKey = g.Key,
                                                  GroupName = g.Key,
                                                  TransactionCount = g.Count(),
                                                  TransactionValue = g.Sum(x => x.Value)
                                              },
            _ => throw new ArgumentOutOfRangeException(nameof(breakdown), breakdown, null)
        };
    }

    private static TransactionMixSummaryResponse BuildTransactionMixSummaryResponse(List<TransactionDetailQueryResult> queryResults,
                                                                                     List<TransactionMixGroupQueryResult> groupResults,
                                                                                     TransactionMixSummaryRequest request,
                                                                                     int totalCount,
                                                                                     decimal totalValue,
                                                                                     PaginationMetadata pagination)
    {
        return new TransactionMixSummaryResponse
        {
            FromDate = DateOnly.FromDateTime(request.StartDate),
            ToDate = DateOnly.FromDateTime(request.EndDate),
            Breakdown = request.Breakdown,
            Measure = request.Measure,
            TotalCount = totalCount,
            TotalValue = totalValue,
            Groups = groupResults.Select(g => new TransactionMixSummaryGroup {
                GroupKey = g.NumericKey?.ToString() ?? g.TextKey ?? string.Empty,
                GroupName = g.GroupName ?? string.Empty,
                TransactionCount = g.TransactionCount,
                TransactionValue = g.TransactionValue
            }).ToList(),
            Transactions = queryResults
                .OrderByDescending(q => q.TransactionDateTime)
                .Select(q => new TransactionMixSummaryTransaction
                {
                    Id = q.TransactionId,
                    DateTime = q.TransactionDateTime,
                    Merchant = q.MerchantName,
                    MerchantId = q.MerchantId,
                    MerchantReportingId = q.MerchantReportingId,
                    Operator = q.OperatorName,
                    OperatorId = q.OperatorId,
                    OperatorReportingId = q.OperatorReportingId,
                    Product = q.ProductName,
                    ProductId = q.ContractProductId,
                    ProductReportingId = q.ContractProductReportingId,
                    Type = q.TransactionType,
                    Status = q.Status,
                    Value = q.Value,
                    TotalFees = q.FeeValue,
                    SettlementReference = q.SettlementId == Guid.Empty ? null : q.SettlementId.ToString(),
                    TransactionNumber = q.TransactionNumber
                })
                .ToList(),
            Pagination = pagination
        };
    }

    public async Task<Result<List<TodaysSalesByHour>>> GetTodaysSalesByHour(TransactionQueries.TodaysSalesByHour request,
                                                                            CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = Resolver.Resolve(EstateManagementDatabaseName, request.estateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        DateTime now = DatePolicy.LocalNow;
        IQueryable<TodayTransaction> todaysSales = this.BuildTodaySalesQuery(context, now);
        IQueryable<TransactionHistory> comparisonSales = this.BuildComparisonSalesQuery(context, request.comparisonDate, now);

        // First we need to get a value of todays sales
        var todaysSalesByHourQuery = (from t in todaysSales group t.TransactionAmount by t.Hour into g select new { Hour = g.Key, TotalSalesCount = g.Count(), TotalSalesValue = g.Sum() });
        var todaysSalesByHourQueryResult = await ReportingQueryExecutor.ToListAsync(todaysSalesByHourQuery, cancellationToken);
        if (todaysSalesByHourQueryResult.IsFailed)
            return ResultHelpers.CreateFailure(todaysSalesByHourQueryResult);
        var todaysSalesByHour = todaysSalesByHourQueryResult.Data;

        var comparisonSalesByHourQuery = (from t in comparisonSales group t.TransactionAmount by t.Hour into g select new { Hour = g.Key, TotalSalesCount = g.Count(), TotalSalesValue = g.Sum() });
        var comparisonSalesByHourQueryResult = await ReportingQueryExecutor.ToListAsync(comparisonSalesByHourQuery, cancellationToken);
        if (comparisonSalesByHourQueryResult.IsFailed)
            return ResultHelpers.CreateFailure(comparisonSalesByHourQueryResult);
        var comparisonSalesByHour = comparisonSalesByHourQueryResult.Data;

        List<TodaysSalesByHour> response = (from today in todaysSalesByHour
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

    public async Task<Result<MerchantDailyPerformanceSummaryResponse>> GetMerchantDailyPerformanceSummary(TransactionQueries.MerchantDailyPerformanceSummaryQuery request,
                                                                                                          CancellationToken cancellationToken) {
        string? validationError = ReportQueryValidation.ValidateDateRange(request.Request.StartDate, request.Request.EndDate);
        if (validationError != null)
            return Result.Failure(validationError);

        using ResolvedDbContext<EstateManagementContext>? resolvedContext = Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        DateTime startDate = request.Request.StartDate.Date;
        DateTime endDate = request.Request.EndDate.Date;
        int dayCount = Math.Max((endDate - startDate).Days + 1, 1);

        Result<List<MerchantDailyPerformanceGroupProjection>> groupedTransactionsResult = await LoadMerchantDailyPerformanceGroups(context, request, startDate, endDate, cancellationToken);
        if (groupedTransactionsResult.IsFailed)
            return ResultHelpers.CreateFailure(groupedTransactionsResult);

        List<MerchantDailyPerformanceGroupProjection> groupedTransactions = groupedTransactionsResult.Data;
        if (groupedTransactions.Any() == false) {
            return Result.Success(BuildEmptyMerchantDailyPerformanceSummaryResponse());
        }

        List<MetricItem> metrics = BuildMerchantDailyPerformanceMetrics(groupedTransactions, dayCount);

        Result<List<MerchantDailyPerformanceRecentSaleProjection>> recentSalesResult = await LoadMerchantDailyPerformanceRecentSales(context, request, startDate, endDate, cancellationToken);
        if (recentSalesResult.IsFailed)
            return ResultHelpers.CreateFailure(recentSalesResult);

        return Result.Success(new MerchantDailyPerformanceSummaryResponse {
            Metrics = metrics,
            DrillDownTransactions = MapMerchantDailyPerformanceRecentSales(recentSalesResult.Data)
        });
    }

        private IQueryable<TodayTransaction> BuildTodaySalesQuery(EstateManagementContext context, DateTime now) {
             return from t in context.TodayTransactions where t.IsAuthorised && t.TransactionType == "Sale" && t.TransactionDate == now.Date && t.TransactionTime <= now.TimeOfDay select t;
        }

        private IQueryable<Decimal> BuildTodaysFailedSalesQuery(EstateManagementContext context,
                                                                String responseCode,
                                                                DateTime now) {
            return from t in context.TodayTransactions
                    where t.IsAuthorised == false && t.TransactionType == "Sale" && t.TransactionDate == now.Date && t.TransactionTime <= now.TimeOfDay && t.ResponseCode == responseCode
                   select t.TransactionAmount;
        }

        private IQueryable<TransactionHistory> BuildComparisonSalesQuery(EstateManagementContext context,
                                                                          DateTime comparisonDate,
                                                                          DateTime now) {
             return from t in context.TransactionHistory where t.IsAuthorised && t.TransactionType == "Sale" && t.TransactionDate == comparisonDate.Date && t.TransactionTime <= now.TimeOfDay select t;
        }

        private IQueryable<Decimal> BuildComparisonFailedSalesQuery(EstateManagementContext context,
                                                                    DateTime comparisonDate,
                                                                    String responseCode,
                                                                    DateTime now) {
            return from t in context.TransactionHistory
                   where t.IsAuthorised == false && t.TransactionType == "Sale" && t.TransactionDate == comparisonDate.Date && t.TransactionTime <= now.TimeOfDay && t.ResponseCode == responseCode
                   select t.TransactionAmount;
        }


        private sealed class MerchantTransactionGroupProjection {
            public Guid MerchantId { get; init; }
            public int MerchantReportingId { get; init; }
            public string MerchantName { get; init; }
            public Guid OperatorId { get; init; }
            public int OperatorReportingId { get; init; }
            public int TotalCount { get; init; }
            public decimal TotalValue { get; init; }
            public int AuthorisedCount { get; init; }
            public int DeclinedCount { get; init; }
        }

        private sealed class MerchantTransactionFinalProjection {
            public Guid MerchantId { get; init; }
            public int MerchantReportingId { get; init; }
            public string MerchantName { get; init; }
            public int TotalCount { get; init; }
            public decimal TotalValue { get; init; }
            public decimal AverageValue { get; init; }
            public int AuthorisedCount { get; init; }
            public int DeclinedCount { get; init; }
            public decimal AuthorisedPercentage { get; init; }
        }

        private sealed class MerchantDailyPerformanceGroupProjection {
            public int ContractProductReportingId { get; init; }
            public string? ProductName { get; init; }
            public string OperatorName { get; init; }
            public bool IsAuthorised { get; init; }
            public int SalesCount { get; init; }
            public decimal SalesValue { get; init; }
        }

        private sealed class MerchantDailyPerformanceRecentSaleProjection {
            public Guid TransactionId { get; init; }
            public string Reference { get; init; }
            public string? Product { get; init; }
            public String Operator { get; init; }
            public string Status { get; init; }
            public decimal Amount { get; init; }
            public DateTime TransactionDateTime { get; init; }
        }

        private static MerchantDailyPerformanceSummaryResponse BuildEmptyMerchantDailyPerformanceSummaryResponse() {
            return new MerchantDailyPerformanceSummaryResponse {
                Metrics = BuildMerchantDailyPerformanceBaseMetrics()
            };
        }

        private static List<MetricItem> BuildMerchantDailyPerformanceBaseMetrics() {
            return new List<MetricItem> {
                new() { Title = "Total Sales Count", Value = 0, Description = "All sales transactions in the range", Category = 1, Type = 0 },
                new() { Title = "Total Sales Value", Value = 0, Description = "All sales value in the range", Category = 1, Type = 1 },
                new() { Title = "Successful Sales Count", Value = 0, Description = "Authorised sales count in the range", Category = 2, Type = 2 },
                new() { Title = "Successful Sales Value", Value = 0, Description = "Authorised sales value in the range", Category = 2, Type = 3 },
                new() { Title = "Failed Sales Count", Value = 0, Description = "Declined sales count in the range", Category = 3, Type = 4 },
                new() { Title = "Failed Sales Value", Value = 0, Description = "Declined sales value in the range", Category = 3, Type = 5 },
                new() { Title = "Average Sales Count", Value = 0, Description = "Average sales count per day in the range", Category = 4, Type = 6 },
                new() { Title = "Average Sales Value", Value = 0, Description = "Average value per sale in the range", Category = 4, Type = 7 }
            };
        }

        private static List<MetricItem> BuildMerchantDailyPerformanceMetrics(List<MerchantDailyPerformanceGroupProjection> groupedTransactions,
                                                                             int dayCount) {
            int totalSalesCount = groupedTransactions.Sum(x => x.SalesCount);
            decimal totalSalesValue = groupedTransactions.Sum(x => x.SalesValue);
            int successfulSalesCount = groupedTransactions.Where(x => x.IsAuthorised).Sum(x => x.SalesCount);
            decimal successfulSalesValue = groupedTransactions.Where(x => x.IsAuthorised).Sum(x => x.SalesValue);
            int failedSalesCount = groupedTransactions.Where(x => !x.IsAuthorised).Sum(x => x.SalesCount);
            decimal failedSalesValue = groupedTransactions.Where(x => !x.IsAuthorised).Sum(x => x.SalesValue);
            decimal averageSalesCount = (decimal)totalSalesCount / dayCount;
            decimal averageSalesValue = totalSalesCount == 0 ? 0m : totalSalesValue / totalSalesCount;

            List<MetricItem> metrics = new() {
                new() { Title = "Total Sales Count", Value = totalSalesCount, Description = "All sales transactions in the range", Category = 1, Type = 0},
                new() { Title = "Total Sales Value", Value = totalSalesValue, Description = "All sales value in the range", Category = 1, Type = 1 },
                new() { Title = "Successful Sales Count", Value = successfulSalesCount, Description = "Authorised sales count in the range", Category = 2, Type = 2 },
                new() { Title = "Successful Sales Value", Value = successfulSalesValue, Description = "Authorised sales value in the range", Category = 2, Type = 3 },
                new() { Title = "Failed Sales Count", Value = failedSalesCount, Description = "Declined sales count in the range", Category = 3, Type = 4 },
                new() { Title = "Failed Sales Value", Value = failedSalesValue, Description = "Declined sales value in the range", Category = 3, Type = 5 },
                new() { Title = "Average Sales Count", Value = averageSalesCount, Description = "Average sales count per day in the range", Category = 4, Type = 6 },
                new() { Title = "Average Sales Value", Value = averageSalesValue, Description = "Average value per sale in the range", Category = 4, Type = 7 }
            };

            MetricItem? topProductMetric = BuildTopProductMetric(groupedTransactions);
            if (topProductMetric != null)
                metrics.Add(topProductMetric);

            return metrics;
        }

        private static MetricItem? BuildTopProductMetric(List<MerchantDailyPerformanceGroupProjection> groupedTransactions) {
            var topProduct = groupedTransactions
                .GroupBy(x => new { x.ContractProductReportingId, x.OperatorName, x.ProductName })
                .Select(g => new {
                    g.Key.ContractProductReportingId,
                    g.Key.ProductName,  
                    g.Key.OperatorName,
                    SalesCount = g.Sum(x => x.SalesCount),
                    SalesValue = g.Sum(x => x.SalesValue)
                })
                .OrderByDescending(x => x.SalesCount)
                .ThenBy(x => x.ProductName)
                .FirstOrDefault();
        
            return new MetricItem {
                Title = "Top Product Sales Count",
                Value = topProduct.SalesCount,
                Description = string.IsNullOrWhiteSpace(topProduct.OperatorName) || string.IsNullOrWhiteSpace(topProduct.ProductName)
                    ? "Unknown product"
                    : $"{topProduct.OperatorName} {topProduct.ProductName}",
                Category = 5,
                Type = 8
            };
        }

        private async Task<Result<List<MerchantDailyPerformanceGroupProjection>>> LoadMerchantDailyPerformanceGroups(EstateManagementContext context,
                                                                                                                   TransactionQueries.MerchantDailyPerformanceSummaryQuery request,
                                                                                                                   DateTime startDate,
                                                                                                                   DateTime endDate,
                                                                                                                   CancellationToken cancellationToken) {
            IQueryable<MerchantDailyPerformanceGroupProjection> query =
                from t in context.Transactions
                join m in context.Merchants on t.MerchantId equals m.MerchantId
                join cp in context.ContractProducts on new { t.ContractProductId, t.ContractId } equals new { cp.ContractProductId, cp.ContractId }
                join op in context.Operators on t.OperatorId equals op.OperatorId into opJoin
                from op in opJoin.DefaultIfEmpty()
                where t.TransactionType == "Sale"
                      && t.TransactionDate >= startDate
                      && t.TransactionDate < endDate.Date.AddDays(1)
                      && m.MerchantReportingId == request.Request.MerchantReportingId
                group t by new
                {
                    cp.ContractProductReportingId,
                    OperatorName = op == null ? string.Empty : op.Name,
                    cp.ProductName,
                    t.IsAuthorised
                }
                into g
                select new MerchantDailyPerformanceGroupProjection
                {
                    ContractProductReportingId = g.Key.ContractProductReportingId,
                    ProductName = g.Key.ProductName,
                    OperatorName = g.Key.OperatorName,
                    IsAuthorised = g.Key.IsAuthorised,
                    SalesCount = g.Count(),
                    SalesValue = g.Sum(x => x.TransactionAmount)
                };

            return await ReportingQueryExecutor.ToListAsync(query, cancellationToken, "Error retrieving merchant daily performance summary");
        }

        private async Task<Result<List<MerchantDailyPerformanceRecentSaleProjection>>> LoadMerchantDailyPerformanceRecentSales(EstateManagementContext context,
                                                                                                                               TransactionQueries.MerchantDailyPerformanceSummaryQuery request,
                                                                                                                               DateTime startDate,
                                                                                                                               DateTime endDate,
                                                                                                                               CancellationToken cancellationToken) {
            IQueryable<MerchantDailyPerformanceRecentSaleProjection> query =
                (from t in context.Transactions
                 join m in context.Merchants on t.MerchantId equals m.MerchantId
                 join cp in context.ContractProducts on new { t.ContractProductId, t.ContractId } equals new { cp.ContractProductId, cp.ContractId }
                 join op in context.Operators on t.OperatorId equals op.OperatorId into opJoin
                 from op in opJoin.DefaultIfEmpty()
                 where t.TransactionType == "Sale"
                       && t.TransactionDate >= startDate
                       && t.TransactionDate < endDate.Date.AddDays(1)
                       && m.MerchantReportingId == request.Request.MerchantReportingId
                  orderby t.TransactionDateTime descending, t.TransactionId descending
                 select new MerchantDailyPerformanceRecentSaleProjection
                  {
                      TransactionId = t.TransactionId,
                      Reference = t.TransactionNumber,
                     Product = cp.ProductName,
                     Operator = op == null ? string.Empty : op.Name,
                     Status = t.IsAuthorised ? "Successful" : "Failed",
                     Amount = t.TransactionAmount,
                     TransactionDateTime = t.TransactionDateTime
                 }).Take(5);

            return await ReportingQueryExecutor.ToListAsync(query, cancellationToken, "Error retrieving merchant recent sales");
        }

        private static List<DrillDownTransaction> MapMerchantDailyPerformanceRecentSales(List<MerchantDailyPerformanceRecentSaleProjection> recentSales) {
            return recentSales.Select(x => new DrillDownTransaction {
                Reference = x.Reference,
                Product = x.Product,
                Operator = x.Operator,
                Status = x.Status,
                Amount = x.Amount,
                TransactionDateTime = x.TransactionDateTime
            }).ToList();
        }

        private sealed class RecentActivityReceiptQueryResult {
            public Guid TransactionId { get; init; }
            public DateTime TransactionDateTime { get; init; }
            public int MerchantReportingId { get; init; }
            public string Reference { get; init; }
            public string? TransactionType { get; init; }
            public string? Product { get; init; }
            public string? Operator { get; init; }
            public string? Status { get; init; }
            public decimal Amount { get; init; }
            public string? ReceiptReference { get; init; }
        }

        private sealed class RecentActivityReceiptRequestParameters {
            public DateOnly ReportDate { get; init; }
            public int? MerchantReportingId { get; init; }
            public string? SearchText { get; init; }
            public int PageNumber { get; init; }
            public int PageSize { get; init; }
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
            public Int32 TransactionNumber { get; init; }
    }

        private sealed class TransactionDetailSummaryQueryResult {
            public int TransactionCount { get; init; }
            public decimal TotalValue { get; init; }
            public decimal TotalFees { get; init; }
        }

        private sealed class TransactionMixGroupQueryResult {
            public int? NumericKey { get; init; }
            public string? TextKey { get; init; }
            public string? GroupName { get; init; }
            public int TransactionCount { get; init; }
            public decimal TransactionValue { get; init; }
        }

        private sealed record MerchantSummaryReportData(
            List<MerchantTransactionFinalProjection> Merchants,
            int TotalMerchants,
            int TotalCount,
            decimal TotalValue);

        private sealed record OperatorSummaryReportData(
            List<OperatorSummaryData> Operators,
            int TotalOperators,
            int TotalCount,
            decimal TotalValue);

        private sealed record ProductPerformanceReportData(
            List<ProductPerformanceItemData> Products,
            int TotalProducts,
            int TotalCount,
            decimal TotalValue);

        private sealed record ProductPerformanceTotals(
            int TotalProducts,
            int TotalCount,
            decimal TotalValue);

        private sealed record TransactionMixReportData(
            List<TransactionDetailQueryResult> Transactions,
            List<TransactionMixGroupQueryResult> Groups,
            int TotalCount,
            decimal TotalValue);

        private sealed class OperatorTransactionData {
            public Guid MerchantId { get; init; }
            public int MerchantReportingId { get; init; }
            public string? MerchantName { get; init; }
            public Guid OperatorId { get; init; }
            public int OperatorReportingId { get; init; }
            public string? OperatorName { get; init; }
            public int TotalCount { get; init; }
            public decimal TotalValue { get; init; }
            public int AuthorisedCount { get; init; }
            public int DeclinedCount { get; init; }
        }

        private sealed class OperatorSummaryData {
            public Guid OperatorId { get; init; }
            public int OperatorReportingId { get; init; }
            public string? OperatorName { get; init; }
            public int TotalCount { get; init; }
            public decimal TotalValue { get; init; }
            public decimal AverageValue { get; init; }
            public int AuthorisedCount { get; init; }
            public int DeclinedCount { get; init; }
            public decimal AuthorisedPercentage { get; init; }
        }

        private sealed class ProductPerformanceItemData {
            public string? ProductName { get; init; }
            public Guid ContractProductId { get; init; }
            public int ContractProductReportingId { get; init; }
            public Guid ContractId { get; init; }
            public int ContractReportingId { get; init; }
            public int TransactionCount { get; init; }
            public decimal TotalAmount { get; init; }
            public decimal PercentOfTotalAmount { get; init; }
        }
}
