using TransactionProcessor.Database.Contexts;
using TransactionProcessor.Database.Entities;
using EstateReportingAPI.BusinessLogic.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.EntityFramework;
using Imposter.Abstractions;

[assembly: GenerateImposter(typeof(Shared.EntityFramework.IDbContextResolver<EstateManagementContext>))]

namespace EstateReportingAPI.BusinessLogic.UnitTests
{
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
            var options = new DbContextOptionsBuilder<EstateManagementContext>()
                          .UseInMemoryDatabase(databaseName: "TestDatabase")
                          .Options;

            EstateManagementContext context = new EstateManagementContext(options);
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
            var services = new ServiceCollection()
                           .AddDbContext<EstateManagementContext>(builder => builder.UseInMemoryDatabase("TestDatabase"))
                           .BuildServiceProvider();
            var resolver = new IDbContextResolverImposter<EstateManagementContext>();
            resolver.Resolve(Arg<String>.Any(), Arg<String>.Any()).Returns(new ResolvedDbContext<EstateManagementContext>(services.CreateScope()));

            var manager = new ReportingManager(resolver.Instance());

            var years = await manager.GetCalendarYears(new CalendarQueries.GetYearsQuery(TestData.EstateId), CancellationToken.None);

            years.Data.Count.ShouldBe(3);
            years.Data.ShouldContain(2021);
            years.Data.ShouldContain(2022);
            years.Data.ShouldContain(2023);
        }
    }
}
