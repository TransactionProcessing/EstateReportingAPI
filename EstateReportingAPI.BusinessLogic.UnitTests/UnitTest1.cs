namespace EstateReportingAPI.BusinessLogic.UnitTests
{
    using EstateManagement.Database.Contexts;
    using EstateManagement.Database.Entities;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Moq;
    using Shouldly;

    public class ReportingManagerTests
    {
        //Task<List<Calendar>> GetCalendarComparisonDates(Guid estateId, CancellationToken cancellationToken);
        //Task<List<Calendar>> GetCalendarDates(Guid estateId, CancellationToken cancellationToken);
        //Task<List<Int32>> GetCalendarYears(Guid estateId, CancellationToken cancellationToken);
        //Task<MerchantKpi> GetMerchantsTransactionKpis(Guid estateId, CancellationToken cancellationToken);
        //Task<TodaysSales> GetTodaysFailedSales(Guid estateId, DateTime comparisonDate, String responseCode, CancellationToken cancellationToken);
        //Task<TodaysSales> GetTodaysSales(Guid estateId, DateTime comparisonDate, CancellationToken cancellationToken);
        //Task<List<TodaysSalesCountByHour>> GetTodaysSalesCountByHour(Guid estateId, DateTime comparisonDate, CancellationToken cancellationToken);
        //Task<List<TodaysSalesValueByHour>> GetTodaysSalesValueByHour(Guid estateId, DateTime comparisonDate, CancellationToken cancellationToken);
        //Task<TodaysSettlement> GetTodaysSettlement(Guid estateId, DateTime comparisonDate, CancellationToken cancellationToken);
        //Task<List<TopBottomData>> GetTopBottomData(Guid estateId, TopBottom direction, Int32 resultCount, Dimension dimension, CancellationToken cancellationToken);

        public class TestData{
            public static Guid EstateId => Guid.Parse("F64241E7-F778-4F77-8A64-099CB51BF4CE");
        }

        [Fact]
        public async Task ReportingManager_GetCalendarYears_YearsAreReturned(){

            //Required properties '{'DayOfWeek', 'DayOfWeekShort', 'MonthNameLong', 'MonthNameShort', 'WeekNumberString', 'YearWeekNumber'}' 
            var options = new DbContextOptionsBuilder<EstateManagementGenericContext>()
                          .UseInMemoryDatabase(databaseName: "TestDatabase")
                          .Options;

            EstateManagementGenericContext context = new EstateManagementSqlServerContext(options);
            await context.Database.EnsureCreatedAsync();

            await context.Calendar.AddRangeAsync(new Calendar{
                                                      Date = new DateTime(2023, 1, 1),
                                                      DayOfWeek = "Monday",
                                                      DayOfWeekShort = "Mon",
                                                      MonthNameLong = "January",
                                                      MonthNameShort ="Jan",
                                                      WeekNumberString = "01",
                                                      YearWeekNumber = "202301",
                                                      Year = 2023
                                                  },
                                                 new Calendar
                                                 {
                                                     Date = new DateTime(2022, 1, 1),
                                                     DayOfWeek = "Monday",
                                                     DayOfWeekShort = "Mon",
                                                     MonthNameLong = "January",
                                                     MonthNameShort = "Jan",
                                                     WeekNumberString = "01",
                                                     YearWeekNumber = "202301",
                                                     Year = 2022
                                                 },
                                                 new Calendar
                                                 {
                                                     Date = new DateTime(2021, 1, 1),
                                                     DayOfWeek = "Monday",
                                                     DayOfWeekShort = "Mon",
                                                     MonthNameLong = "January",
                                                     MonthNameShort = "Jan",
                                                     WeekNumberString = "01",
                                                     YearWeekNumber = "202101",
                                                     Year = 2021
                                                 });
            await context.SaveChangesAsync();
            Mock<Shared.EntityFramework.IDbContextFactory<EstateManagementGenericContext>> resolver = new Mock<Shared.EntityFramework.IDbContextFactory<EstateManagementGenericContext>>();
            resolver.Setup(r => r.GetContext(It.IsAny<Guid>(), It.IsAny<String>(), It.IsAny<CancellationToken>())).ReturnsAsync(context);

            var manager = new ReportingManager(resolver.Object);

            var years = await manager.GetCalendarYears(TestData.EstateId, CancellationToken.None);

            years.Count.ShouldBe(3);
            years.ShouldContain(2021);
            years.ShouldContain(2022);
            years.ShouldContain(2023);
        }
    }
}