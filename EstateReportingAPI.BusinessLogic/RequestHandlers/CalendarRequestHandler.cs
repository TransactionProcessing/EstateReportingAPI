using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.Models;
using MediatR;
using SimpleResults;

namespace EstateReportingAPI.BusinessLogic.RequestHandlers
{
    public class TransactionRequestHandler : IRequestHandler<TransactionQueries.TodaysFailedSales, Result<TodaysSales>>,
        IRequestHandler<TransactionQueries.TodaysSalesQuery, Result<TodaysSales>>,
        IRequestHandler<TransactionQueries.TodaysSalesCountByHour, Result<List<TodaysSalesCountByHour>>>, 
        IRequestHandler<TransactionQueries.TodaysSalesValueByHour, Result<List<TodaysSalesValueByHour>>>,
        IRequestHandler<TransactionQueries.TransactionSearchQuery, Result<List<Models.TransactionResult>>> {
        private readonly IReportingManager Manager;

        public TransactionRequestHandler(IReportingManager manager) {
            this.Manager = manager;
        }

        public async Task<Result<TodaysSales>> Handle(TransactionQueries.TodaysFailedSales request,
                                                      CancellationToken cancellationToken) {
            var result = await this.Manager.GetTodaysFailedSales(request.estateId, request.comparisonDate, request.responseCode, cancellationToken);
            return Result.Success(result);
        }

        public async Task<Result<TodaysSales>> Handle(TransactionQueries.TodaysSalesQuery request,
                                                      CancellationToken cancellationToken) {
            var result = await this.Manager.GetTodaysSales(request.estateId, request.merchantReportingId, request.operatorReportingId, request.comparisonDate, cancellationToken);
            return Result.Success(result);
        }

        public async Task<Result<List<TodaysSalesCountByHour>>> Handle(TransactionQueries.TodaysSalesCountByHour request,
                                                                       CancellationToken cancellationToken) {
            var result = await this.Manager.GetTodaysSalesCountByHour(request.estateId, request.merchantReportingId, request.operatorReportingId, request.comparisonDate, cancellationToken);
            return Result.Success(result);
        }

        public async Task<Result<List<TodaysSalesValueByHour>>> Handle(TransactionQueries.TodaysSalesValueByHour request,
                                                                       CancellationToken cancellationToken) {
            var result = await this.Manager.GetTodaysSalesValueByHour(request.estateId, request.merchantReportingId, request.operatorReportingId, request.comparisonDate, cancellationToken);
            return Result.Success(result);
        }

        public async Task<Result<List<TransactionResult>>> Handle(TransactionQueries.TransactionSearchQuery request,
                                                                  CancellationToken cancellationToken) {
            var result = await this.Manager.TransactionSearch(request.estateId, request.request, request.pagingRequest, request.sortingRequest, cancellationToken);
            return Result.Success(result);
        }

        
    }



    public class CalendarRequestHandler : IRequestHandler<CalendarQueries.GetAllDatesQuery, Result<List<Calendar>>>,
        IRequestHandler<CalendarQueries.GetComparisonDatesQuery, Result<List<Calendar>>>,
        IRequestHandler<CalendarQueries.GetYearsQuery, Result<List<Int32>>> {
        private readonly IReportingManager Manager;
        public CalendarRequestHandler(IReportingManager manager) {
            this.Manager = manager;
        }
        public async Task<Result<List<Calendar>>> Handle(CalendarQueries.GetAllDatesQuery request,
                                                         CancellationToken cancellationToken) {
            List<Calendar> result = await this.Manager.GetCalendarDates(request.EstateId, cancellationToken);

            if (result.Any() == false) {
                return Result.NotFound("No calendar dates found");
            }

            return Result.Success(result);

        }

        public async Task<Result<List<Calendar>>> Handle(CalendarQueries.GetComparisonDatesQuery request,
                                                         CancellationToken cancellationToken) {
            List<Calendar> result = await this.Manager.GetCalendarComparisonDates(request.EstateId, cancellationToken);
            if (result.Any() == false)
            {
                return Result.NotFound("No calendar comparison dates found");
            }

            return Result.Success(result);
        }

        public async Task<Result<List<Int32>>> Handle(CalendarQueries.GetYearsQuery request,
                                                      CancellationToken cancellationToken) {
            List<Int32> result = await this.Manager.GetCalendarYears(request.EstateId, cancellationToken);
            if (result.Any() == false)
            {
                return Result.NotFound("No calendar years found");
            }

            return Result.Success(result);
        }
    }

