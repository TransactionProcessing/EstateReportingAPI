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
    IRequestHandler<MerchantQueries.GetMerchantDevicesQuery, Result<List<MerchantDevice>>>
{
    private readonly IReportingManager Manager;
    public MerchantRequestHandler(IReportingManager manager)
    {
        this.Manager = manager;
    }
        
    public async Task<Result<List<Merchant>>> Handle(MerchantQueries.GetRecentMerchantsQuery request,
                                                     CancellationToken cancellationToken) {
        return await this.Manager.GetRecentMerchants(request, cancellationToken);
    }
    public async Task<Result<MerchantKpi>> Handle(MerchantQueries.GetTransactionKpisQuery request,
                                                  CancellationToken cancellationToken)
    {
        return await this.Manager.GetMerchantsTransactionKpis(request, cancellationToken);
    }

    public async Task<Result<List<Merchant>>> Handle(MerchantQueries.GetMerchantsQuery request,
                                                     CancellationToken cancellationToken) {
        return await this.Manager.GetMerchants(request, cancellationToken);
    }

    public async Task<Result<Merchant>> Handle(MerchantQueries.GetMerchantQuery request,
                                               CancellationToken cancellationToken) {
        return await this.Manager.GetMerchant(request, cancellationToken);
    }

    public async Task<Result<List<MerchantContract>>> Handle(MerchantQueries.GetMerchantContractsQuery request,
                                                             CancellationToken cancellationToken) {
        return await this.Manager.GetMerchantContracts(request, cancellationToken);
    }

    public async Task<Result<List<MerchantOperator>>> Handle(MerchantQueries.GetMerchantOperatorsQuery request,
                                                             CancellationToken cancellationToken) {
        return await this.Manager.GetMerchantOperators(request, cancellationToken);
    }

    public async Task<Result<List<MerchantDevice>>> Handle(MerchantQueries.GetMerchantDevicesQuery request,
                                                           CancellationToken cancellationToken) {
        return await this.Manager.GetMerchantDevices(request, cancellationToken);
    }
}