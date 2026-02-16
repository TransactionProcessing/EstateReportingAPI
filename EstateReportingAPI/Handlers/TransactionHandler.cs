using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.DataTransferObjects;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Results.Web;
using TodaysSales = EstateReportingAPI.Models.TodaysSales;

namespace EstateReportingAPI.Handlers;

public static class TransactionHandler {
    public static async Task<IResult> TodaysSales([FromHeader] Guid estateId,
                                                  [FromQuery] int? merchantReportingId,
                                                  [FromQuery] int? operatorReportingId,
                                                  [FromQuery] DateTime comparisonDate,
                                                  IMediator mediator,
                                                  CancellationToken cancellationToken)
    {
        var query = new TransactionQueries.TodaysSalesQuery(estateId, merchantReportingId.GetValueOrDefault(), operatorReportingId.GetValueOrDefault(), comparisonDate);
        var result = await mediator.Send(query, cancellationToken);

        return ResponseFactory.FromResult(result, r => new TodaysSales
        {
            ComparisonSalesCount = r.ComparisonSalesCount,
            ComparisonSalesValue = r.ComparisonSalesValue,
            TodaysSalesCount = r.TodaysSalesCount,
            TodaysSalesValue = r.TodaysSalesValue,
            ComparisonAverageSalesValue = r.ComparisonAverageSalesValue,
            TodaysAverageSalesValue = r.TodaysAverageSalesValue,
        });
    }

    public static async Task<IResult> TodaysFailedSales([FromHeader] Guid estateId,
                                                        [FromQuery] DateTime comparisonDate,
                                                        [FromQuery] string responseCode,
                                                        IMediator mediator,
                                                        CancellationToken cancellationToken)
    {
        var query = new TransactionQueries.TodaysFailedSales(estateId, comparisonDate, responseCode);
        var result = await mediator.Send(query, cancellationToken);

        return ResponseFactory.FromResult(result, r => new TodaysSales
        {
            ComparisonSalesCount = r.ComparisonSalesCount,
            ComparisonSalesValue = r.ComparisonSalesValue,
            TodaysSalesCount = r.TodaysSalesCount,
            TodaysSalesValue = r.TodaysSalesValue,
            ComparisonAverageSalesValue = r.ComparisonAverageSalesValue,
            TodaysAverageSalesValue = r.TodaysAverageSalesValue,
        });
    }

    public static async Task<IResult> TransactionDetailReport([FromHeader] Guid estateId,
                                                              [FromBody] TransactionDetailReportRequest request, 
                                                              IMediator mediator, CancellationToken cancellationToken) {
        Models.TransactionDetailReportRequest queryRequest = new() {
            Merchants = request.Merchants,
            Operators = request.Operators,
            Products = request.Products,
            StartDate = request.StartDate,
            EndDate = request.EndDate
        };

        var query = new TransactionQueries.TransactionDetailReportQuery(estateId, queryRequest);
        var result = await mediator.Send(query, cancellationToken);

        TransactionDetailReportResponse SuccessFactory (Models.TransactionDetailReportResponse r) =>
            new TransactionDetailReportResponse {
                Summary = new TransactionDetailSummary { TotalFees = r.Summary.TotalFees, TotalValue = r.Summary.TotalValue, TransactionCount = r.Summary.TransactionCount },
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
                        ProductReportingId = t.ProductReportingId

                })
                    .ToList()
            };

        return ResponseFactory.FromResult(result, SuccessFactory);
    }

    public static async Task<IResult> TransactionSummaryByMerchantReport([FromHeader] Guid estateId,
                                                              [FromBody] TransactionSummaryByMerchantRequest request, 
                                                              IMediator mediator, CancellationToken cancellationToken) {
        Models.TransactionSummaryByMerchantRequest queryRequest = new() {
            Merchants = request.Merchants,
            Operators = request.Operators,
            StartDate = request.StartDate,
            EndDate = request.EndDate
        };
        var query = new TransactionQueries.TransactionSummaryByMerchantQuery(estateId, queryRequest);
        var result = await mediator.Send(query, cancellationToken);
        TransactionSummaryByMerchantResponse SuccessFactory (Models.TransactionSummaryByMerchantResponse r) =>
            new TransactionSummaryByMerchantResponse {
                Summary = new MerchantDetailSummary { TotalMerchants = r.Summary.TotalMerchants, TotalCount = r.Summary.TotalCount, TotalValue = r.Summary.TotalValue, AverageValue = r.Summary.AverageValue },
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
        return ResponseFactory.FromResult(result, SuccessFactory);
    }

    public static async Task<IResult> ProductPerformanceReport([FromHeader] Guid estateId,
                                                               [FromQuery] DateTime startDate,
                                                               [FromQuery] DateTime endDate,
                                                               IMediator mediator,
                                                               CancellationToken cancellationToken) {
        var query = new TransactionQueries.ProductPerformanceQuery(estateId, startDate, endDate);
        var result = await mediator.Send(query, cancellationToken);
        ProductPerformanceResponse SuccessFactory(Models.ProductPerformanceResponse r) =>
            new ProductPerformanceResponse
            {
                Summary = new ProductPerformanceSummary { TotalProducts = r.Summary.TotalProducts, TotalCount = r.Summary.TotalCount, TotalValue = r.Summary.TotalValue, AveragePerProduct = r.Summary.AveragePerProduct },
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
        return ResponseFactory.FromResult(result, SuccessFactory);
    }

    public static async Task<IResult> TransactionSummaryByOperatorReport([FromHeader] Guid estateId,
                                                                         [FromBody] TransactionSummaryByOperatorRequest request,
                                                                         IMediator mediator, CancellationToken cancellationToken)
    {
        Models.TransactionSummaryByOperatorRequest queryRequest = new()
        {
            Merchants = request.Merchants,
            Operators = request.Operators,
            StartDate = request.StartDate,
            EndDate = request.EndDate
        };
        var query = new TransactionQueries.TransactionSummaryByOperatorQuery(estateId, queryRequest);
        var result = await mediator.Send(query, cancellationToken);
        TransactionSummaryByOperatorResponse SuccessFactory(Models.TransactionSummaryByOperatorResponse r) =>
            new TransactionSummaryByOperatorResponse
            {
                Summary = new OperatorDetailSummary { TotalOperators = r.Summary.TotalOperators, TotalCount = r.Summary.TotalCount, TotalValue = r.Summary.TotalValue, AverageValue = r.Summary.AverageValue },
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
        return ResponseFactory.FromResult(result, SuccessFactory);
    }

    public static async Task<IResult> TodaysSalesByHour([FromHeader] Guid estateId,
                                                             [FromQuery] DateTime comparisonDate,
                                                             IMediator mediator,
                                                             CancellationToken cancellationToken)
    {
        var query = new TransactionQueries.TodaysSalesByHour(estateId, comparisonDate);
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


        return ResponseFactory.FromResult(result, SuccessFactory);
    }

}