using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Results.Web;

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
}