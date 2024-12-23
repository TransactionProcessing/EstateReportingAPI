using System.Diagnostics.CodeAnalysis;
using EstateReportingAPI.Models;
using MediatR;
using SimpleResults;

namespace EstateReportingAPI.BusinessLogic.Queries;

[ExcludeFromCodeCoverage]
public record ProductQueries
{
    public record GetProductPerformanceQuery(Guid EstateId, DateTime comparisonDate, List<Int32> productReportingIds) : IRequest<Result<TodaysSales>>;
    public record GetTopProductsBySalesValueQuery(Guid EstateId, Int32 numberOfProducts) : IRequest<Result<List<TopBottomData>>>;
    public record GetBottomProductsBySalesValueQuery(Guid EstateId, Int32 numberOfProducts) : IRequest<Result<List<TopBottomData>>>;
}