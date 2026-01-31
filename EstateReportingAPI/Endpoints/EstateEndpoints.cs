using EstateReportingAPI.DataTransferObjects;
using EstateReportingAPI.Handlers;
using Shared.Extensions;
using Shared.General;

namespace EstateReportingAPI.Endpoints;

public static class EstateEndpoints
{
    private const string BaseRoute = "api/estates";

    public static void MapEstateEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup(BaseRoute)
            .WithTags("Estates");

        Boolean disableAuthorisation = ConfigurationReader.GetValueOrDefault<Boolean>("AppSettings", "DisableAuthorisation", false);
        if (disableAuthorisation == false) {
            group = group.RequireAuthorization();
        }

        group.MapGet("/", EstateHandler.GetEstate)
            .WithStandardProduces<Estate>();
        group.MapGet("/operators", EstateHandler.GetOperators)
            .WithStandardProduces<List<EstateOperator>>();
    }
}