using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.DataTrasferObjects;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Results.Web;

namespace EstateReportingAPI.Handlers;
/*
public static class DimensionsHandlers
{
    public static async Task<IResult> GetCalendarYears(
        [FromHeader] Guid estateId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new CalendarQueries.GetYearsQuery(estateId);
        var result = await mediator.Send(query, cancellationToken);

        return ResponseFactory.FromResult(result, r =>
            r.Select(y => new CalendarYear { Year = y }).ToList()
        );
    }

    public static async Task<IResult> GetCalendarDates(
        [FromHeader] Guid estateId,
        [FromRoute] int year,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new CalendarQueries.GetAllDatesQuery(estateId);
        var result = await mediator.Send(query, cancellationToken);

        return ResponseFactory.FromResult(result, r =>
            r.Select(d => new CalendarDate
            {
                Year = d.Year,
                Date = d.Date,
                DayOfWeek = d.DayOfWeek,
                DayOfWeekNumber = d.DayOfWeekNumber,
                DayOfWeekShort = d.DayOfWeekShort,
                MonthName = d.MonthNameLong,
                MonthNameShort = d.MonthNameShort,
                MonthNumber = d.MonthNumber,
                WeekNumber = d.WeekNumber ?? 0,
                WeekNumberString = d.WeekNumberString,
                YearWeekNumber = d.YearWeekNumber
            }).ToList()
        );
    }

    public static async Task<IResult> GetCalendarComparisonDates(
        [FromHeader] Guid estateId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new CalendarQueries.GetComparisonDatesQuery(estateId);
        var result = await mediator.Send(query, cancellationToken);

        return ResponseFactory.FromResult(result, r =>
        {
            var response = new List<ComparisonDate>
            {
                new() { Date = DateTime.Now.Date.AddDays(-1), Description = "Yesterday", OrderValue = 0 },
                new() { Date = DateTime.Now.Date.AddDays(-7), Description = "Last Week", OrderValue = 1 },
                new() { Date = DateTime.Now.Date.AddMonths(-1), Description = "Last Month", OrderValue = 2 }
            };

            int orderValue = 3;
            foreach (var d in r)
            {
                response.Add(new ComparisonDate
                {
                    Date = d.Date,
                    Description = d.Date.ToString("yyyy-MM-dd"),
                    OrderValue = orderValue++
                });
            }

            return response.OrderBy(d => d.OrderValue);
        });
    }

    public static async Task<IResult> GetMerchants(
        [FromHeader] Guid estateId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new MerchantQueries.GetMerchantsQuery(estateId);
        var result = await mediator.Send(query, cancellationToken);

        return ResponseFactory.FromResult(result, r =>
            r.Select(m => new Merchant
                {
                    MerchantReportingId = m.MerchantReportingId,
                    MerchantId = m.MerchantId,
                    EstateReportingId = m.EstateReportingId,
                    Name = m.Name,
                    LastSaleDateTime = m.LastSaleDateTime,
                    CreatedDateTime = m.CreatedDateTime,
                    LastSale = m.LastSale,
                    LastStatement = m.LastStatement,
                    PostCode = m.PostCode,
                    Reference = m.Reference,
                    Region = m.Region,
                    Town = m.Town
                })
                .OrderBy(m => m.Name)
                .ToList()
        );
    }

    public static async Task<IResult> GetOperators(
        [FromHeader] Guid estateId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new OperatorQueries.GetOperatorsQuery(estateId);
        var result = await mediator.Send(query, cancellationToken);

        return ResponseFactory.FromResult(result, r =>
            r.Select(o => new Operator
                {
                    EstateReportingId = o.EstateReportingId,
                    Name = o.Name,
                    OperatorId = o.OperatorId,
                    OperatorReportingId = o.OperatorReportingId
                })
                .OrderBy(o => o.Name)
                .ToList()
        );
    }

    public static async Task<IResult> GetResponseCodes(
        [FromHeader] Guid estateId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new ResponseCodeQueries.GetResponseCodesQuery(estateId);
        var result = await mediator.Send(query, cancellationToken);

        return ResponseFactory.FromResult(result, r =>
            r.Select(o => new ResponseCode
                {
                    Code = o.Code,
                    Description = o.Description
                })
                .OrderBy(r => r.Code)
                .ToList()
        );
    }
}*/