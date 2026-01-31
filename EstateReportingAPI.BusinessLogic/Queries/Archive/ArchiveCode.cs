using EstateReportingAPI.Models;
using MediatR;
using SimpleResults;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace EstateReportingAPI.BusinessLogic.Queries.Archive
{
    public record MerchantQueries
    {
        public record GetMerchantsQuery(Guid EstateId) : IRequest<Result<List<Merchant>>>;
        
        public record GetByLastSaleQuery(Guid EstateId, DateTime StartDateTime, DateTime EndDateTime) : IRequest<Result<List<Merchant>>>;
        public record GetTopMerchantsBySalesValueQuery(Guid EstateId, Int32 numberOfMerchants) : IRequest<Result<List<TopBottomData>>>;
        public record GetBottomMerchantsBySalesValueQuery(Guid EstateId, Int32 numberOfMerchants) : IRequest<Result<List<TopBottomData>>>;
        public record GetMerchantPerformanceQuery(Guid EstateId, DateTime comparisonDate, List<Int32> merchantReportingIds) : IRequest<Result<TodaysSales>>;
    }


    [ExcludeFromCodeCoverage]
    public record OperatorQueries
    {
        public record GetOperatorsQuery(Guid EstateId) : IRequest<Result<List<Operator>>>;
        public record GetOperatorPerformanceQuery(Guid EstateId, DateTime comparisonDate, List<Int32> operatorReportingIds) : IRequest<Result<TodaysSales>>;
        public record GetTopOperatorsBySalesValueQuery(Guid EstateId, Int32 numberOfOperators) : IRequest<Result<List<TopBottomData>>>;
        public record GetBottomOperatorsBySalesValueQuery(Guid EstateId, Int32 numberOfOperators) : IRequest<Result<List<TopBottomData>>>;
    }

    [ExcludeFromCodeCoverage]
    public record ProductQueries
    {
        public record GetProductPerformanceQuery(Guid EstateId, DateTime comparisonDate, List<Int32> productReportingIds) : IRequest<Result<TodaysSales>>;
        public record GetTopProductsBySalesValueQuery(Guid EstateId, Int32 numberOfProducts) : IRequest<Result<List<TopBottomData>>>;
        public record GetBottomProductsBySalesValueQuery(Guid EstateId, Int32 numberOfProducts) : IRequest<Result<List<TopBottomData>>>;
    }

    [ExcludeFromCodeCoverage]
    public record ResponseCodeQueries
    {
        public record GetResponseCodesQuery(Guid EstateId) : IRequest<Result<List<ResponseCode>>>;
    }

    [ExcludeFromCodeCoverage]
    public record SettlementQueries
    {
        public record GetTodaysSettlementQuery(Guid EstateId, Int32 MerchantReportingId, Int32 OperatorReportingId, DateTime ComparisonDate) : IRequest<Result<TodaysSettlement>>;

        public record GetLastSettlementQuery(Guid EstateId) : IRequest<Result<LastSettlement>>;

        public record GetUnsettledFeesQuery(Guid EstateId, DateTime StartDate, DateTime EndDate, List<Int32> MerchantIdFilter, List<Int32> OperatorIdFilter, List<Int32> ProductIdFilter, GroupByOption GroupByOption) : IRequest<Result<List<UnsettledFee>>>;
    }

    [ExcludeFromCodeCoverage]
    public record TransactionQueries
    {
        public record TodaysSalesQuery(Guid estateId, Int32 merchantReportingId, Int32 operatorReportingId, DateTime comparisonDate) : IRequest<Result<TodaysSales>>;
        public record TodaysFailedSales(Guid estateId, DateTime comparisonDate, String responseCode) : IRequest<Result<TodaysSales>>;

        public record TodaysSalesCountByHour(Guid estateId, Int32 merchantReportingId, Int32 operatorReportingId, DateTime comparisonDate) : IRequest<Result<List<Models.TodaysSalesCountByHour>>>;
        public record TodaysSalesValueByHour(Guid estateId, Int32 merchantReportingId, Int32 operatorReportingId, DateTime comparisonDate) : IRequest<Result<List<Models.TodaysSalesValueByHour>>>;

        public record TransactionSearchQuery(Guid estateId, TransactionSearchRequest request, PagingRequest pagingRequest, Models.SortingRequest sortingRequest) : IRequest<Result<List<Models.TransactionResult>>>;
    }
}
