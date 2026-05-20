using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.Models;
using MediatR;
using SimpleResults;

namespace EstateReportingAPI.BusinessLogic.RequestHandlers;

public class SettlementRequestHandler : IRequestHandler<SettlementQueries.TodaysSettlementQuery, Result<TodaysSettlement>>
{
    private readonly IReportingManager Manager;
    public SettlementRequestHandler(IReportingManager manager) {
        this.Manager = manager;
    }
    public async Task<Result<TodaysSettlement>> Handle(SettlementQueries.TodaysSettlementQuery request,
                                                       CancellationToken cancellationToken) {
        return await this.Manager.GetTodaysSettlement(request, cancellationToken);
    }
}

public class FileImportLogRequestHandler : IRequestHandler<FileImportLogQueries.GetFileImportLogListQuery, Result<List<FileImportLog>>>
{
    private readonly IReportingManager Manager;
    public FileImportLogRequestHandler(IReportingManager manager)
    {
        this.Manager = manager;
    }

    public async Task<Result<List<FileImportLog>>> Handle(FileImportLogQueries.GetFileImportLogListQuery request,
                                                          CancellationToken cancellationToken) {
        return await this.Manager.GetFileImportLogList(request, cancellationToken);
    }
}