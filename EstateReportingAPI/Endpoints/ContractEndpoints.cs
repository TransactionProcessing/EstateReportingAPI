using Microsoft.Extensions.Configuration;
using EstateReportingAPI.DataTransferObjects;
using EstateReportingAPI.Handlers;
using Shared.Extensions;

namespace EstateReportingAPI.Endpoints;

public static class ContractEndpoints
{
    private const string BaseRoute = "api/contracts";

    public static void MapContractEndpoints(this IEndpointRouteBuilder app, IConfiguration configuration)
    {
        RouteGroupBuilder group = app.MapGroup(BaseRoute)
            .WithTags("Contracts");

        Boolean disableAuthorisation = configuration.GetValue("AppSettings:DisableAuthorisation", false);
        if (disableAuthorisation == false) {
            group = group.RequireAuthorization();
        }

        group.MapGet("/recent", ContractHandler.GetRecentContracts)
            .WithSummary("List recently created contracts")
            .WithDescription("Returns the most recently created contracts for the current estate.")
            .WithApiResponse<List<Contract>>();
        group.MapGet("/", ContractHandler.GetContracts)
            .WithSummary("List contracts")
            .WithDescription("Returns contracts and their configured products and transaction fees for the current estate.")
            .WithApiResponse<List<Contract>>();
        group.MapGet("/{contractId}", ContractHandler.GetContract)
            .WithSummary("Get a contract")
            .WithDescription("Returns the specified contract, including its products and transaction fees.")
            .WithApiResponse<Contract>();
    }
}
