using Microsoft.Extensions.Configuration;
using EstateReportingAPI.DataTransferObjects;
using EstateReportingAPI.Handlers;
using Shared.Extensions;

namespace EstateReportingAPI.Endpoints;

public static class SettlementEndpoints {
    private const string BaseRoute = "api/settlements";

    public static void MapSettlementEndpoints(this IEndpointRouteBuilder app, IConfiguration configuration) {
        RouteGroupBuilder group = app.MapGroup(BaseRoute).WithTags("Settlements");

        Boolean disableAuthorisation = configuration.GetValue("AppSettings:DisableAuthorisation", false);
        if (disableAuthorisation == false) {
            group = group.RequireAuthorization();
        }

        group.MapGet("todayssettlements", SettlementHandler.TodaysSettlements)
            .WithSummary("Get today's settlements")
            .WithDescription("Returns settled and pending settlement totals for today and the configured comparison date.")
            .WithApiResponse<TodaysSettlement>();
    }
}
