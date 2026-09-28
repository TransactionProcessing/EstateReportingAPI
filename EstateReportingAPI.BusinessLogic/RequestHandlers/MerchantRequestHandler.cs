using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.Models;
using MediatR;
using SimpleResults;

namespace EstateReportingAPI.BusinessLogic.RequestHandlers;

public class MerchantRequestHandler : IRequestHandler<MerchantQueries.GetRecentMerchantsQuery, Result<List<Merchant>>>,
    IRequestHandler<MerchantQueries.GetTransactionKpisQuery, Result<MerchantKpi>>,
    IRequestHandler<MerchantQueries.GetMerchantsQuery, Result<List<Merchant>>>, 
    IRequestHandler<MerchantQueries.GetMerchantQuery, Result<Merchant>>,
    IRequestHandler<MerchantQueries.GetMerchantContractsQuery, Result<List<MerchantContract>>>,
    IRequestHandler<MerchantQueries.GetMerchantOperatorsQuery, Result<List<MerchantOperator>>>,
    IRequestHandler<MerchantQueries.GetMerchantDevicesQuery, Result<List<MerchantDevice>>>,
    IRequestHandler<MerchantQueries.GetMerchantOpeningHoursQuery, Result<List<MerchantOpeningHour>>>,
    IRequestHandler<MerchantQueries.GetMerchantScheduleQuery, Result<MerchantScheduleResponse>>
{
    private readonly IMerchantReportingService ReportingService;
    public MerchantRequestHandler(IMerchantReportingService reportingService)
    {
        this.ReportingService = reportingService;
    }
        
    public async Task<Result<List<Merchant>>> Handle(MerchantQueries.GetRecentMerchantsQuery request,
                                                     CancellationToken cancellationToken) {
        return await this.ReportingService.GetRecentMerchants(request, cancellationToken);
    }
    public async Task<Result<MerchantKpi>> Handle(MerchantQueries.GetTransactionKpisQuery request,
                                                  CancellationToken cancellationToken)
    {
        return await this.ReportingService.GetMerchantsTransactionKpis(request, cancellationToken);
    }

    public async Task<Result<List<Merchant>>> Handle(MerchantQueries.GetMerchantsQuery request,
                                                     CancellationToken cancellationToken) {
        return await this.ReportingService.GetMerchants(request, cancellationToken);
    }

    public async Task<Result<Merchant>> Handle(MerchantQueries.GetMerchantQuery request,
                                               CancellationToken cancellationToken) {
        return await this.ReportingService.GetMerchant(request, cancellationToken);
    }

    public async Task<Result<List<MerchantContract>>> Handle(MerchantQueries.GetMerchantContractsQuery request,
                                                             CancellationToken cancellationToken) {
        return await this.ReportingService.GetMerchantContracts(request, cancellationToken);
    }

    public async Task<Result<List<MerchantOperator>>> Handle(MerchantQueries.GetMerchantOperatorsQuery request,
                                                             CancellationToken cancellationToken) {
        return await this.ReportingService.GetMerchantOperators(request, cancellationToken);
    }

    public async Task<Result<List<MerchantDevice>>> Handle(MerchantQueries.GetMerchantDevicesQuery request,
                                                           CancellationToken cancellationToken) {
        return await this.ReportingService.GetMerchantDevices(request, cancellationToken);
    }

    public async Task<Result<List<MerchantOpeningHour>>> Handle(MerchantQueries.GetMerchantOpeningHoursQuery request,
                                                                CancellationToken cancellationToken) {
        return await this.ReportingService.GetMerchantOpeningHours(request, cancellationToken);
    }

    public async Task<Result<MerchantScheduleResponse>> Handle(MerchantQueries.GetMerchantScheduleQuery request,
                                                               CancellationToken cancellationToken) {
        return await this.ReportingService.GetMerchantSchedule(request, cancellationToken);
    }
}
