using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.BusinessLogic.Services;
using EstateReportingAPI.Models;
using MediatR;
using SimpleResults;

namespace EstateReportingAPI.BusinessLogic.RequestHandlers;

public class SettlementRequestHandler : IRequestHandler<SettlementQueries.TodaysSettlementQuery, Result<TodaysSettlement>>
{
    private readonly ISettlementReportingService Service;
    public SettlementRequestHandler(ISettlementReportingService service) {
        this.Service = service;
    }
    public async Task<Result<TodaysSettlement>> Handle(SettlementQueries.TodaysSettlementQuery request,
                                                       CancellationToken cancellationToken) {
        return await this.Service.GetTodaysSettlement(request, cancellationToken);
    }
}
