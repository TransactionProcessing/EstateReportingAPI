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
        List<Operator> result = await this.Manager.GetOperators(request, cancellationToken);
        return Result.Success(result);
    }

    public async Task<Result<Operator>> Handle(OperatorQueries.GetOperatorQuery request,
                                               CancellationToken cancellationToken) {
        Operator result = await this.Manager.GetOperator(request, cancellationToken);
        return Result.Success(result);
    }
}