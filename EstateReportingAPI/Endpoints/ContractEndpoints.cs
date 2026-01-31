using EstateReportingAPI.DataTransferObjects;
using EstateReportingAPI.Handlers;
using Shared.Extensions;
using Shared.General;

namespace EstateReportingAPI.Endpoints;

public static class ContractEndpoints
{
    private const string BaseRoute = "api/contracts";

    public static void MapContractEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup(BaseRoute)
            .WithTags("Contracts");

        Boolean disableAuthorisation = ConfigurationReader.GetValueOrDefault<Boolean>("AppSettings", "DisableAuthorisation", false);
        if (disableAuthorisation == false) {
            group = group.RequireAuthorization();
        }

        group.MapGet("/recent", ContractHandler.GetRecentContracts)
            .WithStandardProduces<List<Contract>>();
        group.MapGet("/", ContractHandler.GetContracts)
            .WithStandardProduces<List<Contract>>();
        group.MapGet("/{contractId}", ContractHandler.GetContract)
            .WithStandardProduces<List<Contract>>();
    }
}