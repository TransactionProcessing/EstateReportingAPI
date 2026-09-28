using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.Models;
using SimpleResults;

namespace EstateReportingAPI.BusinessLogic;

public interface ISettlementReportingService
{
    Task<Result<TodaysSettlement>> GetTodaysSettlement(SettlementQueries.TodaysSettlementQuery request, CancellationToken cancellationToken);
}
