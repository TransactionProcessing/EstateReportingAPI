using EstateReportingAPI.DataTransferObjects;
using EstateReportingAPI.Handlers;
using Shared.Extensions;
using Shared.General;

namespace EstateReportingAPI.Endpoints;

public static class OperatorEndpoints
{
    private const string BaseRoute = "api/operators";

    public static void MapOperatorEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup(BaseRoute)
            .WithTags("Operators");

        Boolean disableAuthorisation = ConfigurationReader.GetValueOrDefault<Boolean>("AppSettings", "DisableAuthorisation", false);
        if (disableAuthorisation == false) {
            group = group.RequireAuthorization();
        }

        group.MapGet("/", OperatorHandler.GetOperators)
            .WithStandardProduces<List<Operator>>();

        group.MapGet("/{operatorId}", OperatorHandler.GetOperator)
            .WithStandardProduces<Operator>();
    }
}