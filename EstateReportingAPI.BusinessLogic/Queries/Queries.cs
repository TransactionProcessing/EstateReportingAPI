using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EstateReportingAPI.Models;
using MediatR;
using SimpleResults;

namespace EstateReportingAPI.BusinessLogic.Queries
{
    public record CalendarQueries {
        public record GetComparisonDatesQuery(Guid EstateId) : IRequest<Result<List<Calendar>>>;
        public record GetAllDatesQuery(Guid EstateId) : IRequest<Result<List<Calendar>>>;
        public record GetYearsQuery(Guid EstateId) : IRequest<Result<List<Int32>>>;
    }

    public record MerchantQueries {
        public record GetMerchantsQuery(Guid EstateId) : IRequest<Result<List<Merchant>>>;
        public record GetTransactionKpisQuery(Guid EstateId) : IRequest<Result<MerchantKpi>>;
        public record GetByLastSaleQuery(Guid EstateId, DateTime StartDateTime, DateTime EndDateTime) : IRequest<Result<List<Merchant>>>;
        public record GetTopMerchantsBySalesValueQuery(Guid EstateId, Int32 numberOfMerchants) : IRequest<Result<List<TopBottomData>>>;
        public record GetBottomMerchantsBySalesValueQuery(Guid EstateId, Int32 numberOfMerchants) : IRequest<Result<List<TopBottomData>>>;
        public record GetMerchantPerformanceQuery(Guid EstateId, DateTime comparisonDate, List<Int32> merchantReportingIds) : IRequest<Result<TodaysSales>>;
    }

    public record OperatorQueries {
        public record GetOperatorsQuery(Guid EstateId) : IRequest<Result<List<Operator>>>;
        public record GetOperatorPerformanceQuery(Guid EstateId, DateTime comparisonDate, List<Int32> operatorReportingIds) : IRequest<Result<TodaysSales>>;
        public record GetTopOperatorsBySalesValueQuery(Guid EstateId, Int32 numberOfOperators) : IRequest<Result<List<TopBottomData>>>;
        public record GetBottomOperatorsBySalesValueQuery(Guid EstateId, Int32 numberOfOperators) : IRequest<Result<List<TopBottomData>>>;
    }

    public record ProductQueries
    {
        public record GetProductPerformanceQuery(Guid EstateId, DateTime comparisonDate, List<Int32> productReportingIds) : IRequest<Result<TodaysSales>>;
        public record GetTopProductsBySalesValueQuery(Guid EstateId, Int32 numberOfProducts) : IRequest<Result<List<TopBottomData>>>;
        public record GetBottomProductsBySalesValueQuery(Guid EstateId, Int32 numberOfProducts) : IRequest<Result<List<TopBottomData>>>;
    }

    public record ResponseCodeQueries {
        public record GetResponseCodesQuery(Guid EstateId) : IRequest<Result<List<ResponseCode>>>;
    }
}
