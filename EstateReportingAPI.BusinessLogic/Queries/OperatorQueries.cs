using System.Diagnostics.CodeAnalysis;
using EstateReportingAPI.Models;
using MediatR;
using SimpleResults;

namespace EstateReportingAPI.BusinessLogic.Queries;

[ExcludeFromCodeCoverage]
public record OperatorQueries {
    public record GetOperatorsQuery(Guid EstateId) : IRequest<Result<List<Operator>>>;
    public record GetOperatorPerformanceQuery(Guid EstateId, DateTime comparisonDate, List<Int32> operatorReportingIds) : IRequest<Result<TodaysSales>>;
    public record GetTopOperatorsBySalesValueQuery(Guid EstateId, Int32 numberOfOperators) : IRequest<Result<List<TopBottomData>>>;
    public record GetBottomOperatorsBySalesValueQuery(Guid EstateId, Int32 numberOfOperators) : IRequest<Result<List<TopBottomData>>>;
}