using EstateReportingAPI.DataTransferObjects;
using EstateReportingAPI.Handlers;
using Shared.Extensions;
using Shared.General;

namespace EstateReportingAPI.Endpoints;

public static class SettlementEndpoints {
    private const string BaseRoute = "api/settlements";

    public static void MapSettlementEndpoints(this IEndpointRouteBuilder app) {
        RouteGroupBuilder group = app.MapGroup(BaseRoute).WithTags("Settlements");

        Boolean disableAuthorisation = ConfigurationReader.GetValueOrDefault<Boolean>("AppSettings", "DisableAuthorisation", false);
        if (disableAuthorisation == false) {
            group = group.RequireAuthorization();
        }

        group.MapGet("todayssettlements", SettlementHandler.TodaysSettlements).WithStandardProduces<TodaysSettlement>();
    }
}