    public class MerchantRequestHandler :IRequestHandler<MerchantQueries.GetMerchantsQuery, Result<List<Merchant>>>,
        IRequestHandler<MerchantQueries.GetByLastSaleQuery, Result<List<Merchant>>>,
    IRequestHandler<MerchantQueries.GetMerchantPerformanceQuery, Result<TodaysSales>>,
    IRequestHandler<MerchantQueries.GetTransactionKpisQuery, Result<MerchantKpi>>,
    IRequestHandler<MerchantQueries.GetBottomMerchantsBySalesValueQuery, Result<List<TopBottomData>>>,
        IRequestHandler<MerchantQueries.GetTopMerchantsBySalesValueQuery, Result<List<TopBottomData>>>
    {
        private readonly IReportingManager Manager;
        public MerchantRequestHandler(IReportingManager manager)
        {
            this.Manager = manager;
        }
        public async Task<Result<List<Merchant>>> Handle(MerchantQueries.GetMerchantsQuery request,
                                                         CancellationToken cancellationToken) {
            List<Merchant> result = await this.Manager.GetMerchants(request.EstateId, cancellationToken);
            if (result.Any() == false)
            {
                return Result.NotFound("No merchants found");
            }

            return Result.Success(result);
        }

        public async Task<Result<List<Merchant>>> Handle(MerchantQueries.GetByLastSaleQuery request,
                                                         CancellationToken cancellationToken) {
            var result = await this.Manager.GetMerchantsByLastSale(request.EstateId, request.StartDateTime, request.EndDateTime, cancellationToken);
            return Result.Success(result);
        }

        public async Task<Result<TodaysSales>> Handle(MerchantQueries.GetMerchantPerformanceQuery request,
                                                      CancellationToken cancellationToken) {
            var result = await this.Manager.GetMerchantPerformance(request.EstateId, request.comparisonDate, request.merchantReportingIds, cancellationToken);
            return Result.Success(result);
        }

        public async Task<Result<MerchantKpi>> Handle(MerchantQueries.GetTransactionKpisQuery request,
                                                      CancellationToken cancellationToken) {
            var result = await this.Manager.GetMerchantsTransactionKpis(request.EstateId, cancellationToken);
            return Result.Success(result);
        }

        public async Task<Result<List<TopBottomData>>> Handle(MerchantQueries.GetBottomMerchantsBySalesValueQuery request,
                                                              CancellationToken cancellationToken) {
            var result = await this.Manager.GetTopBottomData(request.EstateId, TopBottom.Bottom, request.numberOfMerchants, Dimension.Merchant, cancellationToken);
            return Result.Success(result);
        }

        public async Task<Result<List<TopBottomData>>> Handle(MerchantQueries.GetTopMerchantsBySalesValueQuery request,
                                                              CancellationToken cancellationToken) {
            var result = await this.Manager.GetTopBottomData(request.EstateId, TopBottom.Top, request.numberOfMerchants, Dimension.Merchant, cancellationToken);
            return Result.Success(result);
        }
    }

    public class OperatorRequestHandler : IRequestHandler<OperatorQueries.GetOperatorsQuery, Result<List<Operator>>>,
        IRequestHandler<OperatorQueries.GetOperatorPerformanceQuery, Result<TodaysSales>>,
        IRequestHandler<OperatorQueries.GetTopOperatorsBySalesValueQuery, Result<List<TopBottomData>>>,
        IRequestHandler<OperatorQueries.GetBottomOperatorsBySalesValueQuery, Result<List<TopBottomData>>>
    {
        private readonly IReportingManager Manager;
        public OperatorRequestHandler(IReportingManager manager)
        {
            this.Manager = manager;
        }

        public async Task<Result<List<Operator>>> Handle(OperatorQueries.GetOperatorsQuery request,
                                                         CancellationToken cancellationToken)
        {
            List<Operator> result = await this.Manager.GetOperators(request.EstateId, cancellationToken);
            if (result.Any() == false)
            {
                return Result.NotFound("No operators found");
            }

            return Result.Success(result);
        }
        
        public async Task<Result<TodaysSales>> Handle(OperatorQueries.GetOperatorPerformanceQuery request,
                                                      CancellationToken cancellationToken)
        {
            var result = await this.Manager.GetOperatorPerformance(request.EstateId, request.comparisonDate, request.operatorReportingIds, cancellationToken);
            return Result.Success(result);
        }

        public async Task<Result<List<TopBottomData>>> Handle(OperatorQueries.GetTopOperatorsBySalesValueQuery request,
                                                              CancellationToken cancellationToken) {
            var result = await this.Manager.GetTopBottomData(request.EstateId, TopBottom.Top, request.numberOfOperators, Dimension.Operator, cancellationToken);
            return Result.Success(result);
        }

        public async Task<Result<List<TopBottomData>>> Handle(OperatorQueries.GetBottomOperatorsBySalesValueQuery request,
                                                              CancellationToken cancellationToken) {
            var result = await this.Manager.GetTopBottomData(request.EstateId, TopBottom.Bottom, request.numberOfOperators, Dimension.Operator, cancellationToken);
            return Result.Success(result);
        }
    }

