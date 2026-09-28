using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.Models;
using MediatR;
using SimpleResults;

namespace EstateReportingAPI.BusinessLogic.RequestHandlers;

public class FileImportLogRequestHandler : IRequestHandler<FileImportLogQueries.GetFileImportLogListQuery, Result<List<FileImportLog>>>,
    IRequestHandler<FileImportLogQueries.GetFileImportLogQuery, Result<FileImportLog>> {
    private readonly IFileImportReportingService Service;
    public FileImportLogRequestHandler(IFileImportReportingService service)
    {
        this.Service = service;
    }

    public async Task<Result<List<FileImportLog>>> Handle(FileImportLogQueries.GetFileImportLogListQuery request,
                                                          CancellationToken cancellationToken) {
        return await this.Service.GetFileImportLogList(request, cancellationToken);
    }

    public async Task<Result<FileImportLog>> Handle(FileImportLogQueries.GetFileImportLogQuery request,
                                                          CancellationToken cancellationToken)
    {
        return await this.Service.GetFileImportLog(request, cancellationToken);
    }
}

public class FileProfileConfigurationRequestHandler : IRequestHandler<FileProfileConfigurationQueries.GetFileProfileConfigurationListQuery, Result<List<FileProfileConfiguration>>>
    
{
    private readonly IFileImportReportingService Service;
    public FileProfileConfigurationRequestHandler(IFileImportReportingService service)
    {
        this.Service = service;
    }

    public async Task<Result<List<FileProfileConfiguration>>> Handle(FileProfileConfigurationQueries.GetFileProfileConfigurationListQuery request,
                                                          CancellationToken cancellationToken)
    {
        return await this.Service.GetFileProfileConfigurationList(request, cancellationToken);
    }
}
