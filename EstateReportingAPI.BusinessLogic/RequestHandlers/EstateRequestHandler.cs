using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.Models;
using MediatR;
using SimpleResults;

namespace EstateReportingAPI.BusinessLogic.RequestHandlers;

public class EstateRequestHandler : IRequestHandler<EstateQueries.GetEstateQuery, Result<Estate>>,
    IRequestHandler<EstateQueries.GetEstateOperatorsQuery, Result<List<EstateOperator>>>
{

    private readonly IEstateReportingService Service;

    public EstateRequestHandler(IEstateReportingService service)
    {
        this.Service = service;
    }
    public async Task<Result<Estate>> Handle(EstateQueries.GetEstateQuery request,
                                             CancellationToken cancellationToken)
    {
        return await this.Service.GetEstate(request, cancellationToken);
    }

    public async Task<Result<List<EstateOperator>>> Handle(EstateQueries.GetEstateOperatorsQuery request,
                                                           CancellationToken cancellationToken) {
        return await this.Service.GetEstateOperators(request, cancellationToken);
    }
}
