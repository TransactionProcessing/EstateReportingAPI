using EstateReportingAPI.BusinessLogic.Queries;
using System.ComponentModel;
using EstateReportingAPI.Common;
using EstateReportingAPI.DataTransferObjects;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TransactionProcessing.UnionResults;
using TransactionProcessing.UnionResults.AspNetCore;

namespace EstateReportingAPI.Handlers;

public static class SettlementHandler {
    public static async Task<Result<DataTransferObjects.TodaysSettlement>> TodaysSettlements(IEstateContext estateContext,
                                                        [FromQuery, Description("Date used to compare today's settlements with the corresponding day (YYYY-MM-DD).") ] DateOnly comparisonDate,
                                                        IMediator mediator,
                                                        CancellationToken cancellationToken) {
        var query = new SettlementQueries.TodaysSettlementQuery(estateContext.EstateId, comparisonDate);
        var result = await mediator.Send(query, cancellationToken);

        return result.Map(r => new TodaysSettlement() {
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
