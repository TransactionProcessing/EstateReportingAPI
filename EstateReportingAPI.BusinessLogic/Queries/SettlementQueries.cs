using System.Diagnostics.CodeAnalysis;
using EstateReportingAPI.Models;
using MediatR;
using SimpleResults;

namespace EstateReportingAPI.BusinessLogic.Queries;

[ExcludeFromCodeCoverage]
public record SettlementQueries {
    public record TodaysSettlementQuery(Guid EstateId, DateTime ComparisonDate) : IRequest<Result<TodaysSettlement>>;
}