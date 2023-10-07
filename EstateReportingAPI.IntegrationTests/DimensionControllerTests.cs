namespace EstateReportingAPI.IntegrationTests;

using Common;
using DataTrasferObjects;
using EstateManagement.Database.Contexts;
using Newtonsoft.Json;
using Shouldly;
using Xunit;

public class DimensionsControllerTests :ControllerTestsBase, IDisposable{
    
    #region Methods

    [Fact]
    public async Task DimensionsController_GetCalendarYears_NoDataInDatabase(){
        HttpResponseMessage response = await this.CreateAndSendHttpRequestMessage("api/dimensions/calendar/years");

        response.IsSuccessStatusCode.ShouldBeTrue();
        String content = await response.Content.ReadAsStringAsync(CancellationToken.None);
        List<CalendarYear>? years = JsonConvert.DeserializeObject<List<CalendarYear>>(content);
        years.Count.ShouldBe(0);
    }

    [Fact]
    public async Task DimensionsController_GetCalendarYears_YearsReturned(){
        EstateManagementGenericContext context = new EstateManagementSqlServerContext(GetLocalConnectionString($"EstateReportingReadModel{this.TestId.ToString()}"));

        DatabaseHelper helper = new DatabaseHelper(context);

        List<Int32> yearList = new(){
                                        2023,
                                        2022,
                                        2021
                                    };

        foreach (Int32 year in yearList){
            await helper.AddCalendarYear(year);
        }

        HttpResponseMessage response = await this.CreateAndSendHttpRequestMessage("api/dimensions/calendar/years");

        response.IsSuccessStatusCode.ShouldBeTrue();
        String content = await response.Content.ReadAsStringAsync(CancellationToken.None);
        List<CalendarYear>? years = JsonConvert.DeserializeObject<List<CalendarYear>>(content);
        years.Count.ShouldBe(yearList.Count);
    }

    [Fact]
    public async Task DimensionsController_GetCalendarComparisonDates_DatesReturned(){
        EstateManagementGenericContext context = new EstateManagementSqlServerContext(GetLocalConnectionString($"EstateReportingReadModel{this.TestId.ToString()}"));

        DatabaseHelper helper = new DatabaseHelper(context);

        List<DateTime> datesInYear = helper.GetDatesForYear(2023);
        await helper.AddCalendarDates(datesInYear);

        HttpResponseMessage response = await this.CreateAndSendHttpRequestMessage("api/dimensions/calendar/comparisondates");

        response.IsSuccessStatusCode.ShouldBeTrue();
        String content = await response.Content.ReadAsStringAsync(CancellationToken.None);
        List<ComparisonDate> dates = JsonConvert.DeserializeObject<List<ComparisonDate>>(content);

        List<DateTime> expectedDates = datesInYear.Where(d => d <= DateTime.Now.Date.AddDays(-1)).ToList();
        Int32 expectedCount = expectedDates.Count + 2;
        dates.Count.ShouldBe(expectedCount);
        foreach (DateTime date in expectedDates){
            dates.Select(d => d.Date).Contains(date.Date).ShouldBeTrue();
        }

        dates.Select(d => d.Description).Contains("Yesterday");
        dates.Select(d => d.Description).Contains("Last Week");
        dates.Select(d => d.Description).Contains("Last Month");
    }

    [Fact]
    public async Task DimensionsController_GetCalendarDates_DatesReturned(){
        EstateManagementGenericContext context = new EstateManagementSqlServerContext(GetLocalConnectionString($"EstateReportingReadModel{this.TestId.ToString()}"));

        DatabaseHelper helper = new DatabaseHelper(context);

        List<DateTime> datesInYear = helper.GetDatesForYear(2023);
        await helper.AddCalendarDates(datesInYear);

        HttpResponseMessage response = await this.CreateAndSendHttpRequestMessage("api/dimensions/calendar/2023/dates");

        response.IsSuccessStatusCode.ShouldBeTrue();
        String content = await response.Content.ReadAsStringAsync(CancellationToken.None);
        List<CalendarDate> dates = JsonConvert.DeserializeObject<List<CalendarDate>>(content);
        dates.Count.ShouldBe(datesInYear.Where(d => d <= DateTime.Now.Date).ToList().Count);

        foreach (DateTime date in datesInYear.Where(d => d <= DateTime.Now.Date).ToList()){
            CalendarDate? x = dates.SingleOrDefault(d => d.Date == date);
            x.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task DimensionsController_GetCalendarDates_NoDataInDatabase(){
        HttpResponseMessage response = await this.CreateAndSendHttpRequestMessage("api/dimensions/calendar/2023/dates");

        response.IsSuccessStatusCode.ShouldBeTrue();
        String content = await response.Content.ReadAsStringAsync(CancellationToken.None);
        List<CalendarDate> dates = JsonConvert.DeserializeObject<List<CalendarDate>>(content);
        dates.Count.ShouldBe(0);
    }
    
    #endregion

    public void Dispose(){
        EstateManagementGenericContext context = new EstateManagementSqlServerContext(GetLocalConnectionString($"EstateReportingReadModel{this.TestId.ToString()}"));

        Console.WriteLine($"About to delete database EstateReportingReadModel{this.TestId.ToString()}");
        Boolean result = context.Database.EnsureDeleted();
        Console.WriteLine($"Delete result is {result}");
        result.ShouldBeTrue();
    }
}