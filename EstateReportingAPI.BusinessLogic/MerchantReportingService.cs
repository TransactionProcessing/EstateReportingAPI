using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.Models;
using SimpleResults;
using TransactionProcessor.Database.Contexts;
using Shared.EntityFramework;

namespace EstateReportingAPI.BusinessLogic;

/// <summary>
/// Reporting service for merchant, operator and contract queries.
/// </summary>
public sealed class MerchantReportingService : IMerchantReportingService
{
    private readonly ReportingManager Manager;

    public MerchantReportingService(IDbContextResolver<EstateManagementContext> resolver)
    {
        Manager = new ReportingManager(resolver);
    }

    public Task<Result<List<Contract>>> GetRecentContracts(ContractQueries.GetRecentContractsQuery request, CancellationToken cancellationToken) => Manager.GetRecentContracts(request, cancellationToken);
    public Task<Result<List<Contract>>> GetContracts(ContractQueries.GetContractsQuery request, CancellationToken cancellationToken) => Manager.GetContracts(request, cancellationToken);
    public Task<Result<Contract>> GetContract(ContractQueries.GetContractQuery request, CancellationToken cancellationToken) => Manager.GetContract(request, cancellationToken);
    public Task<Result<List<Merchant>>> GetRecentMerchants(MerchantQueries.GetRecentMerchantsQuery request, CancellationToken cancellationToken) => Manager.GetRecentMerchants(request, cancellationToken);
    public Task<Result<MerchantKpi>> GetMerchantsTransactionKpis(MerchantQueries.GetTransactionKpisQuery request, CancellationToken cancellationToken) => Manager.GetMerchantsTransactionKpis(request, cancellationToken);
    public Task<Result<List<Operator>>> GetOperators(OperatorQueries.GetOperatorsQuery request, CancellationToken cancellationToken) => Manager.GetOperators(request, cancellationToken);
    public Task<Result<List<MerchantOpeningHour>>> GetMerchantOpeningHours(MerchantQueries.GetMerchantOpeningHoursQuery request, CancellationToken cancellationToken) => Manager.GetMerchantOpeningHours(request, cancellationToken);
    public Task<Result<Operator>> GetOperator(OperatorQueries.GetOperatorQuery request, CancellationToken cancellationToken) => Manager.GetOperator(request, cancellationToken);
    public Task<Result<List<Merchant>>> GetMerchants(MerchantQueries.GetMerchantsQuery request, CancellationToken cancellationToken) => Manager.GetMerchants(request, cancellationToken);
    public Task<Result<Merchant>> GetMerchant(MerchantQueries.GetMerchantQuery request, CancellationToken cancellationToken) => Manager.GetMerchant(request, cancellationToken);
    public Task<Result<List<MerchantOperator>>> GetMerchantOperators(MerchantQueries.GetMerchantOperatorsQuery request, CancellationToken cancellationToken) => Manager.GetMerchantOperators(request, cancellationToken);
    public Task<Result<List<MerchantContract>>> GetMerchantContracts(MerchantQueries.GetMerchantContractsQuery request, CancellationToken cancellationToken) => Manager.GetMerchantContracts(request, cancellationToken);
    public Task<Result<List<MerchantDevice>>> GetMerchantDevices(MerchantQueries.GetMerchantDevicesQuery request, CancellationToken cancellationToken) => Manager.GetMerchantDevices(request, cancellationToken);
    public Task<Result<MerchantScheduleResponse>> GetMerchantSchedule(MerchantQueries.GetMerchantScheduleQuery request, CancellationToken cancellationToken) => Manager.GetMerchantSchedule(request, cancellationToken);
}
