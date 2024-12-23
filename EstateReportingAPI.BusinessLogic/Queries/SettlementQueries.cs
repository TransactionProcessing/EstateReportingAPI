using EstateReportingAPI.Models;
using MediatR;
using SimpleResults;
using System.Diagnostics.CodeAnalysis;

namespace EstateReportingAPI.BusinessLogic.Queries
{
    [ExcludeFromCodeCoverage]
    public record SettlementQueries {
        public record GetTodaysSettlementQuery(Guid EstateId, Int32 MerchantReportingId, Int32 OperatorReportingId, DateTime ComparisonDate) : IRequest<Result<TodaysSettlement>>;

        public record GetLastSettlementQuery(Guid EstateId) : IRequest<Result<LastSettlement>>;

        public record GetUnsettledFeesQuery(Guid EstateId,DateTime StartDate, DateTime EndDate, List<Int32> MerchantIdFilter, List<Int32> OperatorIdFilter, List<Int32> ProductIdFilter, GroupByOption GroupByOption) : IRequest<Result<List<UnsettledFee>>>;

    }
}
