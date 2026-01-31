using EstateReportingAPI.DataTransferObjects;
using EstateReportingAPI.DataTrasferObjects;
using EstateReportingAPI.Handlers;
using Shared.Extensions;
using Shared.General;

namespace EstateReportingAPI.Endpoints;

public static class MerchantEndpoints
{
    private const string BaseRoute = "api/merchants";

    public static void MapMerchantEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup(BaseRoute)
            .WithTags("Merchants");

        Boolean disableAuthorisation = ConfigurationReader.GetValueOrDefault<Boolean>("AppSettings", "DisableAuthorisation", false);
        if (disableAuthorisation == false) {
            group = group.RequireAuthorization();
        }

        group.MapGet("/kpis", MerchantHandler.GetMerchantKpis)
            .WithStandardProduces<List<ComparisonDate>>();
        group.MapGet("/recent", MerchantHandler.GetRecentMerchants)
            .WithStandardProduces<List<Merchant>>();
        group.MapGet("/", MerchantHandler.GetMerchants)
            .WithStandardProduces<List<Merchant>>();
        group.MapGet("/{merchantId}", MerchantHandler.GetMerchant)
            .WithStandardProduces<Merchant>();
        group.MapGet("/{merchantId}/operators", MerchantHandler.GetMerchantOperators)
            .WithStandardProduces<Merchant>();
        group.MapGet("/{merchantId}/contracts", MerchantHandler.GetMerchantContracts)
            .WithStandardProduces<Merchant>();
        group.MapGet("/{merchantId}/devices", MerchantHandler.GetMerchantDevices)
            .WithStandardProduces<Merchant>();
    }
}