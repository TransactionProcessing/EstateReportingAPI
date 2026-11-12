using EstateReportingAPI.BusinessLogic.Queries;
using System.ComponentModel;
using EstateReportingAPI.Common;
using EstateReportingAPI.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TransactionProcessing.UnionResults.AspNetCore;
using TransactionProcessing.UnionResults;

namespace EstateReportingAPI.Handlers;

public static class OperatorHandler {
    public static async Task<Result<List<DataTransferObjects.Operator>>> GetOperators(IEstateContext estateContext,
                                                   IMediator mediator,
                                                   CancellationToken cancellationToken)
    {
        OperatorQueries.GetOperatorsQuery query = new(estateContext.EstateId);
        Result<List<Operator>> result = await mediator.Send(query, cancellationToken);
        return result.Map(r => r.Select(m => new DataTransferObjects.Operator
        {
            OperatorId = m.OperatorId,
            Name = m.Name,
            OperatorReportingId = m.OperatorReportingId,
            EstateReportingId = m.EstateReportingId,
            RequireCustomMerchantNumber = m.RequireCustomMerchantNumber,
            RequireCustomTerminalNumber = m.RequireCustomTerminalNumber
        }).ToList());
    }

    public static async Task<Result<DataTransferObjects.Operator>> GetOperator(IEstateContext estateContext,
                                                  [FromRoute, Description("Unique identifier of the operator.")] Guid operatorId,
                                                  IMediator mediator,
                                                  CancellationToken cancellationToken)
    {
        OperatorQueries.GetOperatorQuery query = new(estateContext.EstateId, operatorId);
        Result<Operator> result = await mediator.Send(query, cancellationToken);
        return result.Map(r => new DataTransferObjects.Operator
        {
            OperatorId = r.OperatorId,
            Name = r.Name,
            OperatorReportingId = r.OperatorReportingId,
            EstateReportingId = r.EstateReportingId,
            RequireCustomMerchantNumber = r.RequireCustomMerchantNumber,
            RequireCustomTerminalNumber = r.RequireCustomTerminalNumber
        });
    }
}
