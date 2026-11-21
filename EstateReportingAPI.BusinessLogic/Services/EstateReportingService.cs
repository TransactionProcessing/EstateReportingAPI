using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.DynamicLinq;
using TransactionProcessing.UnionResults;
using TransactionProcessor.Database.Contexts;
using TransactionProcessing.Shared.EntityFramework;


namespace EstateReportingAPI.BusinessLogic.Services;

public interface IEstateReportingService
{
    Task<Result<List<Calendar>>> GetCalendarComparisonDates(CalendarQueries.GetComparisonDatesQuery request, CancellationToken cancellationToken);
    Task<Result<List<Calendar>>> GetCalendarDates(CalendarQueries.GetAllDatesQuery request, CancellationToken cancellationToken);
    Task<Result<List<int>>> GetCalendarYears(CalendarQueries.GetYearsQuery request, CancellationToken cancellationToken);
    Task<Result<List<EstateOperator>>> GetEstateOperators(EstateQueries.GetEstateOperatorsQuery request, CancellationToken cancellationToken);
    Task<Result<Estate>> GetEstate(EstateQueries.GetEstateQuery request, CancellationToken cancellationToken);
}
public sealed class EstateReportingService : IEstateReportingService
{
    private readonly IDbContextResolver<EstateManagementContext> Resolver;
    private readonly ReportingDatePolicy DatePolicy;
    private const string EstateManagementDatabaseName = "TransactionProcessorReadModel";

    public EstateReportingService(IDbContextResolver<EstateManagementContext> resolver, ReportingDatePolicy? datePolicy = null)
    {
        Resolver = resolver;
        DatePolicy = datePolicy ?? new ReportingDatePolicy(TimeProvider.System);
    }

    public async Task<Result<List<Calendar>>> GetCalendarComparisonDates(CalendarQueries.GetComparisonDatesQuery request,
                                                                         CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        DateOnly today = DatePolicy.Today;
        DateOnly startDate = today.AddYears(-1);
        DateOnly endDate = today.AddDays(-1); // yesterday

        var result = await ReportingQueryExecutor.ToListAsync(context.Calendar.Where(c => c.Date >= startDate && c.Date <= endDate).OrderByDescending(c => c.Date), cancellationToken, "Error retrieving calendar comparison dates");

        if (!result.TryGetSuccess(out var resultValue))
            return new ErrorList(result.GetErrors());

        var entities = resultValue;

        if (entities.Any() == false)
            return new Error(ErrorType.NotFound, "No calendar dates found");

        List<Calendar> response = new();
        foreach (TransactionProcessor.Database.Entities.Calendar calendar in entities)
            response.Add(new Calendar {
                Date = calendar.Date,
                DayOfWeek = calendar.DayOfWeek,
                Year = calendar.Year,
                DayOfWeekNumber = calendar.DayOfWeekNumber,
                DayOfWeekShort = calendar.DayOfWeekShort,
                MonthNameLong = calendar.MonthNameLong,
                MonthNameShort = calendar.MonthNameShort,
                MonthNumber = calendar.MonthNumber,
                WeekNumber = calendar.WeekNumber,
                WeekNumberString = calendar.WeekNumberString,
                YearWeekNumber = calendar.YearWeekNumber
            });

        return Results.Ok(response);
    }

    public async Task<Result<List<Calendar>>> GetCalendarDates(CalendarQueries.GetAllDatesQuery request,
                                                               CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;


        var result = await ReportingQueryExecutor.ToListAsync(context.Calendar.Where(c => c.Date <= DatePolicy.Today), cancellationToken, "Error retrieving calendar dates");

        if (!result.TryGetSuccess(out var resultValue))
            return new ErrorList(result.GetErrors());

        var entities = resultValue;

        List<Calendar> response = new();
        foreach (TransactionProcessor.Database.Entities.Calendar calendar in entities)
            response.Add(new Calendar {
                Date = calendar.Date,
                DayOfWeek = calendar.DayOfWeek,
                Year = calendar.Year,
                DayOfWeekNumber = calendar.DayOfWeekNumber,
                DayOfWeekShort = calendar.DayOfWeekShort,
                MonthNameLong = calendar.MonthNameLong,
                MonthNameShort = calendar.MonthNameShort,
                MonthNumber = calendar.MonthNumber,
                WeekNumber = calendar.WeekNumber,
                WeekNumberString = calendar.WeekNumberString,
                YearWeekNumber = calendar.YearWeekNumber
            });

        return Results.Ok(response);
    }

