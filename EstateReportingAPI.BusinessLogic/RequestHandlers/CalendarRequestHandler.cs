using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.Models;
using MediatR;
using SimpleResults;

namespace EstateReportingAPI.BusinessLogic.RequestHandlers;
public class CalendarRequestHandler : IRequestHandler<CalendarQueries.GetAllDatesQuery, Result<List<Calendar>>>,
        IRequestHandler<CalendarQueries.GetComparisonDatesQuery, Result<List<Calendar>>>,
        IRequestHandler<CalendarQueries.GetYearsQuery, Result<List<Int32>>> {
        private readonly IReportingManager Manager;
        public CalendarRequestHandler(IReportingManager manager) {
            this.Manager = manager;
        }
        public async Task<Result<List<Calendar>>> Handle(CalendarQueries.GetAllDatesQuery request,
                                                         CancellationToken cancellationToken) {
            List<Calendar> result = await this.Manager.GetCalendarDates(request, cancellationToken);

            if (result.Any() == false) {
                return Result.NotFound("No calendar dates found");
            }

            return Result.Success(result);

        }

        public async Task<Result<List<Calendar>>> Handle(CalendarQueries.GetComparisonDatesQuery request,
                                                         CancellationToken cancellationToken) {
            List<Calendar> result = await this.Manager.GetCalendarComparisonDates(request, cancellationToken);
            if (result.Any() == false)
            {
                return Result.NotFound("No calendar comparison dates found");
            }

            return Result.Success(result);
        }

        public async Task<Result<List<Int32>>> Handle(CalendarQueries.GetYearsQuery request,
                                                      CancellationToken cancellationToken) {
            List<Int32> result = await this.Manager.GetCalendarYears(request, cancellationToken);
            if (result.Any() == false)
            {
                return Result.NotFound("No calendar years found");
            }

            return Result.Success(result);
        }
    }