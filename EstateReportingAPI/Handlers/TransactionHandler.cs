using EstateReportingAPI.BusinessLogic.Queries;
using System.ComponentModel;
using EstateReportingAPI.Common;
using EstateReportingAPI.DataTransferObjects;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TransactionProcessing.UnionResults.AspNetCore;
using TransactionProcessing.UnionResults;
using TransactionProcessor.Database.Entities.Summary;
using TodaysSales = EstateReportingAPI.Models.TodaysSales;

namespace EstateReportingAPI.Handlers;

public static class TransactionHandler {
    public static async Task<Result<TodaysSales>> TodaysSales(IEstateContext estateContext,
                                                  [FromQuery, Description("Optional merchant reporting identifier used to limit results to one merchant.")] int? merchantReportingId,
                                                  [FromQuery, Description("Optional operator reporting identifier used to limit results to one operator.")] int? operatorReportingId,
                                                  [FromQuery, Description("Date used to compare today's sales with the corresponding day (YYYY-MM-DD).") ] DateOnly comparisonDate,
                                                  IMediator mediator,
                                                  CancellationToken cancellationToken)
    {
        var query = new TransactionQueries.TodaysSalesQuery(estateContext.EstateId, merchantReportingId.GetValueOrDefault(), operatorReportingId.GetValueOrDefault(), comparisonDate.ToDateTime(TimeOnly.MinValue));
        var result = await mediator.Send(query, cancellationToken);

        return result.Map(r => new TodaysSales
        {
            ComparisonSalesCount = r.ComparisonSalesCount,
            ComparisonSalesValue = r.ComparisonSalesValue,
            TodaysSalesCount = r.TodaysSalesCount,
            TodaysSalesValue = r.TodaysSalesValue,
            ComparisonAverageSalesValue = r.ComparisonAverageSalesValue,
            TodaysAverageSalesValue = r.TodaysAverageSalesValue,
        });
    }

    public static async Task<Result<TodaysSales>> TodaysFailedSales(IEstateContext estateContext,
                                                        [FromQuery, Description("Date used to compare failed sales with the corresponding day (YYYY-MM-DD).") ] DateOnly comparisonDate,
                                                        [FromQuery, Description("Response code used to filter failed transactions.")] string responseCode,
                                                        IMediator mediator,
                                                        CancellationToken cancellationToken)
    {
        var query = new TransactionQueries.TodaysFailedSales(estateContext.EstateId, comparisonDate.ToDateTime(TimeOnly.MinValue), responseCode);
        var result = await mediator.Send(query, cancellationToken);

        return result.Map(r => new TodaysSales
        {
            ComparisonSalesCount = r.ComparisonSalesCount,
            ComparisonSalesValue = r.ComparisonSalesValue,
            TodaysSalesCount = r.TodaysSalesCount,
            TodaysSalesValue = r.TodaysSalesValue,
            ComparisonAverageSalesValue = r.ComparisonAverageSalesValue,
            TodaysAverageSalesValue = r.TodaysAverageSalesValue,
        });
    }

