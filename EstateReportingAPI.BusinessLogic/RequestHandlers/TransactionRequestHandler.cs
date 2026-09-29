using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.BusinessLogic.Services;
using EstateReportingAPI.Models;
using MediatR;
using SimpleResults;

namespace EstateReportingAPI.BusinessLogic.RequestHandlers;

public class TransactionRequestHandler : IRequestHandler<TransactionQueries.TodaysFailedSales, Result<TodaysSales>>,
    IRequestHandler<TransactionQueries.TodaysSalesQuery, Result<TodaysSales>>,
    IRequestHandler<TransactionQueries.TransactionDetailReportQuery, Result<TransactionDetailReportResponse>>,
    IRequestHandler<TransactionQueries.TransactionSummaryByMerchantQuery, Result<TransactionSummaryByMerchantResponse>>,
    IRequestHandler<TransactionQueries.TransactionSummaryByOperatorQuery, Result<TransactionSummaryByOperatorResponse>>,
    IRequestHandler<TransactionQueries.ProductPerformanceQuery, Result<ProductPerformanceResponse>>,
    IRequestHandler<TransactionQueries.TransactionMixSummaryQuery, Result<TransactionMixSummaryResponse>>,
    IRequestHandler<TransactionQueries.GetRecentActivityReceiptReportQuery, Result<GetRecentActivityReceiptReportResponse>>,
    IRequestHandler<TransactionQueries.TodaysSalesByHour, Result<List<TodaysSalesByHour>>>,
    IRequestHandler<TransactionQueries.MerchantDailyPerformanceSummaryQuery, Result<MerchantDailyPerformanceSummaryResponse>>

{
    private readonly ITransactionReportingService Service;

    public TransactionRequestHandler(ITransactionReportingService service) {
        this.Service = service;
    }

    public async Task<Result<TodaysSales>> Handle(TransactionQueries.TodaysFailedSales request,
                                                  CancellationToken cancellationToken) {
        return await this.Service.GetTodaysFailedSales(request, cancellationToken);
    }

    public async Task<Result<TodaysSales>> Handle(TransactionQueries.TodaysSalesQuery request,
                                                  CancellationToken cancellationToken) {
        return await this.Service.GetTodaysSales(request, cancellationToken);
    }

    public async Task<Result<TransactionDetailReportResponse>> Handle(TransactionQueries.TransactionDetailReportQuery request,
                                                                      CancellationToken cancellationToken) {
        return await this.Service.GetTransactionDetailReport(request, cancellationToken);
    }

    public async Task<Result<TransactionSummaryByMerchantResponse>> Handle(TransactionQueries.TransactionSummaryByMerchantQuery request,
                                                                      CancellationToken cancellationToken)
    {
        return await this.Service.GetTransactionSummaryByMerchantReport(request, cancellationToken);
    }

    public async Task<Result<TransactionSummaryByOperatorResponse>> Handle(TransactionQueries.TransactionSummaryByOperatorQuery request,
                                                                      CancellationToken cancellationToken)
    {
        return await this.Service.GetTransactionSummaryByOperatorReport(request, cancellationToken);
    }

    public async Task<Result<ProductPerformanceResponse>> Handle(TransactionQueries.ProductPerformanceQuery request,
                                                                 CancellationToken cancellationToken) {
        return await this.Service.GetProductPerformanceReport(request, cancellationToken);
    }

    public async Task<Result<TransactionMixSummaryResponse>> Handle(TransactionQueries.TransactionMixSummaryQuery request,
                                                                    CancellationToken cancellationToken)
    {
        return await this.Service.GetTransactionMixSummary(request, cancellationToken);
    }

    public async Task<Result<GetRecentActivityReceiptReportResponse>> Handle(TransactionQueries.GetRecentActivityReceiptReportQuery request,
                                                                             CancellationToken cancellationToken)
    {
        return await this.Service.GetRecentActivityReceiptReport(request, cancellationToken);
    }

    public async Task<Result<List<TodaysSalesByHour>>> Handle(TransactionQueries.TodaysSalesByHour request,
                                                              CancellationToken cancellationToken) {
        return await this.Service.GetTodaysSalesByHour(request, cancellationToken);
    }

    public async Task<Result<MerchantDailyPerformanceSummaryResponse>> Handle(TransactionQueries.MerchantDailyPerformanceSummaryQuery request,
                                                                              CancellationToken cancellationToken) {
        return await this.Service.GetMerchantDailyPerformanceSummary(request, cancellationToken);
    }
}
