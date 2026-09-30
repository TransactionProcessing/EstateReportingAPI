using EstateReportingAPI.BusinessLogic;
using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.Common;
using EstateReportingAPI.DataTrasferObjects;
using EstateReportingAPI.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Results.Web;
using SimpleResults;

namespace EstateReportingAPI.Handlers;

public static class CalenderHandler
{
    public static async Task<IResult> GetCalendarComparisonDates(IEstateContext estateContext,
                                                                  ReportingDatePolicy datePolicy,
                                                                  IMediator mediator,
                                                                  CancellationToken cancellationToken)
    {
        CalendarQueries.GetComparisonDatesQuery query = new(estateContext.EstateId);
        Result<List<Calendar>> result = await mediator.Send(query, cancellationToken);

        return ResponseFactory.FromResult(result, r =>
        {
            DateTime today = datePolicy.Today.ToDateTime(TimeOnly.MinValue);
            List<ComparisonDate> response = new()
            {
                new() { Date = DateOnly.FromDateTime(today.AddDays(-1)), Description = "Yesterday", OrderValue = 0 },
                new() { Date = DateOnly.FromDateTime(today.AddDays(-7)), Description = "Last Week", OrderValue = 1 },
                new() { Date = DateOnly.FromDateTime(today.AddMonths(-1)), Description = "Last Month", OrderValue = 2 }
            };

            int orderValue = 3;
            foreach (Calendar d in r)
                response.Add(new ComparisonDate { Date = DateOnly.FromDateTime(d.Date), Description = d.Date.ToString("yyyy-MM-dd"), OrderValue = orderValue++ });

            return response.OrderBy(d => d.OrderValue);
        });
    }
}