    public static async Task<Result<TransactionDetailReportResponse>> TransactionDetailReport(IEstateContext estateContext,
                                                              [FromBody] TransactionDetailReportRequest request, 
                                                              BusinessLogic.ReportingDatePolicy datePolicy,
                                                              IMediator mediator, CancellationToken cancellationToken) {
        string? validationError = BusinessLogic.ReportQueryValidation.ValidateDateRange(
                                      request.StartDate,
                                      request.EndDate)
                                  ?? BusinessLogic.ReportQueryValidation.ValidatePaging(request.PageNumber, request.PageSize)
                                  ?? BusinessLogic.ReportQueryValidation.ValidateFilterList("merchants", request.Merchants)
                                  ?? BusinessLogic.ReportQueryValidation.ValidateFilterList("operators", request.Operators)
                                  ?? BusinessLogic.ReportQueryValidation.ValidateFilterList("products", request.Products);
        if (validationError != null)
            return InvalidReportQuery(validationError);

        Models.TransactionDetailReportRequest queryRequest = new() {
            Merchants = request.Merchants,
            Operators = request.Operators,
            Products = request.Products,
            StartDate = request.StartDate,
             EndDate = request.EndDate,
             PageNumber = request.PageNumber,
             PageSize = request.PageSize
        };

        var query = new TransactionQueries.TransactionDetailReportQuery(estateContext.EstateId, queryRequest);
        var result = await mediator.Send(query, cancellationToken);

        TransactionDetailReportResponse SuccessFactory (Models.TransactionDetailReportResponse r) =>
            new TransactionDetailReportResponse {
                Summary = new TransactionDetailSummary { TotalFees = r.Summary.TotalFees, TotalValue = r.Summary.TotalValue, TransactionCount = r.Summary.TransactionCount },
                 Pagination = new PaginationMetadata {
                     PageNumber = r.Pagination.PageNumber,
                     PageSize = r.Pagination.PageSize,
                     TotalItems = r.Pagination.TotalItems,
                     TotalPages = r.Pagination.TotalPages
                 },
                Transactions = r.Transactions.Select(t => new TransactionDetail {
                        TotalFees = t.TotalFees,
                        DateTime = t.DateTime,
                        Id = t.Id,
                        Merchant = t.Merchant,
                        Operator = t.Operator,
                        Product = t.Product,
                        SettlementReference = t.SettlementReference,
                        Status = t.Status,
                        Type = t.Type,
                        Value = t.Value,
                        MerchantId = t.MerchantId,
                        OperatorReportingId = t.OperatorReportingId,
                        OperatorId = t.OperatorId,
                        MerchantReportingId = t.MerchantReportingId,
                        ProductId = t.ProductId,
                        ProductReportingId = t.ProductReportingId,
                        TransactionNumber = t.TransactionNumber

                })
                    .ToList()
            };

        return result.Map(SuccessFactory);
    }

    public static async Task<Result<TransactionSummaryByMerchantResponse>> TransactionSummaryByMerchantReport(IEstateContext estateContext,
                                                              [FromBody] TransactionSummaryByMerchantRequest request, 
                                                              IMediator mediator, CancellationToken cancellationToken) {
        string? validationError = BusinessLogic.ReportQueryValidation.ValidateDateRange(
                                      request.StartDate,
                                      request.EndDate)
                                  ?? BusinessLogic.ReportQueryValidation.ValidatePaging(request.PageNumber, request.PageSize)
                                  ?? BusinessLogic.ReportQueryValidation.ValidateFilterList("merchants", request.Merchants)
                                  ?? BusinessLogic.ReportQueryValidation.ValidateFilterList("operators", request.Operators);
        if (validationError != null)
            return InvalidReportQuery(validationError);

        Models.TransactionSummaryByMerchantRequest queryRequest = new() {
            Merchants = request.Merchants,
            Operators = request.Operators,
            StartDate = request.StartDate,
             EndDate = request.EndDate,
             PageNumber = request.PageNumber,
             PageSize = request.PageSize
        };
        var query = new TransactionQueries.TransactionSummaryByMerchantQuery(estateContext.EstateId, queryRequest);
        var result = await mediator.Send(query, cancellationToken);
        TransactionSummaryByMerchantResponse SuccessFactory (Models.TransactionSummaryByMerchantResponse r) =>
            new TransactionSummaryByMerchantResponse {
                Summary = new MerchantDetailSummary { TotalMerchants = r.Summary.TotalMerchants, TotalCount = r.Summary.TotalCount, TotalValue = r.Summary.TotalValue, AverageValue = r.Summary.AverageValue },
                Pagination = new PaginationMetadata {
                    PageNumber = r.Pagination.PageNumber,
                    PageSize = r.Pagination.PageSize,
                    TotalItems = r.Pagination.TotalItems,
                    TotalPages = r.Pagination.TotalPages
                },
                Merchants = r.Merchants.Select(m => new MerchantDetail {
                        MerchantId = m.MerchantId,
                        MerchantName = m.MerchantName,
                        MerchantReportingId = m.MerchantReportingId,
                        TotalCount = m.TotalCount,
                        TotalValue = m.TotalValue,
                        AverageValue = m.AverageValue,
                        AuthorisedCount = m.AuthorisedCount,
                        DeclinedCount = m.DeclinedCount,
                        AuthorisedPercentage = m.AuthorisedPercentage
                    })
                    .ToList()
            };
        return result.Map(SuccessFactory);
    }

