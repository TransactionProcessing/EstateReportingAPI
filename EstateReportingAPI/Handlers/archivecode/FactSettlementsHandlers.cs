using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.DataTransferObjects;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Results.Web;

namespace EstateReportingAPI.Handlers;
/*
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
            var req = new GetUnsettledFeesRequest(){
                StartDate = ParseDate(q, "startDate", nameof(StartDate)),
                EndDate = ParseDate(q, "endDate", nameof(EndDate)),
                MerchantIds = GetStringValue(q, "merchantIds", nameof(MerchantIds)),
                OperatorIds = GetStringValue(q, "operatorIds", nameof(OperatorIds)),
                ProductIds = GetStringValue(q, "productIds", nameof(ProductIds)),
                GroupByOption = ParseEnum<GroupByOption>(q, "groupByOption", nameof(GroupByOption))
            };

            return ValueTask.FromResult<GetUnsettledFeesRequest?>(req);
        }

        // Extracted helpers to reduce cyclomatic complexity in BindAsync
        private static DateTime ParseDate(IQueryCollection q, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (q.TryGetValue(key, out var values) && DateTime.TryParse(values.ToString(), out var dt))
                {
                    return dt;
                }
            }

            return default;
        }

        private static string? GetStringValue(IQueryCollection q, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (q.TryGetValue(key, out var values))
                {
                    var s = values.ToString();
                    if (!string.IsNullOrWhiteSpace(s))
                        return s;
                }
            }

            return null;
        }

        private static TEnum? ParseEnum<TEnum>(IQueryCollection q, params string[] keys) where TEnum : struct, Enum
        {
            foreach (var key in keys)
            {
                if (q.TryGetValue(key, out var values) && Enum.TryParse<TEnum>(values.ToString(), true, out var parsed))
                {
                    return parsed;
                }
            }

            return null;
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
}*/