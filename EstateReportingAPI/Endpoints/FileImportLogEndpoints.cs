using EstateReportingAPI.DataTransferObjects;
using EstateReportingAPI.Handlers;
using Shared.Extensions;
using Shared.General;

namespace EstateReportingAPI.Endpoints;

public static class FileImportLogEndpoints
{
    private const string BaseRoute = "api/fileimportlogs";

    public static void MapFileImportLogEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup(BaseRoute).WithTags("File Import Logs");

        Boolean disableAuthorisation = ConfigurationReader.GetValueOrDefault<Boolean>("AppSettings", "DisableAuthorisation", false);
        if (disableAuthorisation == false)
        {
            group = group.RequireAuthorization();
        }

        group.MapGet("/", FileImportHandler.GetFileImportLogList).WithStandardProduces<List<FileImportLog>>();
        
        group.MapGet("/{fileimportlogid}", FileImportHandler.GetFileImportLog).WithStandardProduces<FileImportLog>();
    }
}