    public static async Task<Result<ProductPerformanceResponse>> ProductPerformanceReport(IEstateContext estateContext,
                                                               [FromQuery, Description("Inclusive first date in the report period (YYYY-MM-DD).") ] DateOnly startDate,
                                                               [FromQuery, Description("Inclusive last date in the report period (YYYY-MM-DD).") ] DateOnly endDate,
                                                               [FromQuery, Description("One-based page number. Defaults to 1.")] int? pageNumber,
                                                               [FromQuery, Description("Maximum results per page. Defaults to 50.")] int? pageSize,
                                                               IMediator mediator,
                                                               CancellationToken cancellationToken) {
        int requestedPageNumber = pageNumber ?? 1;
        int requestedPageSize = pageSize ?? 50;
        string? validationError = BusinessLogic.ReportQueryValidation.ValidateDateRange(startDate, endDate)
                                  ?? BusinessLogic.ReportQueryValidation.ValidatePaging(requestedPageNumber, requestedPageSize);
        if (validationError != null)
            return InvalidReportQuery(validationError);

        var query = new TransactionQueries.ProductPerformanceQuery(estateContext.EstateId, startDate, endDate, requestedPageNumber, requestedPageSize);
        var result = await mediator.Send(query, cancellationToken);
        ProductPerformanceResponse SuccessFactory(Models.ProductPerformanceResponse r) =>
            new ProductPerformanceResponse
            {
                Summary = new ProductPerformanceSummary { TotalProducts = r.Summary.TotalProducts, TotalCount = r.Summary.TotalCount, TotalValue = r.Summary.TotalValue, AveragePerProduct = r.Summary.AveragePerProduct },
                 Pagination = new PaginationMetadata {
                     PageNumber = r.Pagination.PageNumber,
                     PageSize = r.Pagination.PageSize,
                     TotalItems = r.Pagination.TotalItems,
                     TotalPages = r.Pagination.TotalPages
                 },
                ProductDetails = r.ProductDetails.Select(p => new ProductPerformanceDetail
                    {
                        ProductId = p.ProductId,
                        ProductName = p.ProductName,
                        ProductReportingId = p.ProductReportingId,
                        ContractId = p.ContractId,
                        ContractReportingId = p.ContractReportingId,
                        PercentageOfTotal = p.PercentageOfTotal,
                        TransactionCount = p.TransactionCount,
                        TransactionValue = p.TransactionValue,
                    })
                    .ToList()
            };
        return result.Map(SuccessFactory);
    }

