using EstateReportingAPI.Models;
using MediatR;
using SimpleResults;
using System.Diagnostics.CodeAnalysis;

namespace EstateReportingAPI.BusinessLogic.Queries;

[ExcludeFromCodeCoverage]
public record ResponseCodeQueries {
    public record GetResponseCodesQuery(Guid EstateId) : IRequest<Result<List<ResponseCode>>>;
}