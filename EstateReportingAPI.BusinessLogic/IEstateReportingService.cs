using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.Models;
using SimpleResults;

namespace EstateReportingAPI.BusinessLogic;

public interface IEstateReportingService
{
    Task<Result<List<Calendar>>> GetCalendarComparisonDates(CalendarQueries.GetComparisonDatesQuery request, CancellationToken cancellationToken);
    Task<Result<List<Calendar>>> GetCalendarDates(CalendarQueries.GetAllDatesQuery request, CancellationToken cancellationToken);
    Task<Result<List<int>>> GetCalendarYears(CalendarQueries.GetYearsQuery request, CancellationToken cancellationToken);
    Task<Result<List<EstateOperator>>> GetEstateOperators(EstateQueries.GetEstateOperatorsQuery request, CancellationToken cancellationToken);
    Task<Result<Estate>> GetEstate(EstateQueries.GetEstateQuery request, CancellationToken cancellationToken);
}