    public static async Task<Result<TransactionSummaryByOperatorResponse>> TransactionSummaryByOperatorReport(IEstateContext estateContext,
                                                                         [FromBody] TransactionSummaryByOperatorRequest request,
                                                                         IMediator mediator, CancellationToken cancellationToken)
    {
        string? validationError = BusinessLogic.ReportQueryValidation.ValidateDateRange(
                                      request.StartDate,
                                      request.EndDate)
                                  ?? BusinessLogic.ReportQueryValidation.ValidatePaging(request.PageNumber, request.PageSize)
                                  ?? BusinessLogic.ReportQueryValidation.ValidateFilterList("merchants", request.Merchants)
                                  ?? BusinessLogic.ReportQueryValidation.ValidateFilterList("operators", request.Operators);
        if (validationError != null)
            return InvalidReportQuery(validationError);

        Models.TransactionSummaryByOperatorRequest queryRequest = new()
        {
            Merchants = request.Merchants,
            Operators = request.Operators,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
        var query = new TransactionQueries.TransactionSummaryByOperatorQuery(estateContext.EstateId, queryRequest);
        var result = await mediator.Send(query, cancellationToken);
        TransactionSummaryByOperatorResponse SuccessFactory(Models.TransactionSummaryByOperatorResponse r) =>
            new TransactionSummaryByOperatorResponse
            {
                Summary = new OperatorDetailSummary { TotalOperators = r.Summary.TotalOperators, TotalCount = r.Summary.TotalCount, TotalValue = r.Summary.TotalValue, AverageValue = r.Summary.AverageValue },
                Pagination = new PaginationMetadata {
                    PageNumber = r.Pagination.PageNumber,
                    PageSize = r.Pagination.PageSize,
                    TotalItems = r.Pagination.TotalItems,
                    TotalPages = r.Pagination.TotalPages
                },
                Operators = r.Operators.Select(o => new OperatorDetail
                    {
                        OperatorId = o.OperatorId,
                        OperatorName = o.OperatorName,
                        OperatorReportingId = o.OperatorReportingId,
                        TotalCount = o.TotalCount,
                        TotalValue = o.TotalValue,
                        AverageValue = o.AverageValue,
                        AuthorisedCount = o.AuthorisedCount,
                        DeclinedCount = o.DeclinedCount,
                        AuthorisedPercentage = o.AuthorisedPercentage
                    })
                    .ToList()
            };
        return result.Map(SuccessFactory);
    }

    public static async Task<Result<TransactionMixSummaryResponse>> TransactionMixSummary(IEstateContext estateContext,
                                                            [FromBody] TransactionMixSummaryRequest request,
                                                            BusinessLogic.ReportingDatePolicy datePolicy,
                                                            IMediator mediator,
                                                            CancellationToken cancellationToken)
    {
        string? validationError = BusinessLogic.ReportQueryValidation.ValidateDateRange(
                                      request.StartDate,
                                      request.EndDate)
                                  ?? BusinessLogic.ReportQueryValidation.ValidatePaging(request.PageNumber, request.PageSize)
                                  ?? BusinessLogic.ReportQueryValidation.ValidateTopN(request.TopN);
        if (validationError != null)
            return InvalidReportQuery(validationError);
        if (!Enum.IsDefined(typeof(TransactionMixBreakdown), request.Breakdown))
            return InvalidReportQuery("breakdown is not supported.");
        if (!Enum.IsDefined(typeof(TransactionMixMeasure), request.Measure))
            return InvalidReportQuery("measure is not supported.");

        Models.TransactionMixSummaryRequest queryRequest = new()
        {
            MerchantReportingId = request.MerchantReportingId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Breakdown = (Models.TransactionMixBreakdown)request.Breakdown,
            Measure = (Models.TransactionMixMeasure)request.Measure,
             TopN = request.TopN,
             PageNumber = request.PageNumber,
             PageSize = request.PageSize
        };

        var query = new TransactionQueries.TransactionMixSummaryQuery(estateContext.EstateId, queryRequest);
        var result = await mediator.Send(query, cancellationToken);

        TransactionMixSummaryResponse SuccessFactory(Models.TransactionMixSummaryResponse r) =>
            new TransactionMixSummaryResponse
            {
                FromDate = r.FromDate,
                ToDate = r.ToDate,
                Breakdown = (TransactionMixBreakdown)r.Breakdown,
                Measure = (TransactionMixMeasure)r.Measure,
                TotalCount = r.TotalCount,
                TotalValue = r.TotalValue,
                 Pagination = new PaginationMetadata {
                     PageNumber = r.Pagination.PageNumber,
                     PageSize = r.Pagination.PageSize,
                     TotalItems = r.Pagination.TotalItems,
                     TotalPages = r.Pagination.TotalPages
                 },
                Groups = r.Groups.Select(g => new TransactionMixSummaryGroup
                {
                    GroupKey = g.GroupKey,
                    GroupName = g.GroupName,
                    TransactionCount = g.TransactionCount,
                    TransactionValue = g.TransactionValue
                }).ToList(),
                Transactions = r.Transactions.Select(t => new TransactionMixSummaryTransaction
                {
                    Id = t.Id,
                    DateTime = t.DateTime,
                    Merchant = t.Merchant,
                    MerchantId = t.MerchantId,
                    MerchantReportingId = t.MerchantReportingId,
                    Operator = t.Operator,
                    OperatorId = t.OperatorId,
                    OperatorReportingId = t.OperatorReportingId,
                    Product = t.Product,
                    ProductId = t.ProductId,
                    ProductReportingId = t.ProductReportingId,
                    Type = t.Type,
                    Status = t.Status,
                    Value = t.Value,
                    TotalFees = t.TotalFees,
                    SettlementReference = t.SettlementReference,
                    TransactionNumber = t.TransactionNumber
                }).ToList()
            };

        return result.Map(SuccessFactory);
    }

    public static async Task<Result<GetRecentActivityReceiptReportResponse>> RecentActivityReceiptReport(IEstateContext estateContext,
                                                                  [FromBody] GetRecentActivityReceiptReportRequest request,
                                                                  BusinessLogic.ReportingDatePolicy datePolicy,
                                                                  IMediator mediator,
                                                                  CancellationToken cancellationToken)
    {
        string? validationError = BusinessLogic.ReportQueryValidation.ValidatePaging(request.PageNumber, request.PageSize);
        if (validationError != null)
            return InvalidReportQuery(validationError);

        Models.GetRecentActivityReceiptReportRequest queryRequest = new()
        {
            ReportDate = request.ReportDate.ToDateTime(TimeOnly.MinValue),
            MerchantReportingId = request.MerchantReportingId,
            SearchText = request.SearchText,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        var query = new TransactionQueries.GetRecentActivityReceiptReportQuery(estateContext.EstateId, queryRequest);
        var result = await mediator.Send(query, cancellationToken);

        GetRecentActivityReceiptReportResponse SuccessFactory(Models.GetRecentActivityReceiptReportResponse r) =>
            new GetRecentActivityReceiptReportResponse
            {
                ReportDate = r.ReportDate,
                Pagination = new PaginationMetadata {
                    PageNumber = r.Pagination.PageNumber,
                    PageSize = r.Pagination.PageSize,
                    TotalItems = r.Pagination.TotalItems,
                    TotalPages = r.Pagination.TotalPages
                },
                Items = r.Items.Select(item => new RecentActivityReceiptItemDto
                {
                    Reference = item.Reference,
                    TransactionType = item.TransactionType,
                    Product = item.Product,
                    Operator = item.Operator,
                    Status = item.Status,
                    Amount = item.Amount,
                    TransactionDateTime = item.TransactionDateTime,
                    ReceiptReference = item.ReceiptReference
                }).ToList()
            };

        return result.Map(SuccessFactory);
    }

    public static async Task<Result<List<DataTransferObjects.TodaysSalesByHour>>> TodaysSalesByHour(IEstateContext estateContext,
                                                        [FromQuery, Description("Date used to compare hourly sales with the corresponding day (YYYY-MM-DD).") ] DateOnly comparisonDate,
                                                        IMediator mediator,
                                                        CancellationToken cancellationToken)
    {
        var query = new TransactionQueries.TodaysSalesByHour(estateContext.EstateId, comparisonDate.ToDateTime(TimeOnly.MinValue));
        var result = await mediator.Send(query, cancellationToken);

        List<DataTransferObjects.TodaysSalesByHour> SuccessFactory(List<Models.TodaysSalesByHour> r) =>
            r.Select(item => new DataTransferObjects.TodaysSalesByHour
            {
                Hour = item.Hour,
                ComparisonSalesCount = item.ComparisonSalesCount,
                TodaysSalesCount = item.TodaysSalesCount,
                ComparisonSalesValue = item.ComparisonSalesValue,
                TodaysSalesValue = item.TodaysSalesValue
            }).ToList();    


        return result.Map(SuccessFactory);
    }

    public static async Task<Result<MerchantDailyPerformanceSummaryResponse>> MerchantDailyPerformanceSummary(IEstateContext estateContext,
                                                                      [FromBody] MerchantDailyPerformanceSummaryRequest request,
                                                                      BusinessLogic.ReportingDatePolicy datePolicy,
                                                                      IMediator mediator, CancellationToken cancellationToken)
    {
        string? validationError = BusinessLogic.ReportQueryValidation.ValidateDateRange(
            request.StartDate,
            request.EndDate);
        if (validationError != null)
            return InvalidReportQuery(validationError);

        TransactionQueries.MerchantDailyPerformanceSummaryQuery query = new(estateContext.EstateId, new Models.MerchantDailyPerformanceSummaryRequest {
            EndDate = request.EndDate,
            MerchantReportingId = request.MerchantReportingId,
            StartDate = request.StartDate
        });
        Result<Models.MerchantDailyPerformanceSummaryResponse> result = await mediator.Send(query, cancellationToken);

        MerchantDailyPerformanceSummaryResponse SuccessFactory(Models.MerchantDailyPerformanceSummaryResponse r) =>
            new MerchantDailyPerformanceSummaryResponse
            {
                Metrics = r.Metrics?.Select(metric => new MetricItem
                {
                    Title = metric.Title,
                    Value = metric.Value,
                    Description = metric.Description,
                    Category = metric.Category,
                    Type = metric.Type
                }).ToList() ?? [],
                DrillDownTransactions = r.DrillDownTransactions?.Select(transaction => new DrillDownTransaction
                {
                    Reference = transaction.Reference,
                    Product = $"{transaction.Operator} {transaction.Product}",
                    Status = transaction.Status,
                    Amount = transaction.Amount,
                    TransactionDateTime = transaction.TransactionDateTime
                }).ToList() ?? []
            };

        return result.Map(SuccessFactory);
    }

    private static Error InvalidReportQuery(string detail) => Error.Validation(detail);
}

