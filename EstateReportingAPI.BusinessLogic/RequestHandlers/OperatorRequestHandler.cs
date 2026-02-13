using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.Models;
using MediatR;
using SimpleResults;

namespace EstateReportingAPI.BusinessLogic.RequestHandlers;

public class OperatorRequestHandler : IRequestHandler<OperatorQueries.GetOperatorsQuery, Result<List<Operator>>>,
    IRequestHandler<OperatorQueries.GetOperatorQuery, Result<Operator>>
{

    private readonly IReportingManager Manager;

    public OperatorRequestHandler(IReportingManager manager) {
        this.Manager = manager;
    }

    public async Task<Result<List<Operator>>> Handle(OperatorQueries.GetOperatorsQuery request,
                                                     CancellationToken cancellationToken) {
        return await this.Manager.GetOperators(request, cancellationToken);
    }

    public async Task<Result<Operator>> Handle(OperatorQueries.GetOperatorQuery request,
                                               CancellationToken cancellationToken) {
        return await this.Manager.GetOperator(request, cancellationToken);
    }
}