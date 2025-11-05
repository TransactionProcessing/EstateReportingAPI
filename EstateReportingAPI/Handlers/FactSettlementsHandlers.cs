using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.DataTransferObjects;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Results.Web;

namespace EstateReportingAPI.Handlers;

public static class FactSettlementsHandlers
{
    public static async Task<IResult> TodaysSettlement([FromHeader] Guid estateId,
                                                       [FromQuery] int? merchantReportingId,
                                                       [FromQuery] int? operatorReportingId,
                                                       [FromQuery] DateTime comparisonDate,
                                                       IMediator mediator,
                                                       CancellationToken cancellationToken)
    {
        var query = new SettlementQueries.GetTodaysSettlementQuery(
            estateId, merchantReportingId.GetValueOrDefault(), operatorReportingId.GetValueOrDefault(), comparisonDate);

        var result = await mediator.Send(query, cancellationToken);

        return ResponseFactory.FromResult(result, r => new TodaysSettlement
        {
            ComparisonSettlementCount = r.ComparisonSettlementCount,
            ComparisonSettlementValue = r.ComparisonSettlementValue,
            ComparisonPendingSettlementCount = r.ComparisonPendingSettlementCount,
            ComparisonPendingSettlementValue = r.ComparisonPendingSettlementValue,
            TodaysSettlementCount = r.TodaysSettlementCount,
            TodaysSettlementValue = r.TodaysSettlementValue,
            TodaysPendingSettlementCount = r.TodaysPendingSettlementCount,
            TodaysPendingSettlementValue = r.TodaysPendingSettlementValue
        });
    }

    public static async Task<IResult> LastSettlement([FromHeader] Guid estateId,
                                                     IMediator mediator,
                                                     CancellationToken cancellationToken)
    {
        var query = new SettlementQueries.GetLastSettlementQuery(estateId);
        var result = await mediator.Send(query, cancellationToken);

        return ResponseFactory.FromResult(result, r => new LastSettlement
        {
            SalesCount = r.SalesCount,
            FeesValue = r.FeesValue,
            SalesValue = r.SalesValue,
            SettlementDate = r.SettlementDate
        });
    }

    public static async Task<IResult> GetUnsettledFees([FromHeader] Guid estateId,
                                                       [FromQuery] DateTime startDate,
                                                       [FromQuery] DateTime endDate,
                                                       [FromQuery] string? merchantIds,
                                                       [FromQuery] string? operatorIds,
                                                       [FromQuery] string? productIds,
                                                       [FromQuery] GroupByOption? groupByOption,
                                                       IMediator mediator,
                                                       CancellationToken cancellationToken)
    {
        var merchantIdFilter = ParseIds(merchantIds);
        var operatorIdFilter = ParseIds(operatorIds);
        var productIdFilter = ParseIds(productIds);

        var groupByOptionConverted = ConvertGroupByOption(groupByOption.GetValueOrDefault());

        var query = new SettlementQueries.GetUnsettledFeesQuery(
            estateId,
            startDate,
            endDate,
            merchantIdFilter,
            operatorIdFilter,
            productIdFilter,
            groupByOptionConverted);

        var result = await mediator.Send(query, cancellationToken);

        return ResponseFactory.FromResult(result, r =>
        {
            var response = r.Select(u => new DataTransferObjects.UnsettledFee
            {
                DimensionName = u.DimensionName,
                FeesCount = u.FeesCount,
                FeesValue = u.FeesValue
            }).ToList();

            return response;
        });
    }

    // Helper methods
    private static List<int> ParseIds(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
            return [];

        return csv.Split(',')
            .Select(s => int.TryParse(s, out var id) ? id : (int?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList();
    }

    private static Models.GroupByOption ConvertGroupByOption(GroupByOption groupByOption)
        => groupByOption switch
        {
            GroupByOption.Merchant => Models.GroupByOption.Merchant,
            GroupByOption.Product => Models.GroupByOption.Product,
            _ => Models.GroupByOption.Operator
        };
}