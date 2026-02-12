using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.Models;
using MediatR;
using Shared.Results;
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
            return await this.Manager.GetCalendarDates(request, cancellationToken);
        }

        public async Task<Result<List<Calendar>>> Handle(CalendarQueries.GetComparisonDatesQuery request,
                                                         CancellationToken cancellationToken) {
            Result<List<Calendar>> result = await this.Manager.GetCalendarComparisonDates(request, cancellationToken);

            if (result.IsFailed)
                return ResultHelpers.CreateFailure(result);

            return Result.Success(result.Data);
        }

        public async Task<Result<List<Int32>>> Handle(CalendarQueries.GetYearsQuery request,
                                                      CancellationToken cancellationToken) {
            return await this.Manager.GetCalendarYears(request, cancellationToken);
        }
    }