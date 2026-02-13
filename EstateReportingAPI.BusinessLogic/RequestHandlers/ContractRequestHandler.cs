using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.Models;
using MediatR;
using SimpleResults;

namespace EstateReportingAPI.BusinessLogic.RequestHandlers;

public class ContractRequestHandler : IRequestHandler<ContractQueries.GetRecentContractsQuery, Result<List<Contract>>>,
    IRequestHandler<ContractQueries.GetContractsQuery, Result<List<Contract>>>,
    IRequestHandler<ContractQueries.GetContractQuery, Result<Contract>>
{

    private readonly IReportingManager Manager;

    public ContractRequestHandler(IReportingManager manager)
    {
        this.Manager = manager;
    }
    public async Task<Result<List<Contract>>> Handle(ContractQueries.GetRecentContractsQuery request,
                                                     CancellationToken cancellationToken) {
        return await this.Manager.GetRecentContracts(request, cancellationToken);
    }

    public async Task<Result<List<Contract>>> Handle(ContractQueries.GetContractsQuery request,
                                                     CancellationToken cancellationToken)
    {
        var result = await this.Manager.GetContracts(request, cancellationToken);
        return result;
    }

    public async Task<Result<Contract>> Handle(ContractQueries.GetContractQuery request,
                                               CancellationToken cancellationToken)
    {
        return await this.Manager.GetContract(request, cancellationToken);
    }
}