    public async Task<Result<List<Int32>>> GetCalendarYears(CalendarQueries.GetYearsQuery request,
                                                            CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        var result = await ReportingQueryExecutor.ToListAsync(context.Calendar.Where(c => c.Date <= DatePolicy.Today).GroupBy(c => c.Year).Select(y => y.Key), cancellationToken, "Error retrieving calendar years");

        if (!result.TryGetSuccess(out var resultValue))
            return new ErrorList(result.GetErrors());

        return result;
    }

    public async Task<Result<List<EstateOperator>>> GetEstateOperators(EstateQueries.GetEstateOperatorsQuery request,
                                                                       CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;



        var operatorQuery = context.EstateOperators.Join(context.Operators, eo => new { eo.OperatorId, eo.EstateId }, op => new { op.OperatorId, op.EstateId }, (eo,
                                                                                                                                                                 op) => new { EstateOperator = eo, Operator = op }).Where(e => e.EstateOperator.EstateId == request.EstateId && (e.EstateOperator.IsDeleted ?? false) == false);

        var operatorQueryResult = await ReportingQueryExecutor.ToListAsync(operatorQuery, cancellationToken, "Error retrieving estate operators");
        if (!operatorQueryResult.TryGetSuccess(out var operatorQueryResultValue))
            return new ErrorList(operatorQueryResult.GetErrors());

        var operatorEntities = operatorQueryResultValue;
        List<EstateOperator> operators = new();

        foreach (var operatorEntity in operatorEntities) {
            operators.Add(new EstateOperator() { Name = operatorEntity.Operator.Name, OperatorId = operatorEntity.EstateOperator.OperatorId });
        }

        return Results.Ok(operators);
    }

    public async Task<Result<Estate>> GetEstate(EstateQueries.GetEstateQuery request,
                                                CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        var estateQueryResult = await ReportingQueryExecutor.SingleOrDefaultAsync(context.Estates.Where(e => e.EstateId == request.EstateId), cancellationToken, "Error retrieving estate");

        if (!estateQueryResult.TryGetSuccess(out var estateQueryResultValue))
            return new ErrorList(estateQueryResult.GetErrors());
        var estate = estateQueryResultValue;

        // Operators
        var operatorsListQueryResult = await ReportingQueryExecutor.ToListAsync(context.Operators.Where(e => e.EstateId == request.EstateId), cancellationToken, "Error retrieving estate operators");
        if (!operatorsListQueryResult.TryGetSuccess(out var operatorsListQueryResultValue))
            return new ErrorList(operatorsListQueryResult.GetErrors());
        var operators = operatorsListQueryResultValue;

        // Users
        var usersListQueryResult = await ReportingQueryExecutor.ToListAsync(context.EstateSecurityUsers.Where(e => e.EstateId == request.EstateId), cancellationToken, "Error retrieving estate users");
        if (!usersListQueryResult.TryGetSuccess(out var usersListQueryResultValue))
            return new ErrorList(usersListQueryResult.GetErrors());
        var users = usersListQueryResultValue;

        // Merchants
        var merchantsListQueryResult = await ReportingQueryExecutor.ToListAsync(context.Merchants.Where(e => e.EstateId == request.EstateId), cancellationToken, "Error retrieving merchants");
        if (!merchantsListQueryResult.TryGetSuccess(out var merchantsListQueryResultValue))
            return new ErrorList(merchantsListQueryResult.GetErrors());
        var merchants = merchantsListQueryResultValue;

        // Contracts
        var contractsListQueryResult = await ReportingQueryExecutor.ToListAsync(context.Contracts.Where(e => e.EstateId == request.EstateId), cancellationToken, "Error retrieving contracts");
        if (!contractsListQueryResult.TryGetSuccess(out var contractsListQueryResultValue))
            return new ErrorList(contractsListQueryResult.GetErrors());
        var contracts = contractsListQueryResultValue;

        Estate result = new() {
            EstateId = estate.EstateId,
            EstateName = estate.Name,
            Reference = estate.Reference,
            Operators = operators.Select(o => new Models.EstateOperator { OperatorId = o.OperatorId, Name = o.Name, }).ToList(),
            Users = users.Select(u => new Models.EstateUser { UserId = u.SecurityUserId, EmailAddress = u.EmailAddress, CreatedDateTime = u.CreatedDateTime }).ToList(),
            Merchants = merchants.Select(m => new Models.EstateMerchant { MerchantId = m.MerchantId, Name = m.Name, Reference = m.Reference }).ToList(),
            Contracts = contracts.Select(c => new Models.EstateContract { ContractId = c.ContractId, Name = c.Description, }).ToList()
        };

        return Results.Ok(result);
}
}
