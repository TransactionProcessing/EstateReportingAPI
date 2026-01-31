using System.Diagnostics.CodeAnalysis;
using EstateReportingAPI.Models;
using MediatR;
using SimpleResults;

namespace EstateReportingAPI.BusinessLogic.Queries;

[ExcludeFromCodeCoverage]
public record EstateQueries {
    public record GetEstateQuery(Guid EstateId) : IRequest<Result<Estate>>;
    public record GetEstateOperatorsQuery(Guid EstateId) : IRequest<Result<List<EstateOperator>>>;
}
