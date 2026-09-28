using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.Models;
using SimpleResults;

namespace EstateReportingAPI.BusinessLogic;

public interface ITransactionReportingService
{
    Task<Result<TodaysSales>> GetTodaysFailedSales(TransactionQueries.TodaysFailedSales request, CancellationToken cancellationToken);
    Task<Result<TodaysSales>> GetTodaysSales(TransactionQueries.TodaysSalesQuery request, CancellationToken cancellationToken);
    Task<Result<TransactionDetailReportResponse>> GetTransactionDetailReport(TransactionQueries.TransactionDetailReportQuery request, CancellationToken cancellationToken);
    Task<Result<TransactionSummaryByMerchantResponse>> GetTransactionSummaryByMerchantReport(TransactionQueries.TransactionSummaryByMerchantQuery request, CancellationToken cancellationToken);
    Task<Result<TransactionSummaryByOperatorResponse>> GetTransactionSummaryByOperatorReport(TransactionQueries.TransactionSummaryByOperatorQuery request, CancellationToken cancellationToken);
    Task<Result<ProductPerformanceResponse>> GetProductPerformanceReport(TransactionQueries.ProductPerformanceQuery request, CancellationToken cancellationToken);
    Task<Result<TransactionMixSummaryResponse>> GetTransactionMixSummary(TransactionQueries.TransactionMixSummaryQuery request, CancellationToken cancellationToken);
    Task<Result<GetRecentActivityReceiptReportResponse>> GetRecentActivityReceiptReport(TransactionQueries.GetRecentActivityReceiptReportQuery request, CancellationToken cancellationToken);
    Task<Result<List<TodaysSalesByHour>>> GetTodaysSalesByHour(TransactionQueries.TodaysSalesByHour request, CancellationToken cancellationToken);
    Task<Result<MerchantDailyPerformanceSummaryResponse>> GetMerchantDailyPerformanceSummary(TransactionQueries.MerchantDailyPerformanceSummaryQuery request, CancellationToken cancellationToken);
}
