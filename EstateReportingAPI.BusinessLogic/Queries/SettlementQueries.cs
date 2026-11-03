using System.Diagnostics.CodeAnalysis;
using EstateReportingAPI.Models;
using MediatR;
using SimpleResults;

namespace EstateReportingAPI.BusinessLogic.Queries;

[ExcludeFromCodeCoverage]
public record SettlementQueries {
    public record TodaysSettlementQuery(Guid EstateId, DateOnly ComparisonDate) : IRequest<Result<TodaysSettlement>>;
}