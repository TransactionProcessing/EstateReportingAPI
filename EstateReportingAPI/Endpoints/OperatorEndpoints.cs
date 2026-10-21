using Microsoft.Extensions.Configuration;
using EstateReportingAPI.DataTransferObjects;
using EstateReportingAPI.Handlers;
using Shared.Extensions;

namespace EstateReportingAPI.Endpoints;

public static class OperatorEndpoints
{
    private const string BaseRoute = "api/operators";

    public static void MapOperatorEndpoints(this IEndpointRouteBuilder app, IConfiguration configuration)
    {
        RouteGroupBuilder group = app.MapGroup(BaseRoute)
            .WithTags("Operators");

        Boolean disableAuthorisation = configuration.GetValue("AppSettings:DisableAuthorisation", false);
        if (disableAuthorisation == false) {
            group = group.RequireAuthorization();
        }

        group.MapGet("/", OperatorHandler.GetOperators)
            .WithSummary("List operators")
            .WithDescription("Returns operators available to the current estate.")
            .WithApiResponse<List<Operator>>();

        group.MapGet("/{operatorId}", OperatorHandler.GetOperator)
            .WithSummary("Get an operator")
            .WithDescription("Returns details for the specified operator.")
            .WithApiResponse<Operator>();
    }
}
