using System.Diagnostics.CodeAnalysis;
using EstateReportingAPI.Models;
using MediatR;
using SimpleResults;

namespace EstateReportingAPI.BusinessLogic.Queries;

[ExcludeFromCodeCoverage]
public record TransactionQueries {
    public record TodaysSalesQuery(Guid EstateId, Int32 MerchantReportingId, Int32 OperatorReportingId, DateTime ComparisonDate) : IRequest<Result<TodaysSales>>;
    public record TodaysFailedSales(Guid EstateId, DateTime ComparisonDate, String ResponseCode) : IRequest<Result<TodaysSales>>;
    public record TransactionDetailReportQuery(Guid EstateId, TransactionDetailReportRequest Request) : IRequest<Result<TransactionDetailReportResponse>>;
    public record TransactionSummaryByMerchantQuery(Guid EstateId, TransactionSummaryByMerchantRequest Request) : IRequest<Result<TransactionSummaryByMerchantResponse>>;
    public record TransactionSummaryByOperatorQuery(Guid EstateId, TransactionSummaryByOperatorRequest Request) : IRequest<Result<TransactionSummaryByOperatorResponse>>;
    public record ProductPerformanceQuery(Guid EstateId, DateTime StartDate, DateTime EndDate) : IRequest<Result<ProductPerformanceResponse>>;
    public record TodaysSalesByHour(Guid estateId, DateTime comparisonDate) : IRequest<Result<List<Models.TodaysSalesByHour>>>;
}

[ExcludeFromCodeCoverage]
public record SettlementQueries {
    public record TodaysSettlementQuery(Guid EstateId, DateTime ComparisonDate) : IRequest<Result<Models.TodaysSettlement>>;
}