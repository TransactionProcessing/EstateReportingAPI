using Microsoft.Extensions.Configuration;
using EstateReportingAPI.DataTransferObjects;
using EstateReportingAPI.Handlers;
using Shared.Extensions;
using Shared.Middleware;

namespace EstateReportingAPI.Endpoints;

public static class TransactionEndpoints
{
    private const string BaseRoute = "api/transactions";
    public static void MapTransactionEndpoints(this IEndpointRouteBuilder app, IConfiguration configuration)
    {
        RouteGroupBuilder group = app.MapGroup(BaseRoute)
            .WithTags("Transactions");

        Boolean disableAuthorisation = configuration.GetValue("AppSettings:DisableAuthorisation", false);
        if (disableAuthorisation == false) {
            group = group.RequireAuthorization();
        }

        group.MapGet("todayssales", TransactionHandler.TodaysSales)
            .WithSummary("Get today's sales totals")
            .WithDescription("Returns today's sales count, value, and average alongside totals for the comparison date.")
            .WithApiResponse<TodaysSales>();
        group.MapGet("todaysfailedsales", TransactionHandler.TodaysFailedSales)
            .WithSummary("Get today's failed sales totals")
            .WithDescription("Returns today's failed sales count, value, and average alongside comparison totals.")
            .WithApiResponse<TodaysSales>();
        group.MapPost("transactiondetailreport", TransactionHandler.TransactionDetailReport)
            .WithSummary("Get a transaction detail report")
            .WithDescription("Returns paginated transaction details and aggregate totals for the requested date range and optional operator, merchant, and product filters.")
            .WithStandardProduces<TransactionDetailReportResponse, ErrorResponse>();
        group.MapPost("transactionsummarybymerchantreport", TransactionHandler.TransactionSummaryByMerchantReport)
            .WithSummary("Get a transaction summary by merchant")
            .WithDescription("Returns per-merchant transaction totals and an overall summary for the requested date range and optional filters.")
            .WithApiResponse<TransactionSummaryByMerchantResponse>();
        group.MapPost("transactionsummarybyoperatorreport", TransactionHandler.TransactionSummaryByOperatorReport)
            .WithSummary("Get a transaction summary by operator")
            .WithDescription("Returns per-operator transaction totals and an overall summary for the requested date range and optional filters.")
            .WithApiResponse<TransactionSummaryByOperatorResponse>();
        group.MapGet("productperformancereport", TransactionHandler.ProductPerformanceReport)
            .WithSummary("Get product performance")
            .WithDescription("Returns product transaction performance and aggregate totals for the requested date range.")
            .WithApiResponse<ProductPerformanceResponse>();
        group.MapPost("transactionmixsummary", TransactionHandler.TransactionMixSummary)
            .WithSummary("Get a transaction mix summary")
            .WithDescription("Groups transactions by the requested dimension and measure, with optional merchant filtering, top-N selection, and pagination.")
            .WithStandardProduces<TransactionMixSummaryResponse, ErrorResponse>();
        group.MapPost("recentactivityreceiptreport", TransactionHandler.RecentActivityReceiptReport)
            .WithSummary("Get recent transaction activity and receipt references")
            .WithDescription("Returns paginated transactions for a report date, optionally filtered by merchant and search text.")
            .WithApiResponse<GetRecentActivityReceiptReportResponse>();
        group.MapGet("todayssalesbyhour", TransactionHandler.TodaysSalesByHour)
            .WithSummary("Get today's sales by hour")
            .WithDescription("Returns hourly sales counts and values for today alongside the supplied comparison date.")
            .WithApiResponse<List<TodaysSalesByHour>>();
        group.MapPost("merchantdailyperformancesummary", TransactionHandler.MerchantDailyPerformanceSummary)
            .WithSummary("Get a merchant daily performance summary")
            .WithDescription("Returns performance metrics and drill-down transactions for the requested merchant and date range.")
            .WithApiResponse<MerchantDailyPerformanceSummaryResponse>();

    }
}

