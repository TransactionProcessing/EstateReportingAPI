using EstateReportingAPI.DataTransferObjects;
using EstateReportingAPI.DataTrasferObjects;
using EstateReportingAPI.Handlers;
using Shared.Extensions;

namespace EstateReportingAPI.Endpoints;

public static class FactTransactionsEndpoints
{
    private const string BaseRoute = "api/facts/transactions";

    public static void MapFactTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup(BaseRoute)
            .RequireAuthorization()
            .WithTags("Fact Transactions");

        group.MapGet("todayssales", FactTransactionsHandler.TodaysSales)
            .WithStandardProduces<TodaysSales>();
        group.MapGet("todayssales/countbyhour", FactTransactionsHandler.TodaysSalesCountByHour)
            .WithStandardProduces<List<TodaysSalesCountByHour>>();
        group.MapGet("todayssales/valuebyhour", FactTransactionsHandler.TodaysSalesValueByHour)
            .WithStandardProduces<List<TodaysSalesValueByHour>>();
        group.MapGet("merchants/lastsale", FactTransactionsHandler.GetMerchantsByLastSale)
            .WithStandardProduces<List<Merchant>>();
        group.MapGet("merchantkpis", FactTransactionsHandler.GetMerchantsTransactionKpis)
            .WithStandardProduces<MerchantKpi>();
        group.MapGet("todaysfailedsales", FactTransactionsHandler.TodaysFailedSales)
            .WithStandardProduces<TodaysSales>();
        group.MapGet("products/topbottombyvalue", FactTransactionsHandler.GetTopBottomProductsByValue)
            .WithStandardProduces<List<TopBottomProductData>>();
        group.MapGet("merchants/topbottombyvalue", FactTransactionsHandler.GetTopBottomMerchantsByValue)
            .WithStandardProduces<List<TopBottomMerchantData>>();
        group.MapGet("operators/topbottombyvalue", FactTransactionsHandler.GetTopBottomOperatorsByValue)
            .WithStandardProduces<List<TopBottomOperatorData>>();
        group.MapGet("merchants/performance", FactTransactionsHandler.GetMerchantPerformance)
            .WithStandardProduces<TodaysSales>();
        group.MapGet("products/performance", FactTransactionsHandler.GetProductPerformance)
            .WithStandardProduces<TodaysSales>();
        group.MapGet("operators/performance", FactTransactionsHandler.GetOperatorPerformance)
            .WithStandardProduces<TodaysSales>();
        group.MapPost("search", FactTransactionsHandler.TransactionSearch)
            .WithStandardProduces<List<TransactionResult>>();
    }
}