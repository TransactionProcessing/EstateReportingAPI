using Microsoft.Extensions.Configuration;
using EstateReportingAPI.DataTransferObjects;
using EstateReportingAPI.Handlers;
using Shared.Extensions;

namespace EstateReportingAPI.Endpoints;

public static class EstateEndpoints
{
    private const string BaseRoute = "api/estates";

    public static void MapEstateEndpoints(this IEndpointRouteBuilder app, IConfiguration configuration)
    {
        RouteGroupBuilder group = app.MapGroup(BaseRoute)
            .WithTags("Estates");

        Boolean disableAuthorisation = configuration.GetValue("AppSettings:DisableAuthorisation", false);
        if (disableAuthorisation == false) {
            group = group.RequireAuthorization();
        }

        group.MapGet("/", EstateHandler.GetEstate)
            .WithSummary("Get the current estate")
            .WithDescription("Returns estate details and its associated operators, merchants, contracts, and users.")
            .WithApiResponse<Estate>();
        group.MapGet("/operators", EstateHandler.GetOperators)
            .WithSummary("List estate operators")
            .WithDescription("Returns operators associated with the current estate.")
            .WithApiResponse<List<EstateOperator>>();
    }
}
