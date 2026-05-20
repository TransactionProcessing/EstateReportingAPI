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