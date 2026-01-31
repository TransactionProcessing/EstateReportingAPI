using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.DataTrasferObjects;
using EstateReportingAPI.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Results.Web;
using SimpleResults;

namespace EstateReportingAPI.Handlers;

public static class CalenderHandler {
    public static async Task<IResult> GetCalendarComparisonDates([FromHeader] Guid estateId,
                                                                 IMediator mediator,
                                                                 CancellationToken cancellationToken) {
        CalendarQueries.GetComparisonDatesQuery query = new CalendarQueries.GetComparisonDatesQuery(estateId);
        Result<List<Calendar>> result = await mediator.Send(query, cancellationToken);

        return ResponseFactory.FromResult(result, r => {
            List<ComparisonDate> response = new() { new() { Date = DateTime.Now.Date.AddDays(-1), Description = "Yesterday", OrderValue = 0 }, new() { Date = DateTime.Now.Date.AddDays(-7), Description = "Last Week", OrderValue = 1 }, new() { Date = DateTime.Now.Date.AddMonths(-1), Description = "Last Month", OrderValue = 2 } };

            int orderValue = 3;
            foreach (Calendar d in r) {
                response.Add(new ComparisonDate { Date = d.Date, Description = d.Date.ToString("yyyy-MM-dd"), OrderValue = orderValue++ });
            }

            return response.OrderBy(d => d.OrderValue);
        });
    }
}