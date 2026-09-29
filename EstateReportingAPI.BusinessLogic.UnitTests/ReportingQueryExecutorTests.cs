using EstateReportingAPI.BusinessLogic;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using SimpleResults;
using TransactionProcessor.Database.Contexts;

namespace EstateReportingAPI.BusinessLogic.UnitTests;

public sealed class ReportingQueryExecutorTests
{
    [Fact]
    public async Task QueryOperations_ReturnSuccessfulResults()
    {
        await using EstateManagementContext context = CreateContext();
        await context.Calendar.AddRangeAsync(
            CreateCalendar(new DateTime(2025, 1, 1), 2025),
            CreateCalendar(new DateTime(2026, 1, 1), 2026));
        await context.SaveChangesAsync();

        Result<List<int>> list = await ReportingQueryExecutor.ToListAsync(
            context.Calendar.Select(calendar => calendar.Year), CancellationToken.None);
        Result<int> count = await ReportingQueryExecutor.CountAsync(
            context.Calendar, CancellationToken.None);
        Result<int> sum = await ReportingQueryExecutor.SumAsync(
            context.Calendar.Select(calendar => calendar.Year), CancellationToken.None);
        Result<TransactionProcessor.Database.Entities.Calendar> single = await ReportingQueryExecutor.SingleOrDefaultAsync(
            context.Calendar.Where(calendar => calendar.Year == 2025), CancellationToken.None, "Calendar not found");

        list.Data.ShouldBe([2025, 2026]);
        count.Data.ShouldBe(2);
        sum.Data.ShouldBe(4051);
        single.Data.Year.ShouldBe(2025);
    }

    [Fact]
    public async Task SingleOrDefaultAsync_ReturnsNotFoundWhenNoItemExists()
    {
        await using EstateManagementContext context = CreateContext();

        Result<TransactionProcessor.Database.Entities.Calendar> result = await ReportingQueryExecutor.SingleOrDefaultAsync(
            context.Calendar, CancellationToken.None, "Calendar not found");

        result.IsSuccess.ShouldBeFalse();
        result.Message.ShouldBe("Calendar not found");
    }

    [Fact]
    public async Task ToListAsync_ReturnsFailureWhenQueryThrows()
    {
        await using EstateManagementContext context = CreateContext();
        await context.DisposeAsync();

        Result<List<TransactionProcessor.Database.Entities.Calendar>> result = await ReportingQueryExecutor.ToListAsync(
            context.Calendar, CancellationToken.None, "Error retrieving calendars");

        result.IsFailed.ShouldBeTrue();
        result.Message.ShouldContain("Error retrieving calendars");
    }

    private static EstateManagementContext CreateContext()
    {
        DbContextOptions<EstateManagementContext> options = new DbContextOptionsBuilder<EstateManagementContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EstateManagementContext(options);
    }

    private static TransactionProcessor.Database.Entities.Calendar CreateCalendar(DateTime date, int year)
    {
        return new TransactionProcessor.Database.Entities.Calendar
        {
            Date = date,
            Year = year,
            DayOfWeek = "Monday",
            DayOfWeekShort = "Mon",
            MonthNameLong = "January",
            MonthNameShort = "Jan",
            WeekNumberString = "01",
            YearWeekNumber = $"{year}01"
        };
    }
}
