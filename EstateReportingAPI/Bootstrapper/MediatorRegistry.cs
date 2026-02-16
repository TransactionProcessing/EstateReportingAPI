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

        this.AddSingleton<IRequestHandler<EstateQueries.GetEstateQuery, Result<Estate>>, EstateRequestHandler >();
        this.AddSingleton<IRequestHandler<EstateQueries.GetEstateOperatorsQuery, Result<List<EstateOperator>>>, EstateRequestHandler>();

        this.AddSingleton<IRequestHandler<OperatorQueries.GetOperatorsQuery, Result<List<Operator>>>, OperatorRequestHandler>();
        this.AddSingleton<IRequestHandler<OperatorQueries.GetOperatorQuery, Result<Operator>>, OperatorRequestHandler>();

        this.AddSingleton<IRequestHandler<TransactionQueries.TodaysSalesQuery, Result<TodaysSales>>, TransactionRequestHandler>();
        this.AddSingleton<IRequestHandler<TransactionQueries.TodaysFailedSales, Result<TodaysSales>>, TransactionRequestHandler>();
        this.AddSingleton<IRequestHandler<TransactionQueries.TransactionDetailReportQuery, Result<TransactionDetailReportResponse>>, TransactionRequestHandler>();
        this.AddSingleton<IRequestHandler<TransactionQueries.TransactionSummaryByMerchantQuery, Result<TransactionSummaryByMerchantResponse>>, TransactionRequestHandler>();
        this.AddSingleton<IRequestHandler<TransactionQueries.TransactionSummaryByOperatorQuery, Result<TransactionSummaryByOperatorResponse>>, TransactionRequestHandler>();
        this.AddSingleton<IRequestHandler<TransactionQueries.ProductPerformanceQuery, Result<ProductPerformanceResponse>>, TransactionRequestHandler>();
        this.AddSingleton<IRequestHandler<TransactionQueries.TodaysSalesByHour, Result<List<TodaysSalesByHour>>>, TransactionRequestHandler>();

        this.AddSingleton<IRequestHandler<SettlementQueries.TodaysSettlementQuery, Result<TodaysSettlement>>,SettlementRequestHandler>();


        this.AddSingleton<IRequestHandler<CalendarQueries.GetYearsQuery, Result<List<Int32>>>, CalendarRequestHandler>();
        this.AddSingleton<IRequestHandler<CalendarQueries.GetAllDatesQuery, Result<List<Calendar>>>, CalendarRequestHandler>();
        this.AddSingleton<IRequestHandler<CalendarQueries.GetComparisonDatesQuery, Result<List<Calendar>>>, CalendarRequestHandler>();
        
        this.AddSingleton<IRequestHandler<MerchantQueries.GetRecentMerchantsQuery, Result<List<Merchant>>>, MerchantRequestHandler>();
        this.AddSingleton<IRequestHandler<MerchantQueries.GetTransactionKpisQuery, Result<MerchantKpi>>, MerchantRequestHandler>();
        this.AddSingleton<IRequestHandler<MerchantQueries.GetMerchantsQuery, Result<List<Merchant>>>, MerchantRequestHandler>();
        this.AddSingleton<IRequestHandler<MerchantQueries.GetMerchantQuery, Result<Merchant>>, MerchantRequestHandler>();
        this.AddSingleton<IRequestHandler<MerchantQueries.GetMerchantContractsQuery, Result<List<MerchantContract>>>, MerchantRequestHandler>();
        this.AddSingleton<IRequestHandler<MerchantQueries.GetMerchantOperatorsQuery, Result<List<MerchantOperator>>>, MerchantRequestHandler>();
        this.AddSingleton<IRequestHandler<MerchantQueries.GetMerchantDevicesQuery, Result<List<MerchantDevice>>>, MerchantRequestHandler>();

        this.AddSingleton<IRequestHandler<ContractQueries.GetRecentContractsQuery, Result<List<Contract>>>, ContractRequestHandler>();
        this.AddSingleton<IRequestHandler<ContractQueries.GetContractsQuery, Result<List<Contract>>>, ContractRequestHandler>();
        this.AddSingleton<IRequestHandler<ContractQueries.GetContractQuery, Result<Contract>>, ContractRequestHandler>();
    }
}


