using System.Diagnostics.CodeAnalysis;
using EstateReportingAPI.Models;
using MediatR;
using SimpleResults;

namespace EstateReportingAPI.BusinessLogic.Queries;

[ExcludeFromCodeCoverage]
public record TransactionQueries {
    public record TodaysSalesQuery(Guid EstateId, Int32 MerchantReportingId, Int32 OperatorReportingId, DateTime ComparisonDate) : IRequest<Result<TodaysSales>>;
    public record TodaysFailedSales(Guid EstateId, DateTime ComparisonDate, String ResponseCode) : IRequest<Result<TodaysSales>>;
}