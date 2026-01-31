using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.Models;
using MediatR;
using SimpleResults;

namespace EstateReportingAPI.BusinessLogic.RequestHandlers;

public class TransactionRequestHandler : IRequestHandler<TransactionQueries.TodaysFailedSales, Result<TodaysSales>>,
    IRequestHandler<TransactionQueries.TodaysSalesQuery, Result<TodaysSales>>{
    private readonly IReportingManager Manager;

    public TransactionRequestHandler(IReportingManager manager) {
        this.Manager = manager;
    }

    public async Task<Result<TodaysSales>> Handle(TransactionQueries.TodaysFailedSales request,
                                                  CancellationToken cancellationToken) {
        var result = await this.Manager.GetTodaysFailedSales(request, cancellationToken);
        return Result.Success(result);
    }

    public async Task<Result<TodaysSales>> Handle(TransactionQueries.TodaysSalesQuery request,
                                                  CancellationToken cancellationToken) {
        var result = await this.Manager.GetTodaysSales(request, cancellationToken);
        return Result.Success(result);
    }
}