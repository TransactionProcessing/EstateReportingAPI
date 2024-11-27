using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.BusinessLogic.RequestHandlers;
using EstateReportingAPI.Models;
using Lamar;
using MediatR;
using SimpleResults;

namespace EstateReportingAPI.Bootstrapper;

public class MediatorRegistry : ServiceRegistry {
    public MediatorRegistry() {
        this.AddTransient<IMediator, Mediator>();

        this.AddSingleton<IRequestHandler<CalendarQueries.GetYearsQuery, Result<List<Int32>>>, CalendarRequestHandler>();
        this.AddSingleton<IRequestHandler<CalendarQueries.GetAllDatesQuery, Result<List<Calendar>>>, CalendarRequestHandler>();
        this.AddSingleton<IRequestHandler<CalendarQueries.GetComparisonDatesQuery, Result<List<Calendar>>>, CalendarRequestHandler>();
        
        this.AddSingleton<IRequestHandler<MerchantQueries.GetMerchantsQuery, Result<List<Merchant>>>, MerchantRequestHandler>();

        this.AddSingleton<IRequestHandler<OperatorQueries.GetOperatorsQuery, Result<List<Operator>>>, OperatorRequestHandler>();

        this.AddSingleton<IRequestHandler<ResponseCodeQueries.GetResponseCodesQuery, Result<List<ResponseCode>>>, ResponseCodeRequestHandler>();
        
        this.AddSingleton<IRequestHandler<SettlementQueries.GetLastSettlementQuery, Result<LastSettlement>>, SettlementRequestHandler>();
        this.AddSingleton<IRequestHandler<SettlementQueries.GetTodaysSettlementQuery, Result<TodaysSettlement>>, SettlementRequestHandler>();
        this.AddSingleton<IRequestHandler<SettlementQueries.GetUnsettledFeesQuery, Result<List<UnsettledFee>>>, SettlementRequestHandler>();
    }
}