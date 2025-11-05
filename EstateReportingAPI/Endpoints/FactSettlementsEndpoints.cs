using EstateReportingAPI.DataTransferObjects;
using EstateReportingAPI.Handlers;
using Shared.Extensions;

namespace EstateReportingAPI.Endpoints;

public static class FactSettlementsEndpoints
{
    private const string BaseRoute = "api/facts/settlements";

    public static void MapFactSettlementsEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup(BaseRoute)
            .RequireAuthorization()
            .WithTags("Fact Settlements");

        group.MapGet("todayssettlement", FactSettlementsHandlers.TodaysSettlement)
        .WithStandardProduces<TodaysSettlement>();
        group.MapGet("lastsettlement", FactSettlementsHandlers.LastSettlement)
            .WithStandardProduces<LastSettlement>();
        group.MapGet("unsettledfees", FactSettlementsHandlers.GetUnsettledFees)
            .WithStandardProduces<List<UnsettledFee>>();
    }
}