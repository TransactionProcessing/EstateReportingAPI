using EstateReportingAPI.BusinessLogic.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.DynamicLinq;
using SimpleResults;
using System;
using System.Linq;
using System.Threading;
using TransactionProcessor.Database.Contexts;
using TransactionProcessor.Database.Entities;
using TransactionProcessor.Database.Entities.Summary;
using Shared.EntityFramework;
using Shared.Results;
using EstateReportingAPI.Models;
using MerchantBalanceProjectionState = TransactionProcessor.ProjectionEngine.Database.Database.Entities.MerchantBalanceProjectionState;

namespace EstateReportingAPI.BusinessLogic;

public sealed class TransactionReportingService : ITransactionReportingService
{
    private readonly IDbContextResolver<EstateManagementContext> Resolver;
    private const string EstateManagementDatabaseName = "TransactionProcessorReadModel";

    public TransactionReportingService(IDbContextResolver<EstateManagementContext> resolver)
    {
        Resolver = resolver;
}

    public async Task<Result<TodaysSales>> GetTodaysFailedSales(TransactionQueries.TodaysFailedSales request,
                                                                CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        IQueryable<Decimal> todaysSalesQuery = this.BuildTodaysFailedSalesQuery(context, request.ResponseCode);
        IQueryable<Decimal> comparisonSalesQuery = this.BuildComparisonFailedSalesQuery(context, request.ComparisonDate, request.ResponseCode);

        var todaysSalesQueryResult = await ReportingQueryExecutor.ToListAsync(todaysSalesQuery, cancellationToken, "Error retrieving todays failed sales");
        if (todaysSalesQueryResult.IsFailed)
            return ResultHelpers.CreateFailure(todaysSalesQueryResult);
        var comparisonSalesQueryResult = await ReportingQueryExecutor.ToListAsync(comparisonSalesQuery, cancellationToken, "Error retrieving comparison failed sales");
        if (comparisonSalesQueryResult.IsFailed)
            return ResultHelpers.CreateFailure(comparisonSalesQueryResult);
        var todaysSales = todaysSalesQueryResult.Data;
        var comparisonSales = comparisonSalesQueryResult.Data;

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

        IQueryable<TodayTransaction> todaysSales = this.BuildTodaySalesQuery(context);
        IQueryable<TransactionHistory> comparisonSales = this.BuildComparisonSalesQuery(context, request.ComparisonDate);

        todaysSales = todaysSales.ApplyMerchantFilter(request.MerchantReportingId).ApplyOperatorFilter(request.OperatorReportingId);
        comparisonSales = comparisonSales.ApplyMerchantFilter(request.MerchantReportingId).ApplyOperatorFilter(request.OperatorReportingId);

        var todaysSalesQueryResult = await ReportingQueryExecutor.ToListAsync(todaysSales, cancellationToken, "Error retrieving todays failed sales");
        if (todaysSalesQueryResult.IsFailed)
            return ResultHelpers.CreateFailure(todaysSalesQueryResult);
        var comparisonSalesQueryResult = await ReportingQueryExecutor.ToListAsync(comparisonSales, cancellationToken, "Error retrieving comparison failed sales");
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
    public async Task<Result<TransactionDetailReportResponse>> GetTransactionDetailReport(TransactionQueries.TransactionDetailReportQuery request,
                                                                                          CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        var query = ApplyTransactionDetailFilters(BuildTransactionDetailBaseQuery(context, request.Request), request.Request);
        var queryResult = await ReportingQueryExecutor.ToListAsync(query, cancellationToken, "Error retrieving transaction details report");

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
                SettlementReference = q.SettlementId.ToString(),
                TransactionNumber = q.TransactionNumber
            }).ToList(),
            Summary = new TransactionDetailSummary { TransactionCount = queryResults.Count(), TotalValue = queryResults.Sum(q => q.Value), TotalFees = queryResults.Sum(q => q.FeeValue) }
        };
    }

    public async Task<Result<TransactionSummaryByMerchantResponse>> GetTransactionSummaryByMerchantReport(TransactionQueries.TransactionSummaryByMerchantQuery request,
                                                                                                          CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        var query = BuildMerchantTransactionBaseQuery(context, request.Request.StartDate, request.Request.EndDate);

        if (request.Request.Merchants != null && request.Request.Merchants.Any())
            query = query.Where(q => request.Request.Merchants.Contains(q.MerchantReportingId));

        if (request.Request.Operators != null && request.Request.Operators.Any())
            query = query.Where(q => request.Request.Operators.Contains(q.OperatorReportingId));

        var finalQuery = BuildMerchantTransactionFinalQuery(query);

        var queryResult = await ReportingQueryExecutor.ToListAsync(finalQuery, cancellationToken, "Error retrieving transaction summary by merchant report");

        if (queryResult.IsFailed)
            return ResultHelpers.CreateFailure(queryResult);

        var queryResults = queryResult.Data;

        if (queryResults.Any() == false)
            return new TransactionSummaryByMerchantResponse { Summary = new MerchantDetailSummary(), Merchants = new List<MerchantDetail>() };

        return new TransactionSummaryByMerchantResponse {
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
            Summary = new MerchantDetailSummary {
                TotalCount = queryResults.Sum(q => q.TotalCount),
                TotalValue = queryResults.Sum(q => q.TotalValue),
                AverageValue = SafeDivide(queryResults.Sum(q => q.TotalValue), queryResults.Sum(q => q.TotalCount)),
                TotalMerchants = queryResults.Count()
            }
        };
    }

    private static IQueryable<MerchantTransactionGroupProjection> BuildMerchantTransactionBaseQuery(EstateManagementContext context,
                                                                                                    DateTime startDate,
                                                                                                    DateTime endDate) {
        return from t in context.Transactions
            join m in context.Merchants on t.MerchantId equals m.MerchantId
            join o in context.Operators on t.OperatorId equals o.OperatorId
            where t.TransactionType == "Sale" && t.TransactionDate >= startDate && t.TransactionDate <= endDate
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
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        var baseQuery = BuildOperatorTransactionBaseQuery(context, request);
        var finalQuery = BuildOperatorFinalSummaryQuery(baseQuery);

        var queryResult = await ReportingQueryExecutor.ToListAsync(finalQuery, cancellationToken, "Error retrieving transaction summary by operator report");

        if (queryResult.IsFailed)
            return ResultHelpers.CreateFailure(queryResult);

        var queryResults = queryResult.Data;

        if (queryResults.Any() == false)
            return new TransactionSummaryByOperatorResponse { Summary = new OperatorDetailSummary(), Operators = new List<OperatorDetail>() };

        return BuildOperatorSummaryResponse(queryResults);
    }

    private static IQueryable<OperatorTransactionData> BuildOperatorTransactionBaseQuery(EstateManagementContext context,
                                                                                         TransactionQueries.TransactionSummaryByOperatorQuery request) {
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

    private TransactionSummaryByOperatorResponse BuildOperatorSummaryResponse(List<OperatorSummaryData> queryResults) {
        return new TransactionSummaryByOperatorResponse {
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
            Summary = new OperatorDetailSummary {
                TotalCount = queryResults.Sum(q => q.TotalCount),
                TotalValue = queryResults.Sum(q => q.TotalValue),
                AverageValue = SafeDivide(queryResults.Sum(q => q.TotalValue), queryResults.Sum(q => q.TotalCount)),
                TotalOperators = queryResults.Count()
            }
        };
    }
    public async Task<Result<ProductPerformanceResponse>> GetProductPerformanceReport(TransactionQueries.ProductPerformanceQuery request,
                                                                                      CancellationToken cancellationToken) {

        using ResolvedDbContext<EstateManagementContext>? resolvedContext = Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        var grandTotalAmountQuery = (from t in context.Transactions where t.TransactionType == "Sale" && t.TransactionDate >= request.StartDate && t.TransactionDate <= request.EndDate select t.TransactionAmount);
        var grandTotalAmountResult = await ReportingQueryExecutor.SumAsync<decimal>(grandTotalAmountQuery, cancellationToken);
        if (grandTotalAmountResult.IsFailed)
            return ResultHelpers.CreateFailure(grandTotalAmountResult);
        var grandTotalAmount = grandTotalAmountResult.Data;

        var query = BuildProductPerformanceQuery(context, request.StartDate, request.EndDate, grandTotalAmount);
        var queryResult = await ReportingQueryExecutor.ToListAsync(query, cancellationToken);
        if (queryResult.IsFailed)
            return ResultHelpers.CreateFailure(queryResult);
        var queryResults = queryResult.Data;
        if (queryResults.Any() == false)
            return Result.Success(new ProductPerformanceResponse() { Summary = new ProductPerformanceSummary(), ProductDetails = new List<ProductPerformanceDetail>() });

        return Result.Success(BuildProductPerformanceResponse(queryResults));
    }

    public async Task<Result<TransactionMixSummaryResponse>> GetTransactionMixSummary(TransactionQueries.TransactionMixSummaryQuery request,
                                                                                      CancellationToken cancellationToken)
    {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        if (request.Request.StartDate > request.Request.EndDate)
            return Result.Failure("StartDate must be less than or equal to EndDate.");

        if (!Enum.IsDefined(typeof(TransactionMixBreakdown), request.Request.Breakdown))
            return Result.Failure("Unsupported transaction mix breakdown.");

        if (!Enum.IsDefined(typeof(TransactionMixMeasure), request.Request.Measure))
            return Result.Failure("Unsupported transaction mix measure.");

        var detailRequest = new TransactionDetailReportRequest
        {
            Merchants = request.Request.MerchantReportingId.HasValue ? new List<int> { request.Request.MerchantReportingId.Value } : [],
            Operators = [],
            Products = [],
            StartDate = request.Request.StartDate,
            EndDate = request.Request.EndDate
        };

        var query = ApplyTransactionDetailFilters(BuildTransactionDetailBaseQuery(context, detailRequest), detailRequest);
        var queryResult = await ReportingQueryExecutor.ToListAsync(query, cancellationToken, "Error retrieving transaction mix summary report");

        if (queryResult.IsFailed)
            return ResultHelpers.CreateFailure(queryResult);

        var queryResults = queryResult.Data;

        if (queryResults.Any() == false)
        {
            return Result.Success(new TransactionMixSummaryResponse
            {
                FromDate = request.Request.StartDate,
                ToDate = request.Request.EndDate,
                Breakdown = request.Request.Breakdown,
                Measure = request.Request.Measure,
                TotalCount = 0,
                TotalValue = 0m,
                Groups = [],
                Transactions = []
            });
        }

        return Result.Success(BuildTransactionMixSummaryResponse(queryResults, request.Request));
    }

    public async Task<Result<GetRecentActivityReceiptReportResponse>> GetRecentActivityReceiptReport(TransactionQueries.GetRecentActivityReceiptReportQuery request,
                                                                                                     CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        RecentActivityReceiptRequestParameters parameters = BuildRecentActivityReceiptRequestParameters(request.Request);
        IQueryable<RecentActivityReceiptQueryResult> query = BuildRecentActivityReceiptQuery(context, parameters);

        var totalCountResult = await ReportingQueryExecutor.CountAsync(query, cancellationToken, "Error counting recent activity receipt report rows");
        if (totalCountResult.IsFailed)
            return ResultHelpers.CreateFailure(totalCountResult);

        int totalCount = totalCountResult.Data;
        if (totalCount == 0)
            return Result.Success(BuildEmptyRecentActivityReceiptReportResponse(parameters));

        var pageQuery = query.Skip((parameters.PageNumber - 1) * parameters.PageSize).Take(parameters.PageSize);
        var pageResult = await ReportingQueryExecutor.ToListAsync(pageQuery, cancellationToken, "Error retrieving recent activity receipt report");
        if (pageResult.IsFailed)
            return ResultHelpers.CreateFailure(pageResult);

        return Result.Success(BuildRecentActivityReceiptReportResponse(parameters, totalCount, pageResult.Data));
    }

    private static RecentActivityReceiptRequestParameters BuildRecentActivityReceiptRequestParameters(GetRecentActivityReceiptReportRequest request) {
        return new RecentActivityReceiptRequestParameters {
            ReportDate = request.ReportDate.Date,
            MerchantReportingId = request.MerchantReportingId,
            SearchText = request.SearchText?.Trim(),
            PageNumber = request.PageNumber > 0 ? request.PageNumber : 1,
            PageSize = request.PageSize > 0 ? request.PageSize : 10
        };
    }

    private static IQueryable<RecentActivityReceiptQueryResult> BuildRecentActivityReceiptQuery(EstateManagementContext context,
                                                                                                RecentActivityReceiptRequestParameters parameters) {
        IQueryable<RecentActivityReceiptQueryResult> query = from t in context.Transactions
            join m in context.Merchants on t.MerchantId equals m.MerchantId
            join o in context.Operators on t.OperatorId equals o.OperatorId
            join cp in context.ContractProducts on new { t.ContractProductId, t.ContractId } equals new { cp.ContractProductId, cp.ContractId }
            where t.TransactionDate == parameters.ReportDate
            select new RecentActivityReceiptQueryResult {
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

        return ApplyRecentActivityReceiptFilters(query, parameters).OrderByDescending(q => q.TransactionDateTime);
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
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize,
            TotalCount = 0,
            Items = []
        };
    }

    private static GetRecentActivityReceiptReportResponse BuildRecentActivityReceiptReportResponse(RecentActivityReceiptRequestParameters parameters,
                                                                                                    int totalCount,
                                                                                                    List<RecentActivityReceiptQueryResult> queryResults) {
        return new GetRecentActivityReceiptReportResponse {
            ReportDate = parameters.ReportDate,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize,
            TotalCount = totalCount,
            Items = queryResults.Select(MapRecentActivityReceiptItem).ToList()
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
            where t.TransactionType == "Sale" && t.TransactionDate >= startDate && t.TransactionDate <= endDate
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

    private ProductPerformanceResponse BuildProductPerformanceResponse(List<ProductPerformanceItemData> queryResults) {
        return new ProductPerformanceResponse {
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
            Summary = new ProductPerformanceSummary { TotalCount = queryResults.Sum(q => q.TransactionCount), TotalValue = queryResults.Sum(q => q.TotalAmount), AveragePerProduct = SafeDivide(queryResults.Sum(q => q.TotalAmount), queryResults.Count), TotalProducts = queryResults.Count() }
        };
    }

    private static TransactionMixSummaryResponse BuildTransactionMixSummaryResponse(List<TransactionDetailQueryResult> queryResults,
                                                                                     TransactionMixSummaryRequest request)
    {
        var groupedResults = queryResults
            .GroupBy(q => request.Breakdown switch
            {
                TransactionMixBreakdown.Product => q.ContractProductReportingId.ToString(),
                TransactionMixBreakdown.TransactionType => q.TransactionType ?? string.Empty,
                TransactionMixBreakdown.Operator => q.OperatorReportingId.ToString(),
                TransactionMixBreakdown.Status => q.Status ?? string.Empty,
                _ => string.Empty
            })
            .Select(g =>
            {
                var first = g.First();
                string groupName = request.Breakdown switch
                {
                    TransactionMixBreakdown.Product => first.ProductName ?? string.Empty,
                    TransactionMixBreakdown.TransactionType => first.TransactionType ?? string.Empty,
                    TransactionMixBreakdown.Operator => first.OperatorName ?? string.Empty,
                    TransactionMixBreakdown.Status => first.Status ?? string.Empty,
                    _ => string.Empty
                };

                return new TransactionMixSummaryGroup
                {
                    GroupKey = g.Key,
                    GroupName = groupName,
                    TransactionCount = g.Count(),
                    TransactionValue = g.Sum(x => x.Value)
                };
            });

        var orderedGroups = request.Measure == TransactionMixMeasure.Count
            ? groupedResults.OrderByDescending(g => g.TransactionCount).ThenBy(g => g.GroupName)
            : groupedResults.OrderByDescending(g => g.TransactionValue).ThenBy(g => g.GroupName);

        int topN = request.TopN > 0 ? request.TopN : 5;

        return new TransactionMixSummaryResponse
        {
            FromDate = request.StartDate,
            ToDate = request.EndDate,
            Breakdown = request.Breakdown,
            Measure = request.Measure,
            TotalCount = queryResults.Count,
            TotalValue = queryResults.Sum(q => q.Value),
            Groups = orderedGroups.Take(topN).ToList(),
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
                .ToList()
        };
    }

    public async Task<Result<List<TodaysSalesByHour>>> GetTodaysSalesByHour(TransactionQueries.TodaysSalesByHour request,
                                                                            CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = Resolver.Resolve(EstateManagementDatabaseName, request.estateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        IQueryable<TodayTransaction> todaysSales = this.BuildTodaySalesQuery(context);
        IQueryable<TransactionHistory> comparisonSales = this.BuildComparisonSalesQuery(context, request.comparisonDate);

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

    public async Task<Result<MerchantDailyPerformanceSummaryResponse>> GetMerchantDailyPerformanceSummary(TransactionQueries.MerchantDailyPerformanceSummaryQuery request,
                                                                                                          CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        DateTime startDate = request.Request.StartDate.Date;
        DateTime endDate = request.Request.EndDate.Date;
        int dayCount = Math.Max((endDate - startDate).Days + 1, 1);

        var groupedTransactionsResult = await LoadMerchantDailyPerformanceGroups(context, request, startDate, endDate, cancellationToken);
        if (groupedTransactionsResult.IsFailed)
            return ResultHelpers.CreateFailure(groupedTransactionsResult);

        var groupedTransactions = groupedTransactionsResult.Data;
        if (groupedTransactions.Any() == false) {
            return Result.Success(BuildEmptyMerchantDailyPerformanceSummaryResponse());
        }

        List<MetricItem> metrics = BuildMerchantDailyPerformanceMetrics(groupedTransactions, dayCount);

        var recentSalesResult = await LoadMerchantDailyPerformanceRecentSales(context, request, startDate, endDate, cancellationToken);
        if (recentSalesResult.IsFailed)
            return ResultHelpers.CreateFailure(recentSalesResult);

        return Result.Success(new MerchantDailyPerformanceSummaryResponse {
            Metrics = metrics,
            DrillDownTransactions = MapMerchantDailyPerformanceRecentSales(recentSalesResult.Data)
        });
    }

        private IQueryable<TodayTransaction> BuildTodaySalesQuery(EstateManagementContext context) {
            return from t in context.TodayTransactions where t.IsAuthorised && t.TransactionType == "Sale" && t.TransactionDate == DateTime.Now.Date && t.TransactionTime <= DateTime.Now.TimeOfDay select t;
        }

        private IQueryable<Decimal> BuildTodaysFailedSalesQuery(EstateManagementContext context,
                                                                String responseCode) {
            return from t in context.TodayTransactions
                   where t.IsAuthorised == false && t.TransactionType == "Sale" && t.ResponseCode == responseCode
                   select t.TransactionAmount;
        }

        private IQueryable<TransactionHistory> BuildComparisonSalesQuery(EstateManagementContext context,
                                                                          DateTime comparisonDate) {
            return from t in context.TransactionHistory where t.IsAuthorised && t.TransactionType == "Sale" && t.TransactionDate == comparisonDate && t.TransactionTime <= DateTime.Now.TimeOfDay select t;
        }

        private IQueryable<Decimal> BuildComparisonFailedSalesQuery(EstateManagementContext context,
                                                                    DateTime comparisonDate,
                                                                    String responseCode) {
            return from t in context.TransactionHistory
                   where t.IsAuthorised == false && t.TransactionType == "Sale" && t.TransactionDate == comparisonDate && t.TransactionTime <= DateTime.Now.TimeOfDay && t.ResponseCode == responseCode
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

            if (topProduct == null)
                return null;

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
            var query =
                from t in context.Transactions
                join m in context.Merchants on t.MerchantId equals m.MerchantId
                join cp in context.ContractProducts on new { t.ContractProductId, t.ContractId } equals new { cp.ContractProductId, cp.ContractId }
                join op in context.Operators on t.OperatorId equals op.OperatorId into opJoin
                from op in opJoin.DefaultIfEmpty()
                where t.TransactionType == "Sale"
                      && t.TransactionDate >= startDate
                      && t.TransactionDate <= endDate
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
            var query =
                (from t in context.Transactions
                 join m in context.Merchants on t.MerchantId equals m.MerchantId
                 join cp in context.ContractProducts on new { t.ContractProductId, t.ContractId } equals new { cp.ContractProductId, cp.ContractId }
                 join op in context.Operators on t.OperatorId equals op.OperatorId into opJoin
                 from op in opJoin.DefaultIfEmpty()
                 where t.TransactionType == "Sale"
                       && t.TransactionDate >= startDate
                       && t.TransactionDate <= endDate
                       && m.MerchantReportingId == request.Request.MerchantReportingId
                 orderby t.TransactionDateTime descending
                 select new MerchantDailyPerformanceRecentSaleProjection
                 {
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
            public DateTime ReportDate { get; init; }
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
