using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.Models;
using MediatR;
using SimpleResults;

namespace EstateReportingAPI.BusinessLogic.RequestHandlers;

public class EstateRequestHandler : IRequestHandler<EstateQueries.GetEstateQuery, Result<Estate>>,
    IRequestHandler<EstateQueries.GetEstateOperatorsQuery, Result<List<EstateOperator>>>
{

    private readonly IReportingManager Manager;

    public EstateRequestHandler(IReportingManager manager)
    {
        this.Manager = manager;
    }
    public async Task<Result<Estate>> Handle(EstateQueries.GetEstateQuery request,
                                             CancellationToken cancellationToken)
    {
        var result = await this.Manager.GetEstate(request, cancellationToken);
        return Result.Success(result);
    }

    public async Task<Result<List<EstateOperator>>> Handle(EstateQueries.GetEstateOperatorsQuery request,
                                                           CancellationToken cancellationToken) {
        var result = await this.Manager.GetEstateOperators(request, cancellationToken);
        return Result.Success(result);
    }
}