    public class ResponseCodeRequestHandler : IRequestHandler<ResponseCodeQueries.GetResponseCodesQuery, Result<List<ResponseCode>>> {
        private readonly IReportingManager Manager;

        public ResponseCodeRequestHandler(IReportingManager manager) {
            this.Manager = manager;
        }

        public async Task<Result<List<ResponseCode>>> Handle(ResponseCodeQueries.GetResponseCodesQuery request,
                                                             CancellationToken cancellationToken) {
            List<ResponseCode> result = await this.Manager.GetResponseCodes(request.EstateId, cancellationToken);
            if (result.Any() == false) {
                return Result.NotFound("No response codes found");
            }

            return Result.Success(result);
        }
    }

    public class SettlementRequestHandler : IRequestHandler<SettlementQueries.GetTodaysSettlementQuery, Result<TodaysSettlement>>,
        IRequestHandler<SettlementQueries.GetLastSettlementQuery, Result<LastSettlement>>,
        IRequestHandler<SettlementQueries.GetUnsettledFeesQuery, Result<List<UnsettledFee>>> {
        private readonly IReportingManager Manager;
        public SettlementRequestHandler(IReportingManager manager)
        {
            this.Manager = manager;
        }

        public async Task<Result<TodaysSettlement>> Handle(SettlementQueries.GetTodaysSettlementQuery request,
                                                           CancellationToken cancellationToken) {
            Models.TodaysSettlement model = await this.Manager.GetTodaysSettlement(request.EstateId, request.MerchantReportingId, request.OperatorReportingId, request.ComparisonDate, cancellationToken);

            return Result.Success(model);
        }

        public async Task<Result<LastSettlement>> Handle(SettlementQueries.GetLastSettlementQuery request,
                                                         CancellationToken cancellationToken)
        {
            LastSettlement model = await this.Manager.GetLastSettlement(request.EstateId, cancellationToken);

            return Result.Success(model);
        }

        public async Task<Result<List<UnsettledFee>>> Handle(SettlementQueries.GetUnsettledFeesQuery request,
                                                             CancellationToken cancellationToken) {
            List<UnsettledFee> model = await this.Manager.GetUnsettledFees(request.EstateId, request.StartDate, request.EndDate, request.MerchantIdFilter, request.OperatorIdFilter, request.ProductIdFilter, request.GroupByOption, cancellationToken);
            return Result.Success(model);
        }
    }

    public class ProductRequestHandler : IRequestHandler<ProductQueries.GetProductPerformanceQuery, Result<TodaysSales>>,
    IRequestHandler<ProductQueries.GetTopProductsBySalesValueQuery, Result<List<TopBottomData>>>,
    IRequestHandler<ProductQueries.GetBottomProductsBySalesValueQuery, Result<List<TopBottomData>>>
    {
        private readonly IReportingManager Manager;

        public ProductRequestHandler(IReportingManager manager) {
            this.Manager = manager;
        }
        public async Task<Result<TodaysSales>> Handle(ProductQueries.GetProductPerformanceQuery request,
                                                      CancellationToken cancellationToken) {
            var result = await this.Manager.GetProductPerformance(request.EstateId, request.comparisonDate, request.productReportingIds, cancellationToken);

            return Result.Success(result);
        }

        public async Task<Result<List<TopBottomData>>> Handle(ProductQueries.GetTopProductsBySalesValueQuery request,
                                                              CancellationToken cancellationToken) {
            var result = await this.Manager.GetTopBottomData(request.EstateId, TopBottom.Top, request.numberOfProducts, Dimension.Product, cancellationToken);
            return Result.Success(result);
        }

        public async Task<Result<List<TopBottomData>>> Handle(ProductQueries.GetBottomProductsBySalesValueQuery request,
                                                              CancellationToken cancellationToken) {
            var result = await this.Manager.GetTopBottomData(request.EstateId, TopBottom.Bottom, request.numberOfProducts, Dimension.Product, cancellationToken);
            return Result.Success(result);
        }
    }
}
