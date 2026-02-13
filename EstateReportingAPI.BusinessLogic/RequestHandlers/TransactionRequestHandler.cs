using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.Models;
using MediatR;
using SimpleResults;

namespace EstateReportingAPI.BusinessLogic.RequestHandlers;

public class TransactionRequestHandler : IRequestHandler<TransactionQueries.TodaysFailedSales, Result<TodaysSales>>,
    IRequestHandler<TransactionQueries.TodaysSalesQuery, Result<TodaysSales>>,
IRequestHandler<TransactionQueries.TransactionDetailReportQuery, Result<TransactionDetailReportResponse>>,
    IRequestHandler<TransactionQueries.TransactionSummaryByMerchantQuery, Result<TransactionSummaryByMerchantResponse>>
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
}