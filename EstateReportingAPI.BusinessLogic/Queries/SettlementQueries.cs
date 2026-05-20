using System.Diagnostics.CodeAnalysis;
using EstateReportingAPI.Models;
using MediatR;
using SimpleResults;

namespace EstateReportingAPI.BusinessLogic.Queries;

[ExcludeFromCodeCoverage]
public record SettlementQueries {
    public record TodaysSettlementQuery(Guid EstateId, DateTime ComparisonDate) : IRequest<Result<TodaysSettlement>>;
}

[ExcludeFromCodeCoverage]
public record FileImportLogQueries
{
    public record GetFileImportLogListQuery(Guid EstateId, Guid? MerchantId, DateTime StartDate, DateTime EndDate) : IRequest<Result<List<FileImportLog>>>;
}