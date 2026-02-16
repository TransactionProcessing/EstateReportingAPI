using EstateReportingAPI.DataTransferObjects;
using EstateReportingAPI.Handlers;
using Shared.Extensions;
using Shared.General;

namespace EstateReportingAPI.Endpoints;

public static class TransactionEndpoints
{
    private const string BaseRoute = "api/transactions";
    public static void MapTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup(BaseRoute)
            .WithTags("Transactions");

        Boolean disableAuthorisation = ConfigurationReader.GetValueOrDefault<Boolean>("AppSettings", "DisableAuthorisation", false);
        if (disableAuthorisation == false) {
            group = group.RequireAuthorization();
        }

        group.MapGet("todayssales", TransactionHandler.TodaysSales)
            .WithStandardProduces<TodaysSales>();
        group.MapGet("todaysfailedsales", TransactionHandler.TodaysFailedSales)
            .WithStandardProduces<TodaysSales>();
        group.MapPost("transactiondetailreport", TransactionHandler.TransactionDetailReport)
            .WithStandardProduces<TransactionDetailReportResponse>();
        group.MapPost("transactionsummarybymerchantreport", TransactionHandler.TransactionSummaryByMerchantReport)
            .WithStandardProduces<TransactionSummaryByMerchantResponse>();
        group.MapPost("transactionsummarybyoperatorreport", TransactionHandler.TransactionSummaryByOperatorReport)
            .WithStandardProduces<TransactionSummaryByOperatorResponse>();
        group.MapGet("productperformancereport", TransactionHandler.ProductPerformanceReport)
            .WithStandardProduces<ProductPerformanceResponse>();

    }
}