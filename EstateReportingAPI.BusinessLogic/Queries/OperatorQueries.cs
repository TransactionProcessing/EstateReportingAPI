using System.Diagnostics.CodeAnalysis;
using EstateReportingAPI.Models;
using MediatR;
using SimpleResults;

namespace EstateReportingAPI.BusinessLogic.Queries;

[ExcludeFromCodeCoverage]
public record OperatorQueries {
    public record GetOperatorsQuery(Guid EstateId) : IRequest<Result<List<Operator>>>;
    public record GetOperatorQuery(Guid EstateId, Guid OperatorId) : IRequest<Result<Operator>>;
}