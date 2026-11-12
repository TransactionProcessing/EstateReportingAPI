using EstateReportingAPI.BusinessLogic.Queries;
using System.ComponentModel;
using EstateReportingAPI.Common;
using EstateReportingAPI.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TransactionProcessing.UnionResults.AspNetCore;
using TransactionProcessing.UnionResults;

namespace EstateReportingAPI.Handlers;

public static class MerchantHandler {
    public static async Task<Result<DataTransferObjects.MerchantKpi>> GetMerchantKpis(IEstateContext estateContext,
                                                      IMediator mediator,
                                                      CancellationToken cancellationToken) {
        MerchantQueries.GetTransactionKpisQuery query = new(estateContext.EstateId);
        Result<MerchantKpi> result = await mediator.Send(query, cancellationToken);
        return result.Map(r => new DataTransferObjects.MerchantKpi() { MerchantsWithNoSaleInLast7Days = r.MerchantsWithNoSaleInLast7Days, MerchantsWithNoSaleToday = r.MerchantsWithNoSaleToday, MerchantsWithSaleInLastHour = r.MerchantsWithSaleInLastHour });
    }

    public static async Task<Result<List<DataTransferObjects.Merchant>>> GetRecentMerchants(IEstateContext estateContext,
                                                         IMediator mediator,
                                                         CancellationToken cancellationToken) {
        MerchantQueries.GetRecentMerchantsQuery query = new(estateContext.EstateId);
        Result<List<Merchant>> result = await mediator.Send(query, cancellationToken);
        return result.Map(r => r.Select(m => new EstateReportingAPI.DataTransferObjects.Merchant {
            MerchantReportingId = m.MerchantReportingId,
            MerchantId = m.MerchantId,
            Name = m.Name,
            Reference = m.Reference,
            SettlementSchedule = m.SettlementSchedule,
            CreatedDateTime = m.CreatedDateTime,
            Balance = m.Balance,
            AddressId = m.AddressId,
            AddressLine1 = m.AddressLine1,
            AddressLine2 = m.AddressLine2,
            Town = m.Town,
            Region = m.Region,
            PostCode = m.PostCode,
            Country = m.Country,
            ContactId = m.ContactId,
            ContactName = m.ContactName,
            ContactEmail = m.ContactEmail,
            ContactPhone = m.ContactPhone
        }).OrderByDescending(m => m.CreatedDateTime).ToList());
    }

    public static async Task<Result<List<DataTransferObjects.Merchant>>> GetMerchants(IEstateContext estateContext,
                                                   [FromQuery, Description("Optional merchant name filter.")] String? name,
                                                   [FromQuery, Description("Optional merchant reference filter.")] String? reference,
                                                   [FromQuery, Description("Optional settlement schedule identifier filter.")] Int32? settlementSchedule,
                                                   [FromQuery, Description("Optional region filter.")] String? region,
                                                   [FromQuery, Description("Optional postal code filter.")] String? postCode,
                                                   IMediator mediator,
                                                   CancellationToken cancellationToken) {
        // Build the query options
        MerchantQueries.MerchantQueryOptions queryOptions = new(name, reference, settlementSchedule, region, postCode);
        MerchantQueries.GetMerchantsQuery query = new(estateContext.EstateId, queryOptions);
        Result<List<Merchant>> result = await mediator.Send(query, cancellationToken);

        return result.Map(r => r.Select(m => new EstateReportingAPI.DataTransferObjects.Merchant {
            MerchantReportingId = m.MerchantReportingId,
            MerchantId = m.MerchantId,
            Name = m.Name,
            Reference = m.Reference,
            SettlementSchedule = m.SettlementSchedule,
            CreatedDateTime = m.CreatedDateTime,
            Balance = m.Balance,
            AddressId = m.AddressId,
            AddressLine1 = m.AddressLine1,
            AddressLine2 = m.AddressLine2,
            Town = m.Town,
            Region = m.Region,
            PostCode = m.PostCode,
            Country = m.Country,
            ContactId = m.ContactId,
            ContactName = m.ContactName,
            ContactEmail = m.ContactEmail,
            ContactPhone = m.ContactPhone
        }).OrderByDescending(m => m.CreatedDateTime).ToList());
    }

