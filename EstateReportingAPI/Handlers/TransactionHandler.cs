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
                        Value = t.Value
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
}