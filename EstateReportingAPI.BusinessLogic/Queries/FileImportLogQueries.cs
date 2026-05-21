using System.Diagnostics.CodeAnalysis;
using EstateReportingAPI.Models;
using MediatR;
using SimpleResults;

namespace EstateReportingAPI.BusinessLogic.Queries;

[ExcludeFromCodeCoverage]
public record FileImportLogQueries
{
    public record GetFileImportLogListQuery(Guid EstateId, Guid? MerchantId, DateTime StartDate, DateTime EndDate) : IRequest<Result<List<FileImportLog>>>;
    public record GetFileImportLogQuery(Guid EstateId, Guid? MerchantId, Guid FileImportLogId) : IRequest<Result<FileImportLog>>;
}

[ExcludeFromCodeCoverage]
public record FileProfileConfigurationQueries
{
    public record GetFileProfileConfigurationListQuery(Guid EstateId) : IRequest<Result<List<FileProfileConfiguration>>>;
}