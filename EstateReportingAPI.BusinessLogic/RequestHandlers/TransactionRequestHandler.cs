using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.Models;
using MediatR;
using SimpleResults;

namespace EstateReportingAPI.BusinessLogic.RequestHandlers;

public class SettlementRequestHandler : IRequestHandler<SettlementQueries.TodaysSettlementQuery, Result<TodaysSettlement>>
{
    private readonly IReportingManager Manager;
    public SettlementRequestHandler(IReportingManager manager) {
        this.Manager = manager;
    }
    public async Task<Result<TodaysSettlement>> Handle(SettlementQueries.TodaysSettlementQuery request,
                                                        CancellationToken cancellationToken) {
        return await this.Manager.GetTodaysSettlement(request, cancellationToken);
    }
}

public class TransactionRequestHandler : IRequestHandler<TransactionQueries.TodaysFailedSales, Result<TodaysSales>>,
    IRequestHandler<TransactionQueries.TodaysSalesQuery, Result<TodaysSales>>,
    IRequestHandler<TransactionQueries.TransactionDetailReportQuery, Result<TransactionDetailReportResponse>>,
    IRequestHandler<TransactionQueries.TransactionSummaryByMerchantQuery, Result<TransactionSummaryByMerchantResponse>>,
    IRequestHandler<TransactionQueries.TransactionSummaryByOperatorQuery, Result<TransactionSummaryByOperatorResponse>>,
    IRequestHandler<TransactionQueries.ProductPerformanceQuery, Result<ProductPerformanceResponse>>,
    IRequestHandler<TransactionQueries.TodaysSalesByHour, Result<List<TodaysSalesByHour>>>

{
    private readonly IReportingManager Manager;

    public TransactionRequestHandler(IReportingManager manager) {
        this.Manager = manager;
    }

    public async Task<Result<TodaysSales>> Handle(TransactionQueries.TodaysFailedSales request,
                                                  CancellationToken cancellationToken) {
        return await this.Manager.GetTodaysFailedSales(request, cancellationToken);
    }

    public async Task<Result<TodaysSales>> Handle(TransactionQueries.TodaysSalesQuery request,
                                                  CancellationToken cancellationToken) {
        return await this.Manager.GetTodaysSales(request, cancellationToken);
    }

    public async Task<Result<TransactionDetailReportResponse>> Handle(TransactionQueries.TransactionDetailReportQuery request,
                                                                      CancellationToken cancellationToken) {
        return await this.Manager.GetTransactionDetailReport(request, cancellationToken);
    }

    public async Task<Result<TransactionSummaryByMerchantResponse>> Handle(TransactionQueries.TransactionSummaryByMerchantQuery request,
                                                                      CancellationToken cancellationToken)
    {
        return await this.Manager.GetTransactionSummaryByMerchantReport(request, cancellationToken);
    }

    public async Task<Result<TransactionSummaryByOperatorResponse>> Handle(TransactionQueries.TransactionSummaryByOperatorQuery request,
                                                                      CancellationToken cancellationToken)
    {
        return await this.Manager.GetTransactionSummaryByOperatorReport(request, cancellationToken);
    }

    public async Task<Result<ProductPerformanceResponse>> Handle(TransactionQueries.ProductPerformanceQuery request,
                                                                 CancellationToken cancellationToken) {
        return await this.Manager.GetProductPerformanceReport(request, cancellationToken);
    }

    public async Task<Result<List<TodaysSalesByHour>>> Handle(TransactionQueries.TodaysSalesByHour request,
                                                              CancellationToken cancellationToken) {
        return await this.Manager.GetTodaysSalesByHour(request, cancellationToken);
    }
}