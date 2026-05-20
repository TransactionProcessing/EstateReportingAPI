using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.DataTransferObjects;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Results.Web;

namespace EstateReportingAPI.Handlers;

public static class SettlementHandler {
    public static async Task<IResult> TodaysSettlements([FromHeader] Guid estateId,
                                                        [FromQuery] DateTime comparisonDate,
                                                        IMediator mediator,
                                                        CancellationToken cancellationToken) {
        var query = new SettlementQueries.TodaysSettlementQuery(estateId, comparisonDate);
        var result = await mediator.Send(query, cancellationToken);

        return ResponseFactory.FromResult(result, r => new TodaysSettlement() {
            ComparisonPendingSettlementCount = r.ComparisonPendingSettlementCount,
            ComparisonPendingSettlementValue = r.ComparisonPendingSettlementValue,
            ComparisonSettlementCount = r.ComparisonSettlementCount,
            ComparisonSettlementValue = r.ComparisonSettlementValue,
            TodaysPendingSettlementCount = r.TodaysPendingSettlementCount,
            TodaysPendingSettlementValue = r.TodaysPendingSettlementValue,
            TodaysSettlementCount = r.TodaysSettlementCount,
            TodaysSettlementValue = r.TodaysSettlementValue
        });
    }
}