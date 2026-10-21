using Microsoft.Extensions.Configuration;
using EstateReportingAPI.DataTransferObjects;
using EstateReportingAPI.Handlers;
using Shared.Extensions;

namespace EstateReportingAPI.Endpoints;

public static class FileImportLogEndpoints
{
    private const string BaseRoute = "api/fileimportlogs";

    public static void MapFileImportLogEndpoints(this IEndpointRouteBuilder app, IConfiguration configuration)
    {
        RouteGroupBuilder group = app.MapGroup(BaseRoute).WithTags("File Import Logs");

        Boolean disableAuthorisation = configuration.GetValue("AppSettings:DisableAuthorisation", false);
        if (disableAuthorisation == false)
        {
            group = group.RequireAuthorization();
        }

        group.MapGet("/", FileImportHandler.GetFileImportLogList)
            .WithSummary("List file import logs")
            .WithDescription("Returns file import logs within the requested date range, optionally filtered by merchant.")
            .WithApiResponse<List<FileImportLog>>();
        
        group.MapGet("/{fileimportlogid}", FileImportHandler.GetFileImportLog)
            .WithSummary("Get a file import log")
            .WithDescription("Returns one file import log, including its uploaded files and line results. An optional merchant filter can be supplied.")
            .WithApiResponse<FileImportLog>();
    }
}

public static class FileProfileConfigurationEndpoints
{
    private const string BaseRoute = "api/fileprofiles";

    public static void MapFileProfileConfigurationEndpoints(this IEndpointRouteBuilder app, IConfiguration configuration)
    {
        RouteGroupBuilder group = app.MapGroup(BaseRoute).WithTags("File Profile Configurations");

        Boolean disableAuthorisation = configuration.GetValue("AppSettings:DisableAuthorisation", false);
        if (disableAuthorisation == false)
        {
            group = group.RequireAuthorization();
        }

        group.MapGet("/", FileProfileConfigurationHandler.GetFileProfileConfigurationList)
            .WithSummary("List file profile configurations")
            .WithDescription("Returns the file import profile configurations available to the current estate.")
            .WithApiResponse<List<FileProfileConfiguration>>();
    }
}
