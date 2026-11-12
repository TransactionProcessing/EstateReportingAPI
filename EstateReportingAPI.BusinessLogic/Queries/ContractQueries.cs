using EstateReportingAPI.Models;
using MediatR;
using TransactionProcessing.UnionResults;
using System.Diagnostics.CodeAnalysis;

namespace EstateReportingAPI.BusinessLogic.Queries;

[ExcludeFromCodeCoverage]
public record ContractQueries {
    public record GetRecentContractsQuery(Guid EstateId) : IRequest<Result<List<Contract>>>;
    public record GetContractsQuery(Guid EstateId) : IRequest<Result<List<Contract>>>;
    public record GetContractQuery(Guid EstateId, Guid ContractId) : IRequest<Result<Contract>>;
}