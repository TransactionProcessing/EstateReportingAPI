using EstateReportingAPI.DataTrasferObjects;
using EstateReportingAPI.Handlers;
using Shared.Extensions;

namespace EstateReportingAPI.Endpoints;

public static class DimensionsEndpoints
{
    private const string BaseRoute = "api/dimensions";

    public static void MapDimensionsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(BaseRoute)
            .RequireAuthorization()
            .WithTags("Dimensions");

        group.MapGet("calendar/years", DimensionsHandlers.GetCalendarYears)
            .WithStandardProduces<List<CalendarYear>>();

        group.MapGet("calendar/{year}/dates", DimensionsHandlers.GetCalendarDates)
            .WithStandardProduces<List<CalendarDate>>();
        group.MapGet("calendar/comparisondates", DimensionsHandlers.GetCalendarComparisonDates)
            .WithStandardProduces<List<ComparisonDate>>();
        group.MapGet("merchants", DimensionsHandlers.GetMerchants).WithStandardProduces<List<Merchant>>();
        group.MapGet("operators", DimensionsHandlers.GetOperators).WithStandardProduces<List<Operator>>();
        group.MapGet("responsecodes", DimensionsHandlers.GetResponseCodes).WithStandardProduces<List<ResponseCode>>();
    }
}