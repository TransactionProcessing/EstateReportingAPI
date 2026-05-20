using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.Models;
using MediatR;
using SimpleResults;

namespace EstateReportingAPI.BusinessLogic.RequestHandlers;

public class FileImportLogRequestHandler : IRequestHandler<FileImportLogQueries.GetFileImportLogListQuery, Result<List<FileImportLog>>>,
    IRequestHandler<FileImportLogQueries.GetFileImportLogQuery, Result<FileImportLog>> {
    private readonly IReportingManager Manager;
    public FileImportLogRequestHandler(IReportingManager manager)
    {
        this.Manager = manager;
    }

    public async Task<Result<List<FileImportLog>>> Handle(FileImportLogQueries.GetFileImportLogListQuery request,
                                                          CancellationToken cancellationToken) {
        return await this.Manager.GetFileImportLogList(request, cancellationToken);
    }

    public async Task<Result<FileImportLog>> Handle(FileImportLogQueries.GetFileImportLogQuery request,
                                                          CancellationToken cancellationToken)
    {
        return await this.Manager.GetFileImportLog(request, cancellationToken);
    }
}