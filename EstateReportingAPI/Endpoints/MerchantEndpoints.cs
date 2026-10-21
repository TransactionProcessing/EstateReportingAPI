using Microsoft.Extensions.Configuration;
using EstateReportingAPI.DataTransferObjects;
using EstateReportingAPI.DataTrasferObjects;
using EstateReportingAPI.Handlers;
using Shared.Extensions;

namespace EstateReportingAPI.Endpoints;

public static class MerchantEndpoints
{
    private const string BaseRoute = "api/merchants";

    public static void MapMerchantEndpoints(this IEndpointRouteBuilder app, IConfiguration configuration)
    {
        RouteGroupBuilder group = app.MapGroup(BaseRoute)
            .WithTags("Merchants");

        Boolean disableAuthorisation = configuration.GetValue("AppSettings:DisableAuthorisation", false);
        if (disableAuthorisation == false) {
            group = group.RequireAuthorization();
        }

        group.MapGet("/kpis", MerchantHandler.GetMerchantKpis)
            .WithSummary("Get merchant performance indicators")
            .WithDescription("Returns merchant activity counts for the current estate, including recent and absent sales.")
            .WithApiResponse<MerchantKpi>();
        group.MapGet("/recent", MerchantHandler.GetRecentMerchants)
            .WithSummary("List recently created merchants")
            .WithDescription("Returns the most recently created merchants for the current estate.")
            .WithApiResponse<List<Merchant>>();
        group.MapGet("/", MerchantHandler.GetMerchants)
            .WithSummary("List merchants")
            .WithDescription("Returns merchants belonging to the current estate.")
            .WithApiResponse<List<Merchant>>();
        group.MapGet("/{merchantId}", MerchantHandler.GetMerchant)
            .WithSummary("Get a merchant")
            .WithDescription("Returns details for the specified merchant.")
            .WithApiResponse<Merchant>();
        group.MapGet("/{merchantId}/operators", MerchantHandler.GetMerchantOperators)
            .WithSummary("List a merchant's operators")
            .WithDescription("Returns the operators configured for the specified merchant.")
            .WithApiResponse<List<MerchantOperator>>();
        group.MapGet("/{merchantId}/contracts", MerchantHandler.GetMerchantContracts)
            .WithSummary("List a merchant's contracts")
            .WithDescription("Returns the contracts and products configured for the specified merchant.")
            .WithApiResponse<List<MerchantContract>>();
        group.MapGet("/{merchantId}/devices", MerchantHandler.GetMerchantDevices)
            .WithSummary("List a merchant's devices")
            .WithDescription("Returns the devices configured for the specified merchant.")
            .WithApiResponse<List<MerchantDevice>>();
        group.MapGet("/{merchantId}/openinghours", MerchantHandler.GetMerchantOpeningHours)
            .WithSummary("Get a merchant's opening hours")
            .WithDescription("Returns the opening and closing times configured for each day for the specified merchant.")
            .WithApiResponse<List<MerchantOpeningHour>>();
        group.MapGet("/{merchantId}/schedule/{year}", MerchantHandler.GetMerchantSchedule)
            .WithSummary("Get a merchant's schedule")
            .WithDescription("Returns the closed days for each month of the specified year for the merchant.")
            .WithApiResponse<MerchantScheduleResponse>();
    }
}
