using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.Models;
using SimpleResults;

namespace EstateReportingAPI.BusinessLogic;

public interface IMerchantReportingService
{
    Task<Result<List<Contract>>> GetRecentContracts(ContractQueries.GetRecentContractsQuery request, CancellationToken cancellationToken);
    Task<Result<List<Contract>>> GetContracts(ContractQueries.GetContractsQuery request, CancellationToken cancellationToken);
    Task<Result<Contract>> GetContract(ContractQueries.GetContractQuery request, CancellationToken cancellationToken);
    Task<Result<List<Merchant>>> GetRecentMerchants(MerchantQueries.GetRecentMerchantsQuery request, CancellationToken cancellationToken);
    Task<Result<MerchantKpi>> GetMerchantsTransactionKpis(MerchantQueries.GetTransactionKpisQuery request, CancellationToken cancellationToken);
    Task<Result<List<Operator>>> GetOperators(OperatorQueries.GetOperatorsQuery request, CancellationToken cancellationToken);
    Task<Result<List<MerchantOpeningHour>>> GetMerchantOpeningHours(MerchantQueries.GetMerchantOpeningHoursQuery request, CancellationToken cancellationToken);
    Task<Result<Operator>> GetOperator(OperatorQueries.GetOperatorQuery request, CancellationToken cancellationToken);
    Task<Result<List<Merchant>>> GetMerchants(MerchantQueries.GetMerchantsQuery request, CancellationToken cancellationToken);
    Task<Result<Merchant>> GetMerchant(MerchantQueries.GetMerchantQuery request, CancellationToken cancellationToken);
    Task<Result<List<MerchantOperator>>> GetMerchantOperators(MerchantQueries.GetMerchantOperatorsQuery request, CancellationToken cancellationToken);
    Task<Result<List<MerchantContract>>> GetMerchantContracts(MerchantQueries.GetMerchantContractsQuery request, CancellationToken cancellationToken);
    Task<Result<List<MerchantDevice>>> GetMerchantDevices(MerchantQueries.GetMerchantDevicesQuery request, CancellationToken cancellationToken);
    Task<Result<MerchantScheduleResponse>> GetMerchantSchedule(MerchantQueries.GetMerchantScheduleQuery request, CancellationToken cancellationToken);
}
