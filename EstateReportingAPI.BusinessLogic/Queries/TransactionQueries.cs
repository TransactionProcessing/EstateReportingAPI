using System.Diagnostics.CodeAnalysis;
using EstateReportingAPI.Models;
using MediatR;
using SimpleResults;

namespace EstateReportingAPI.BusinessLogic.Queries;

[ExcludeFromCodeCoverage]
public record TransactionQueries {
    public record TodaysSalesQuery(Guid estateId, Int32 merchantReportingId, Int32 operatorReportingId, DateTime comparisonDate) : IRequest<Result<TodaysSales>>;
    public record TodaysFailedSales(Guid estateId, DateTime comparisonDate, String responseCode) : IRequest<Result<TodaysSales>>;

    public record TodaysSalesCountByHour(Guid estateId, Int32 merchantReportingId, Int32 operatorReportingId, DateTime comparisonDate) : IRequest<Result<List<Models.TodaysSalesCountByHour>>>;
    public record TodaysSalesValueByHour(Guid estateId, Int32 merchantReportingId, Int32 operatorReportingId, DateTime comparisonDate) : IRequest<Result<List<Models.TodaysSalesValueByHour>>>;

    public record TransactionSearchQuery(Guid estateId, TransactionSearchRequest request, PagingRequest pagingRequest, Models.SortingRequest sortingRequest) : IRequest<Result<List<Models.TransactionResult>>>;
}