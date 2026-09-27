using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.Common;
using EstateReportingAPI.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Results.Web;
using SimpleResults;

namespace EstateReportingAPI.Handlers;

public static class OperatorHandler {
    public static async Task<IResult> GetOperators(IEstateContext estateContext,
                                                   IMediator mediator,
                                                   CancellationToken cancellationToken)
    {
        OperatorQueries.GetOperatorsQuery query = new(estateContext.EstateId);
        Result<List<Operator>> result = await mediator.Send(query, cancellationToken);
        return ResponseFactory.FromResult(result, r => r.Select(m => new DataTransferObjects.Operator
        {
            OperatorId = m.OperatorId,
            Name = m.Name,
            OperatorReportingId = m.OperatorReportingId,
            EstateReportingId = m.EstateReportingId,
            RequireCustomMerchantNumber = m.RequireCustomMerchantNumber,
            RequireCustomTerminalNumber = m.RequireCustomTerminalNumber
        }).ToList());
    }

    public static async Task<IResult> GetOperator(IEstateContext estateContext,
                                                  [FromRoute] Guid operatorId,
                                                  IMediator mediator,
                                                  CancellationToken cancellationToken)
    {
        OperatorQueries.GetOperatorQuery query = new(estateContext.EstateId, operatorId);
        Result<Operator> result = await mediator.Send(query, cancellationToken);
        return ResponseFactory.FromResult(result, r => new DataTransferObjects.Operator
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