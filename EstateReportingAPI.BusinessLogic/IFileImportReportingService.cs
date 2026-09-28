using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.Models;
using SimpleResults;

namespace EstateReportingAPI.BusinessLogic;

public interface IFileImportReportingService
{
    Task<Result<List<FileImportLog>>> GetFileImportLogList(FileImportLogQueries.GetFileImportLogListQuery request, CancellationToken cancellationToken);
    Task<Result<FileImportLog>> GetFileImportLog(FileImportLogQueries.GetFileImportLogQuery request, CancellationToken cancellationToken);
    Task<Result<List<FileProfileConfiguration>>> GetFileProfileConfigurationList(FileProfileConfigurationQueries.GetFileProfileConfigurationListQuery request, CancellationToken cancellationToken);
}
