using System.Diagnostics.CodeAnalysis;
using EstateReportingAPI.Models;
using MediatR;
using SimpleResults;

namespace EstateReportingAPI.BusinessLogic.Queries;

[ExcludeFromCodeCoverage]
public record MerchantQueries {
    public record GetRecentMerchantsQuery(Guid EstateId) : IRequest<Result<List<Merchant>>>;
    public record GetTransactionKpisQuery(Guid EstateId) : IRequest<Result<MerchantKpi>>;
    public record GetMerchantsQuery(Guid EstateId, MerchantQueryOptions QueryOptions) : IRequest<Result<List<Merchant>>>;
    public record GetMerchantQuery(Guid EstateId, Guid MerchantId) : IRequest<Result<Merchant>>;
    public record GetMerchantOperatorsQuery(Guid EstateId, Guid MerchantId) : IRequest<Result<List<MerchantOperator>>>;
    public record GetMerchantContractsQuery(Guid EstateId, Guid MerchantId) : IRequest<Result<List<MerchantContract>>>;
    public record GetMerchantDevicesQuery(Guid EstateId, Guid MerchantId) : IRequest<Result<List<MerchantDevice>>>;

    public record MerchantQueryOptions(String Name,String Reference,Int32? SettlementSchedule,String Region, String PostCode);
}