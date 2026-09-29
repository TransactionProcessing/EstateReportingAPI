using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.BusinessLogic.Services;
using EstateReportingAPI.Models;
using MediatR;
using SimpleResults;

namespace EstateReportingAPI.BusinessLogic.RequestHandlers;

public class OperatorRequestHandler : IRequestHandler<OperatorQueries.GetOperatorsQuery, Result<List<Operator>>>,
    IRequestHandler<OperatorQueries.GetOperatorQuery, Result<Operator>>
{

    private readonly IMerchantReportingService ReportingService;

    public OperatorRequestHandler(IMerchantReportingService reportingService) {
        this.ReportingService = reportingService;
    }

    public async Task<Result<List<Operator>>> Handle(OperatorQueries.GetOperatorsQuery request,
                                                     CancellationToken cancellationToken) {
        return await this.ReportingService.GetOperators(request, cancellationToken);
    }

    public async Task<Result<Operator>> Handle(OperatorQueries.GetOperatorQuery request,
                                               CancellationToken cancellationToken) {
        return await this.ReportingService.GetOperator(request, cancellationToken);
    }
}
