using EstateReportingAPI.BusinessLogic.RequestHandlers;
using EstateReportingAPI.Models;
using Lamar;
using MediatR;
using SimpleResults;
using System.Diagnostics.CodeAnalysis;
using EstateReportingAPI.BusinessLogic.Queries;

namespace EstateReportingAPI.Bootstrapper;

[ExcludeFromCodeCoverage]
public class MediatorRegistry : ServiceRegistry {
    public MediatorRegistry() {
        this.AddTransient<IMediator, Mediator>();

        this.AddSingleton<IRequestHandler<TransactionQueries.TodaysSalesQuery, Result<TodaysSales>>, TransactionRequestHandler>();
        this.AddSingleton<IRequestHandler<TransactionQueries.TodaysFailedSales, Result<TodaysSales>>, TransactionRequestHandler>();
        this.AddSingleton<IRequestHandler<TransactionQueries.TodaysSalesCountByHour, Result<List<TodaysSalesCountByHour>>>, TransactionRequestHandler>();
        this.AddSingleton<IRequestHandler<TransactionQueries.TodaysSalesValueByHour, Result<List<TodaysSalesValueByHour>>>, TransactionRequestHandler>();
        this.AddSingleton<IRequestHandler<TransactionQueries.TransactionSearchQuery, Result<List<Models.TransactionResult>>>, TransactionRequestHandler>();

        this.AddSingleton<IRequestHandler<CalendarQueries.GetYearsQuery, Result<List<Int32>>>, CalendarRequestHandler>();
        this.AddSingleton<IRequestHandler<CalendarQueries.GetAllDatesQuery, Result<List<Calendar>>>, CalendarRequestHandler>();
        this.AddSingleton<IRequestHandler<CalendarQueries.GetComparisonDatesQuery, Result<List<Calendar>>>, CalendarRequestHandler>();
        
        this.AddSingleton<IRequestHandler<MerchantQueries.GetMerchantsQuery, Result<List<Merchant>>>, MerchantRequestHandler>();
        this.AddSingleton<IRequestHandler<MerchantQueries.GetBottomMerchantsBySalesValueQuery, Result<List<TopBottomData>>>, MerchantRequestHandler>();
        this.AddSingleton<IRequestHandler<MerchantQueries.GetTopMerchantsBySalesValueQuery, Result<List<TopBottomData>>>, MerchantRequestHandler>();
        this.AddSingleton<IRequestHandler<MerchantQueries.GetByLastSaleQuery, Result<List<Merchant>>>, MerchantRequestHandler>();
        this.AddSingleton<IRequestHandler<MerchantQueries.GetMerchantPerformanceQuery, Result<TodaysSales>>, MerchantRequestHandler>();
        this.AddSingleton<IRequestHandler<MerchantQueries.GetTransactionKpisQuery, Result<MerchantKpi>>, MerchantRequestHandler>();

        this.AddSingleton<IRequestHandler<OperatorQueries.GetOperatorsQuery, Result<List<Operator>>>, OperatorRequestHandler>();
        this.AddSingleton<IRequestHandler<OperatorQueries.GetOperatorPerformanceQuery, Result<TodaysSales>>, OperatorRequestHandler>();
        this.AddSingleton<IRequestHandler<OperatorQueries.GetBottomOperatorsBySalesValueQuery, Result<List<TopBottomData>>>, OperatorRequestHandler>();
        this.AddSingleton<IRequestHandler<OperatorQueries.GetTopOperatorsBySalesValueQuery, Result<List<TopBottomData>>>, OperatorRequestHandler>();

        this.AddSingleton<IRequestHandler<ResponseCodeQueries.GetResponseCodesQuery, Result<List<ResponseCode>>>, ResponseCodeRequestHandler>();
        
        this.AddSingleton<IRequestHandler<SettlementQueries.GetLastSettlementQuery, Result<LastSettlement>>, SettlementRequestHandler>();
        this.AddSingleton<IRequestHandler<SettlementQueries.GetTodaysSettlementQuery, Result<TodaysSettlement>>, SettlementRequestHandler>();
        this.AddSingleton<IRequestHandler<SettlementQueries.GetUnsettledFeesQuery, Result<List<UnsettledFee>>>, SettlementRequestHandler>();

        this.AddSingleton<IRequestHandler<ProductQueries.GetProductPerformanceQuery, Result<TodaysSales>>, ProductRequestHandler>();
        this.AddSingleton<IRequestHandler<ProductQueries.GetBottomProductsBySalesValueQuery, Result<List<TopBottomData>>>, ProductRequestHandler>();
        this.AddSingleton<IRequestHandler<ProductQueries.GetTopProductsBySalesValueQuery, Result<List<TopBottomData>>>, ProductRequestHandler>();
    }
}