using System.Diagnostics.CodeAnalysis;
using EstateReportingAPI.Models;
using MediatR;
using TransactionProcessing.UnionResults;

namespace EstateReportingAPI.BusinessLogic.Queries;

[ExcludeFromCodeCoverage]
public record FileImportLogQueries
{
    public record GetFileImportLogListQuery(Guid EstateId, Guid? MerchantId, DateOnly StartDate, DateOnly EndDate) : IRequest<Result<List<FileImportLog>>>;
    public record GetFileImportLogQuery(Guid EstateId, Guid? MerchantId, Guid FileImportLogId) : IRequest<Result<FileImportLog>>;
}

[ExcludeFromCodeCoverage]
public record FileProfileConfigurationQueries
{
    public record GetFileProfileConfigurationListQuery(Guid EstateId) : IRequest<Result<List<FileProfileConfiguration>>>;
}