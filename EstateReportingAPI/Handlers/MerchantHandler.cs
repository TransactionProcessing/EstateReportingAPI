using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Results.Web;
using SimpleResults;

namespace EstateReportingAPI.Handlers;

public static class MerchantHandler
{
    public static async Task<IResult> GetMerchantKpis([FromHeader] Guid estateId,
                                                      IMediator mediator,
                                                      CancellationToken cancellationToken)
    {
        MerchantQueries.GetTransactionKpisQuery query = new(estateId);
        Result<MerchantKpi> result = await mediator.Send(query, cancellationToken);
        return ResponseFactory.FromResult(result, r =>
            new DataTransferObjects.MerchantKpi()
            {
                MerchantsWithNoSaleInLast7Days =r.MerchantsWithNoSaleInLast7Days,
                MerchantsWithNoSaleToday = r.MerchantsWithNoSaleToday,
                MerchantsWithSaleInLastHour = r.MerchantsWithSaleInLastHour
            });
    }

    public static async Task<IResult> GetRecentMerchants([FromHeader] Guid estateId,
                                                         IMediator mediator,
                                                         CancellationToken cancellationToken) {
        MerchantQueries.GetRecentMerchantsQuery query = new(estateId);
        Result<List<Merchant>> result = await mediator.Send(query, cancellationToken);
        return ResponseFactory.FromResult(result, r => r.Select(m => new EstateReportingAPI.DataTransferObjects.Merchant
        {
            MerchantReportingId = m.MerchantReportingId,
            MerchantId = m.MerchantId,
            Name = m.Name,
            Reference = m.Reference,
            SettlementSchedule = m.SettlementSchedule,
            CreatedDateTime = m.CreatedDateTime,
            Balance = m.Balance,
            AddressLine1 = m.AddressLine1,
            AddressLine2 = m.AddressLine2,
            Town = m.Town,
            Region = m.Region,
            PostCode = m.PostCode,
            Country = m.Country,
            ContactName = m.ContactName,
            ContactEmail = m.ContactEmail,
            ContactPhone = m.ContactPhone
                
        }).OrderByDescending(m => m.CreatedDateTime).ToList());
    }

    public static async Task<IResult> GetMerchants([FromHeader] Guid estateId,
                                                   [FromQuery] String? name,
                                                   [FromQuery] String? reference,
                                                   [FromQuery] Int32? settlementSchedule,
                                                   [FromQuery] String? region,
                                                   [FromQuery] String? postCode,
                                                   IMediator mediator,
                                                   CancellationToken cancellationToken)
    {
        // Build the query options
        MerchantQueries.MerchantQueryOptions queryOptions = new(name, reference, settlementSchedule, region, postCode);
        MerchantQueries.GetMerchantsQuery query = new(estateId, queryOptions);
        Result<List<Merchant>> result = await mediator.Send(query, cancellationToken);

        return ResponseFactory.FromResult(result, r => r.Select(m => new EstateReportingAPI.DataTransferObjects.Merchant
        {
            MerchantReportingId = m.MerchantReportingId,
            MerchantId = m.MerchantId,
            Name = m.Name,
            Reference = m.Reference,
            SettlementSchedule = m.SettlementSchedule,
            CreatedDateTime = m.CreatedDateTime,
            Balance = m.Balance,
            AddressLine1 = m.AddressLine1,
            AddressLine2 = m.AddressLine2,
            Town = m.Town,
            Region = m.Region,
            PostCode = m.PostCode,
            Country = m.Country,
            ContactName = m.ContactName,
            ContactEmail = m.ContactEmail,
            ContactPhone = m.ContactPhone
        }).OrderByDescending(m => m.CreatedDateTime).ToList());
    }

    public static async Task<IResult> GetMerchant([FromHeader] Guid estateId,
                                                  [FromRoute] Guid merchantId,
                                                  IMediator mediator,
                                                  CancellationToken cancellationToken)
    {
        // Build the query options
        MerchantQueries.GetMerchantQuery query = new(estateId, merchantId);
        Result<Merchant> result = await mediator.Send(query, cancellationToken);

        return ResponseFactory.FromResult(result, r => new EstateReportingAPI.DataTransferObjects.Merchant
        {
            MerchantReportingId = r.MerchantReportingId,
            MerchantId = r.MerchantId,
            Name = r.Name,
            Reference = r.Reference,
            SettlementSchedule = r.SettlementSchedule,
            CreatedDateTime = r.CreatedDateTime,
            Balance = r.Balance,
            AddressLine1 = r.AddressLine1,
            AddressLine2 = r.AddressLine2,
            Town = r.Town,
            Region = r.Region,
            PostCode = r.PostCode,
            Country = r.Country,
            ContactName = r.ContactName,
            ContactEmail = r.ContactEmail,
            ContactPhone = r.ContactPhone,
            AddressId = r.AddressId,
            ContactId = r.ContactId
        });
    }

    public static async Task<IResult> GetMerchantOperators([FromHeader] Guid estateId,
                                                           [FromRoute] Guid merchantId,
                                                           IMediator mediator,
                                                           CancellationToken cancellationToken) {
        MerchantQueries.GetMerchantOperatorsQuery query = new(estateId, merchantId);
        Result<List<MerchantOperator>> result = await mediator.Send(query, cancellationToken);

        return ResponseFactory.FromResult(result, r => r.Select(o => new DataTransferObjects.MerchantOperator() {
            IsDeleted = o.IsDeleted,
            MerchantId = o.MerchantId,
            MerchantNumber = o.MerchantNumber,
            OperatorId = o.OperatorId,
            OperatorName = o.OperatorName,
            TerminalNumber = o.TerminalNumber
        }).ToList());
    }

    public static async Task<IResult> GetMerchantContracts([FromHeader] Guid estateId,
                                                           [FromRoute] Guid merchantId,
                                                           IMediator mediator,
                                                           CancellationToken cancellationToken)
    {
        MerchantQueries.GetMerchantContractsQuery query = new(estateId, merchantId);
        Result<List<MerchantContract>> result = await mediator.Send(query, cancellationToken);

        return ResponseFactory.FromResult(result, r => r.Select(c => new DataTransferObjects.MerchantContract()
        {
            ContractId = c.ContractId,
            ContractName = c.ContractName,
            OperatorName = c.OperatorName,
            IsDeleted = c.IsDeleted,
            MerchantId = c.MerchantId,
            ContractProducts = c.ContractProducts.Select(p => new DataTransferObjects.MerchantContractProduct()
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                ContractId = c.ContractId,
                DisplayText = p.DisplayText,
                MerchantId = c.MerchantId
            }).ToList()
        }).ToList());
    }

    public static async Task<IResult> GetMerchantDevices([FromHeader] Guid estateId,
                                                         [FromRoute] Guid merchantId,
                                                         IMediator mediator,
                                                         CancellationToken cancellationToken)
    {
        MerchantQueries.GetMerchantDevicesQuery query = new(estateId, merchantId);
        Result<List<MerchantDevice>> result = await mediator.Send(query, cancellationToken);

        return ResponseFactory.FromResult(result, r => r.Select(c => new DataTransferObjects.MerchantDevice()
        {
            IsDeleted = c.IsDeleted,
            MerchantId = c.MerchantId,
            DeviceId = c.DeviceId,
            DeviceIdentifier = c.DeviceIdentifier
        }).ToList());
    }
}