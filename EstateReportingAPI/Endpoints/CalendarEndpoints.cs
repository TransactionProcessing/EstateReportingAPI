using EstateReportingAPI.DataTrasferObjects;
using EstateReportingAPI.Handlers;
using Shared.Extensions;
using Shared.General;

namespace EstateReportingAPI.Endpoints;

public static class CalendarEndpoints
{
    private const string BaseRoute = "api/calendars";

    public static void MapCalendarEndpoints(this IEndpointRouteBuilder app) {
        

        RouteGroupBuilder group = app.MapGroup(BaseRoute)
            .WithTags("Calendars");

        Boolean disableAuthorisation = ConfigurationReader.GetValueOrDefault<Boolean>("AppSettings", "DisableAuthorisation", false);
        if (disableAuthorisation == false) {
            group = group.RequireAuthorization();
        }
        
        group.MapGet("/comparisondates", CalenderHandler.GetCalendarComparisonDates)
            .WithStandardProduces<List<ComparisonDate>>();
    }
}