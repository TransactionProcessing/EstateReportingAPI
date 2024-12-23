using System.Diagnostics.CodeAnalysis;
using EstateReportingAPI.Models;
using MediatR;
using SimpleResults;

namespace EstateReportingAPI.BusinessLogic.Queries;

[ExcludeFromCodeCoverage]
public record MerchantQueries {
    public record GetMerchantsQuery(Guid EstateId) : IRequest<Result<List<Merchant>>>;
    public record GetTransactionKpisQuery(Guid EstateId) : IRequest<Result<MerchantKpi>>;
    public record GetByLastSaleQuery(Guid EstateId, DateTime StartDateTime, DateTime EndDateTime) : IRequest<Result<List<Merchant>>>;
    public record GetTopMerchantsBySalesValueQuery(Guid EstateId, Int32 numberOfMerchants) : IRequest<Result<List<TopBottomData>>>;
    public record GetBottomMerchantsBySalesValueQuery(Guid EstateId, Int32 numberOfMerchants) : IRequest<Result<List<TopBottomData>>>;
    public record GetMerchantPerformanceQuery(Guid EstateId, DateTime comparisonDate, List<Int32> merchantReportingIds) : IRequest<Result<TodaysSales>>;
}