    public static async Task<Result<DataTransferObjects.Merchant>> GetMerchant(IEstateContext estateContext,
                                                  [FromRoute, Description("Unique identifier of the merchant.")] Guid merchantId,
                                                  IMediator mediator,
                                                  CancellationToken cancellationToken) {
        // Build the query options
        MerchantQueries.GetMerchantQuery query = new(estateContext.EstateId, merchantId);
        Result<Merchant> result = await mediator.Send(query, cancellationToken);

        return result.Map(r => new EstateReportingAPI.DataTransferObjects.Merchant {
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

    public static async Task<Result<List<DataTransferObjects.MerchantOperator>>> GetMerchantOperators(IEstateContext estateContext,
                                                           [FromRoute, Description("Unique identifier of the merchant.")] Guid merchantId,
                                                           IMediator mediator,
                                                           CancellationToken cancellationToken) {
        MerchantQueries.GetMerchantOperatorsQuery query = new(estateContext.EstateId, merchantId);
        Result<List<MerchantOperator>> result = await mediator.Send(query, cancellationToken);

        return result.Map(r => r.Select(o => new DataTransferObjects.MerchantOperator() {
            IsDeleted = o.IsDeleted,
            MerchantId = o.MerchantId,
            MerchantNumber = o.MerchantNumber,
            OperatorId = o.OperatorId,
            OperatorName = o.OperatorName,
            TerminalNumber = o.TerminalNumber
        }).ToList());
    }



    public static async Task<Result<List<DataTransferObjects.MerchantContract>>> GetMerchantContracts(IEstateContext estateContext,
                                                           [FromRoute, Description("Unique identifier of the merchant.")] Guid merchantId,
                                                           IMediator mediator,
                                                           CancellationToken cancellationToken) {
        MerchantQueries.GetMerchantContractsQuery query = new(estateContext.EstateId, merchantId);
        Result<List<MerchantContract>> result = await mediator.Send(query, cancellationToken);

        return result.Map(r => r.Select(c => new DataTransferObjects.MerchantContract() {
            ContractId = c.ContractId,
            ContractName = c.ContractName,
            OperatorName = c.OperatorName,
            IsDeleted = c.IsDeleted,
            MerchantId = c.MerchantId,
            ContractProducts = c.ContractProducts.Select(p => new DataTransferObjects.MerchantContractProduct() {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                ContractId = c.ContractId,
                DisplayText = p.DisplayText,
                MerchantId = c.MerchantId
            }).ToList()
        }).ToList());
    }

    public static async Task<Result<List<DataTransferObjects.MerchantDevice>>> GetMerchantDevices(IEstateContext estateContext,
                                                         [FromRoute, Description("Unique identifier of the merchant.")] Guid merchantId,
                                                         IMediator mediator,
                                                         CancellationToken cancellationToken) {
        MerchantQueries.GetMerchantDevicesQuery query = new(estateContext.EstateId, merchantId);
        Result<List<MerchantDevice>> result = await mediator.Send(query, cancellationToken);

        return result.Map(r => r.Select(c => new DataTransferObjects.MerchantDevice() { IsDeleted = c.IsDeleted, MerchantId = c.MerchantId, DeviceId = c.DeviceId, DeviceIdentifier = c.DeviceIdentifier }).ToList());
    }

    public static async Task<Result<List<DataTransferObjects.MerchantOpeningHour>>> GetMerchantOpeningHours(IEstateContext estateContext,
                                                              [FromRoute, Description("Unique identifier of the merchant.")] Guid merchantId,
                                                              IMediator mediator,
                                                              CancellationToken cancellationToken) {
        MerchantQueries.GetMerchantOpeningHoursQuery query = new(estateContext.EstateId, merchantId);
        Result<List<MerchantOpeningHour>> result = await mediator.Send(query, cancellationToken);

        return result.Map(r => r.Select(c => new DataTransferObjects.MerchantOpeningHour() { MerchantId = c.MerchantId, DayOfWeek = c.DayOfWeek, OpeningTime = c.OpeningTime, ClosingTime = c.ClosingTime }).ToList());
    }

    public static async Task<Result<DataTransferObjects.MerchantScheduleResponse>> GetMerchantSchedule(IEstateContext estateContext,
                                                          [FromRoute, Description("Unique identifier of the merchant.")] Guid merchantId,
                                                          [FromRoute, Description("Schedule year (for example, 2026).") ] Int32 year,
                                                          IMediator mediator,
                                                          CancellationToken cancellationToken) {
        MerchantQueries.GetMerchantScheduleQuery query = new(estateContext.EstateId, merchantId, year);
        Result<MerchantScheduleResponse> result = await mediator.Send(query, cancellationToken);

        return result.Map(r => new DataTransferObjects.MerchantScheduleResponse() { Year = r.Year, Months = r.Months.Select(m => new DataTransferObjects.MerchantScheduleMonthResponse() { Month = m.Month, ClosedDays = m.ClosedDays }).ToList() });
    }
}
