using Microsoft.Extensions.Configuration;
using EstateReportingAPI.DataTrasferObjects;
using EstateReportingAPI.Handlers;
using Shared.Extensions;

namespace EstateReportingAPI.Endpoints;

public static class CalendarEndpoints
{
    private const string BaseRoute = "api/calendars";

    public static void MapCalendarEndpoints(this IEndpointRouteBuilder app, IConfiguration configuration) {
        

        RouteGroupBuilder group = app.MapGroup(BaseRoute)
            .WithTags("Calendars");

        Boolean disableAuthorisation = configuration.GetValue("AppSettings:DisableAuthorisation", false);
        if (disableAuthorisation == false) {
            group = group.RequireAuthorization();
        }
        
        group.MapGet("/comparisondates", CalenderHandler.GetCalendarComparisonDates)
            .WithSummary("List available comparison dates")
            .WithDescription("Returns yesterday, the equivalent dates from the previous week and month, and calendar dates available for this estate.")
            .WithApiResponse<List<ComparisonDate>>();
    }
}
