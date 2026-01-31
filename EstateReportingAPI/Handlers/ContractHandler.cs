using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Results.Web;
using SimpleResults;

namespace EstateReportingAPI.Handlers;

public static class ContractHandler {
    public static async Task<IResult> GetRecentContracts([FromHeader] Guid estateId,
                                                         IMediator mediator,
                                                         CancellationToken cancellationToken)
    {
        ContractQueries.GetRecentContractsQuery query = new ContractQueries.GetRecentContractsQuery(estateId);
        Result<List<Contract>> result = await mediator.Send(query, cancellationToken);
        return ResponseFactory.FromResult(result, r => r.Select(m => new DataTransferObjects.Contract
        {
            EstateReportingId = m.EstateReportingId,
            ContractId = m.ContractId,
            ContractReportingId = m.ContractReportingId,
            Description = m.Description,
            EstateId = m.EstateId,
            OperatorId = m.OperatorId,
            OperatorName = m.OperatorName,
            OperatorReportingId = m.OperatorReportingId
        }).ToList());
    }

    public static async Task<IResult> GetContracts([FromHeader] Guid estateId,
                                                   IMediator mediator,
                                                   CancellationToken cancellationToken)
    {
        ContractQueries.GetContractsQuery query = new ContractQueries.GetContractsQuery(estateId);
        Result<List<Contract>> result = await mediator.Send(query, cancellationToken);
        return ResponseFactory.FromResult(result, r => r.Select(m => new DataTransferObjects.Contract
        {
            EstateReportingId = m.EstateReportingId,
            ContractId = m.ContractId,
            ContractReportingId = m.ContractReportingId,
            Description = m.Description,
            EstateId = m.EstateId,
            OperatorId = m.OperatorId,
            OperatorName = m.OperatorName,
            OperatorReportingId = m.OperatorReportingId,
            Products = m.Products.Select(p => new DataTransferObjects.ContractProduct
            {
                ContractId = p.ContractId,
                DisplayText = p.DisplayText,
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                ProductType = p.ProductType,
                Value = p.Value,
                TransactionFees = p.TransactionFees.Select(f => new DataTransferObjects.ContractProductTransactionFee {
                    Description = f.Description,
                    Value = f.Value,
                    CalculationType = f.CalculationType,
                    FeeType = f.FeeType,
                    TransactionFeeId = f.TransactionFeeId
                }).ToList()
            }).ToList()
        }).ToList());
    }

    public static async Task<IResult> GetContract([FromHeader] Guid estateId,
                                                  [FromRoute] Guid contractId,
                                                  IMediator mediator,
                                                  CancellationToken cancellationToken)
    {
        ContractQueries.GetContractQuery query = new ContractQueries.GetContractQuery(estateId, contractId);
        Result<Contract> result = await mediator.Send(query, cancellationToken);
        return ResponseFactory.FromResult(result, r => new DataTransferObjects.Contract
        {
            EstateReportingId = r.EstateReportingId,
            ContractId = r.ContractId,
            ContractReportingId = r.ContractReportingId,
            Description = r.Description,
            EstateId = r.EstateId,
            OperatorId = r.OperatorId,
            OperatorName = r.OperatorName,
            OperatorReportingId = r.OperatorReportingId,
            Products = r.Products.Select(p => new DataTransferObjects.ContractProduct
            {
                ContractId = p.ContractId,
                DisplayText = p.DisplayText,
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                ProductType = p.ProductType,
                Value = p.Value,
                TransactionFees = p.TransactionFees.Select(f => new DataTransferObjects.ContractProductTransactionFee
                {
                    Description = f.Description,
                    Value = f.Value,
                    CalculationType = f.CalculationType,
                    FeeType = f.FeeType,
                    TransactionFeeId = f.TransactionFeeId
                }).ToList()
            }).ToList()
        });
    }
}