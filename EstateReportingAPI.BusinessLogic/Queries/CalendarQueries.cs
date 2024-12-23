using System.Diagnostics.CodeAnalysis;
using EstateReportingAPI.Models;
using MediatR;
using SimpleResults;

namespace EstateReportingAPI.BusinessLogic.Queries;

[ExcludeFromCodeCoverage]
public record CalendarQueries {
    public record GetComparisonDatesQuery(Guid EstateId) : IRequest<Result<List<Calendar>>>;
    public record GetAllDatesQuery(Guid EstateId) : IRequest<Result<List<Calendar>>>;
    public record GetYearsQuery(Guid EstateId) : IRequest<Result<List<Int32>>>;
}