using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.DynamicLinq;
using SimpleResults;
using TransactionProcessor.Database.Contexts;
using Shared.EntityFramework;
using Shared.Results;

namespace EstateReportingAPI.BusinessLogic;

public sealed class EstateReportingService : IEstateReportingService
{
    private readonly IDbContextResolver<EstateManagementContext> Resolver;
    private const string EstateManagementDatabaseName = "TransactionProcessorReadModel";

    public EstateReportingService(IDbContextResolver<EstateManagementContext> resolver)
    {
        Resolver = resolver;
    }

    public async Task<Result<List<Calendar>>> GetCalendarComparisonDates(CalendarQueries.GetComparisonDatesQuery request,
                                                                         CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        DateTime today = DateTime.Today;
        DateTime startDate = today.AddYears(-1);
        DateTime endDate = today.AddDays(-1); // yesterday

        var result = await ReportingQueryExecutor.ToListAsync(context.Calendar.Where(c => c.Date >= startDate && c.Date < endDate).OrderByDescending(c => c.Date), cancellationToken, "Error retrieving calendar comparison dates");

        if (result.IsFailed)
            return ResultHelpers.CreateFailure(result);

        var entities = result.Data;

        if (entities.Any() == false)
            return Result.NotFound("No calendar dates found");

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

        return Result.Success(response);
    }

    public async Task<Result<List<Calendar>>> GetCalendarDates(CalendarQueries.GetAllDatesQuery request,
                                                               CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;


        var result = await ReportingQueryExecutor.ToListAsync(context.Calendar.Where(c => c.Date <= DateTime.Now.Date), cancellationToken, "Error retrieving calendar dates");

        if (result.IsFailed)
            return ResultHelpers.CreateFailure(result);

        var entities = result.Data;

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

        return Result.Success(response);
    }

    public async Task<Result<List<Int32>>> GetCalendarYears(CalendarQueries.GetYearsQuery request,
                                                            CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        var result = await ReportingQueryExecutor.ToListAsync(context.Calendar.Where(c => c.Date <= DateTime.Now.Date).GroupBy(c => c.Year).Select(y => y.Key), cancellationToken, "Error retrieving calendar years");

        if (result.IsFailed)
            return ResultHelpers.CreateFailure(result);

        return result;
    }

    public async Task<Result<List<EstateOperator>>> GetEstateOperators(EstateQueries.GetEstateOperatorsQuery request,
                                                                       CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;



        var operatorQuery = context.EstateOperators.Join(context.Operators, eo => new { eo.OperatorId, eo.EstateId }, op => new { op.OperatorId, op.EstateId }, (eo,
                                                                                                                                                                 op) => new { EstateOperator = eo, Operator = op }).Where(e => e.EstateOperator.EstateId == request.EstateId && (e.EstateOperator.IsDeleted ?? false) == false);

        var operatorQueryResult = await ReportingQueryExecutor.ToListAsync(operatorQuery, cancellationToken, "Error retrieving estate operators");
        if (operatorQueryResult.IsFailed)
            return ResultHelpers.CreateFailure(operatorQueryResult);

        var operatorEntities = operatorQueryResult.Data;
        List<EstateOperator> operators = new();

        foreach (var operatorEntity in operatorEntities) {
            operators.Add(new EstateOperator() { Name = operatorEntity.Operator.Name, OperatorId = operatorEntity.EstateOperator.OperatorId });
        }

        return Result.Success(operators);
    }

    public async Task<Result<Estate>> GetEstate(EstateQueries.GetEstateQuery request,
                                                CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        var estateQueryResult = await ReportingQueryExecutor.SingleOrDefaultAsync(context.Estates.Where(e => e.EstateId == request.EstateId), cancellationToken, "Error retrieving estate");

        if (estateQueryResult.IsFailed)
            return ResultHelpers.CreateFailure(estateQueryResult);
        var estate = estateQueryResult.Data;

        // Operators
        var operatorsListQueryResult = await ReportingQueryExecutor.ToListAsync(context.Operators.Where(e => e.EstateId == request.EstateId), cancellationToken, "Error retrieving estate operators");
        if (operatorsListQueryResult.IsFailed)
            return ResultHelpers.CreateFailure(operatorsListQueryResult);
        var operators = operatorsListQueryResult.Data;

        // Users
        var usersListQueryResult = await ReportingQueryExecutor.ToListAsync(context.EstateSecurityUsers.Where(e => e.EstateId == request.EstateId), cancellationToken, "Error retrieving estate users");
        if (usersListQueryResult.IsFailed)
            return ResultHelpers.CreateFailure(usersListQueryResult);
        var users = usersListQueryResult.Data;

        // Merchants
        var merchantsListQueryResult = await ReportingQueryExecutor.ToListAsync(context.Merchants.Where(e => e.EstateId == request.EstateId), cancellationToken, "Error retrieving merchants");
        if (merchantsListQueryResult.IsFailed)
            return ResultHelpers.CreateFailure(merchantsListQueryResult);
        var merchants = merchantsListQueryResult.Data;

        // Contracts
        var contractsListQueryResult = await ReportingQueryExecutor.ToListAsync(context.Contracts.Where(e => e.EstateId == request.EstateId), cancellationToken, "Error retrieving contracts");
        if (contractsListQueryResult.IsFailed)
            return ResultHelpers.CreateFailure(contractsListQueryResult);
        var contracts = contractsListQueryResult.Data;

        Estate result = new() {
            EstateId = estate.EstateId,
            EstateName = estate.Name,
            Reference = estate.Reference,
            Operators = operators.Select(o => new Models.EstateOperator { OperatorId = o.OperatorId, Name = o.Name, }).ToList(),
            Users = users.Select(u => new Models.EstateUser { UserId = u.SecurityUserId, EmailAddress = u.EmailAddress, CreatedDateTime = u.CreatedDateTime }).ToList(),
            Merchants = merchants.Select(m => new Models.EstateMerchant { MerchantId = m.MerchantId, Name = m.Name, Reference = m.Reference }).ToList(),
            Contracts = contracts.Select(c => new Models.EstateContract { ContractId = c.ContractId, Name = c.Description, }).ToList()
        };

        return result;
}
}
