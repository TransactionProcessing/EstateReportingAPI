using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.DataTransferObjects;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Results.Web;

namespace EstateReportingAPI.Handlers;

public static class FactSettlementsHandlers
{

    public class GetUnsettledFeesRequest
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? MerchantIds { get; set; }
        public string? OperatorIds { get; set; }
        public string? ProductIds { get; set; }
        public GroupByOption? GroupByOption { get; set; }

        // Minimal API binder: called by RequestDelegateFactory when binding complex parameter
        public static ValueTask<GetUnsettledFeesRequest?> BindAsync(HttpContext context)
        {
            var q = context.Request.Query;
            var req = new GetUnsettledFeesRequest();

            if (q.TryGetValue("startDate", out var s) || q.TryGetValue(nameof(StartDate), out s))
            {
                DateTime.TryParse(s, out var d);
                req.StartDate = d;
            }

            if (q.TryGetValue("endDate", out var e) || q.TryGetValue(nameof(EndDate), out e))
            {
                DateTime.TryParse(e, out var d);
                req.EndDate = d;
            }

            if (q.TryGetValue("merchantIds", out var m) || q.TryGetValue(nameof(MerchantIds), out m))
            {
                req.MerchantIds = m.ToString();
            }

            if (q.TryGetValue("operatorIds", out var o) || q.TryGetValue(nameof(OperatorIds), out o))
            {
                req.OperatorIds = o.ToString();
            }

            if (q.TryGetValue("productIds", out var p) || q.TryGetValue(nameof(ProductIds), out p))
            {
                req.ProductIds = p.ToString();
            }

            if (q.TryGetValue("groupByOption", out var g) || q.TryGetValue(nameof(GroupByOption), out g))
            {
                if (Enum.TryParse<GroupByOption>(g.ToString(), true, out var gv))
                {
                    req.GroupByOption = gv;
                }
            }

            return ValueTask.FromResult<GetUnsettledFeesRequest?>(req);
        }
    }

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
                                                       GetUnsettledFeesRequest request,
                                                       IMediator mediator,
                                                       CancellationToken cancellationToken)
    {
        var merchantIdFilter = ParseIds(request.MerchantIds);
        var operatorIdFilter = ParseIds(request.OperatorIds);
        var productIdFilter = ParseIds(request.ProductIds);

        var groupByOptionConverted = ConvertGroupByOption(request.GroupByOption.GetValueOrDefault());

        var query = new SettlementQueries.GetUnsettledFeesQuery(
            estateId,
            request.StartDate,
            request.EndDate,
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