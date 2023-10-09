namespace EstateReportingAPI.IntegrationTests;

using Common;
using DataTrasferObjects;
using EstateManagement.Database.Contexts;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using Shouldly;
using Xunit;

public class DimensionsControllerTests :ControllerTestsBase, IDisposable{

    #region Methods

    [Fact]
    public async Task DimensionsController_GetCalendarYears_NoDataInDatabase(){
        HttpResponseMessage response = await this.CreateAndSendHttpRequestMessage("api/dimensions/calendar/years");

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

        String content = await response.Content.ReadAsStringAsync(CancellationToken.None);
        List<CalendarDate> dates = JsonConvert.DeserializeObject<List<CalendarDate>>(content);
        dates.Count.ShouldBe(0);
    }

    [Fact]
    public async Task DimensionsController_GetMerchants_NoData_NoMerchantsReturned()
    {
        EstateManagementGenericContext context = new EstateManagementSqlServerContext(GetLocalConnectionString($"EstateReportingReadModel{this.TestId.ToString()}"));

        DatabaseHelper helper = new DatabaseHelper(context);
        
        HttpResponseMessage response = await this.CreateAndSendHttpRequestMessage("api/dimensions/merchants");

        String content = await response.Content.ReadAsStringAsync(CancellationToken.None);
        List<Merchant> merchants = JsonConvert.DeserializeObject<List<Merchant>>(content);
        merchants.Count.ShouldBe(0);
    }

    [Fact]
    public async Task DimensionsController_GetMerchants_NoAddresses_MerchantsReturned()
    {
        EstateManagementGenericContext context = new EstateManagementSqlServerContext(GetLocalConnectionString($"EstateReportingReadModel{this.TestId.ToString()}"));

        DatabaseHelper helper = new DatabaseHelper(context);

        for (int i = 0; i < 10; i++){
            await helper.AddMerchant(1, $"Test Merchant {i}", DateTime.Now);
        }

        HttpResponseMessage response = await this.CreateAndSendHttpRequestMessage("api/dimensions/merchants");

        String content = await response.Content.ReadAsStringAsync(CancellationToken.None);
        List<Merchant> merchants = JsonConvert.DeserializeObject<List<Merchant>>(content);
        merchants.Count.ShouldBe(10);
        merchants.Any(m => String.IsNullOrEmpty(m.Region) == false || String.IsNullOrEmpty(m.Town) == false ||
            String.IsNullOrEmpty(m.PostCode) == false).ShouldBeFalse();

        for (int i = 0; i < 10; i++){
            var expected = merchants.SingleOrDefault(m => m.Name == $"Test Merchant {i}");
            expected.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task DimensionsController_GetMerchants_EachMerchantHasOneAddress_MerchantsReturned()
    {
        EstateManagementGenericContext context = new EstateManagementSqlServerContext(GetLocalConnectionString($"EstateReportingReadModel{this.TestId.ToString()}"));

        DatabaseHelper helper = new DatabaseHelper(context);

        for (int i = 0; i < 10; i++){
            var addressList = new List<(String addressLine1, String town, String postCode, String region)>();

            addressList.Add(("Address Line 1", $"Test Town {i}", $"TE57 {i}NG", $"Region {i}"));
            await helper.AddMerchant(1, $"Test Merchant {i}", DateTime.Now,addressList );
        }

        HttpResponseMessage response = await this.CreateAndSendHttpRequestMessage("api/dimensions/merchants");

        String content = await response.Content.ReadAsStringAsync(CancellationToken.None);
        List<Merchant> merchants = JsonConvert.DeserializeObject<List<Merchant>>(content);
        merchants.Count.ShouldBe(10);
        merchants.Any(m => String.IsNullOrEmpty(m.Region) == false || String.IsNullOrEmpty(m.Town) == false ||
                           String.IsNullOrEmpty(m.PostCode) == false).ShouldBeTrue();

        for (int i = 0; i < 10; i++)
        {
            var expected = merchants.SingleOrDefault(m => m.Name == $"Test Merchant {i}");
            expected.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task DimensionsController_GetMerchants_EachMerchantHasTwoAddress_MerchantsReturned()
    {
        EstateManagementGenericContext context = new EstateManagementSqlServerContext(GetLocalConnectionString($"EstateReportingReadModel{this.TestId.ToString()}"));

        DatabaseHelper helper = new DatabaseHelper(context);

        for (int i = 0; i < 10; i++){
            var addressList = new List<(String addressLine1, String town, String postCode, String region)>();
            for (int j = 0; j < 2; j++){
                addressList.Add(("Address Line 1", $"Test Town {i}{j}", $"TE5{j} {i}NG", $"Region {i}{j}"));
            }
            await helper.AddMerchant(1, $"Test Merchant {i}", DateTime.Now, addressList);
        }

        HttpResponseMessage response = await this.CreateAndSendHttpRequestMessage("api/dimensions/merchants");

        String content = await response.Content.ReadAsStringAsync(CancellationToken.None);
        List<Merchant> merchants = JsonConvert.DeserializeObject<List<Merchant>>(content);
        merchants.Count.ShouldBe(10);
        merchants.Any(m => String.IsNullOrEmpty(m.Region) == false || String.IsNullOrEmpty(m.Town) == false ||
                           String.IsNullOrEmpty(m.PostCode) == false).ShouldBeTrue();

        for (int i = 0; i < 10; i++)
        {
            var expected = merchants.SingleOrDefault(m => m.Name == $"Test Merchant {i}");
            expected.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task DimensionsController_GetOperators_NoData_NoOperatorsReturned()
    {
        EstateManagementGenericContext context = new EstateManagementSqlServerContext(GetLocalConnectionString($"EstateReportingReadModel{this.TestId.ToString()}"));

        DatabaseHelper helper = new DatabaseHelper(context);

        HttpResponseMessage response = await this.CreateAndSendHttpRequestMessage("api/dimensions/operators");
        
        String content = await response.Content.ReadAsStringAsync(CancellationToken.None);
        List<Operator> operators = JsonConvert.DeserializeObject<List<Operator>>(content);
        operators.Count.ShouldBe(0);
    }

    [Fact]
    public async Task DimensionsController_GetOperators_OperatorsReturned()
    {
        EstateManagementGenericContext context = new EstateManagementSqlServerContext(GetLocalConnectionString($"EstateReportingReadModel{this.TestId.ToString()}"));

        DatabaseHelper helper = new DatabaseHelper(context);
        await helper.AddEstateOperator("Operator1");
        await helper.AddEstateOperator("Operator2");
        await helper.AddEstateOperator("Operator3");

        HttpResponseMessage response = await this.CreateAndSendHttpRequestMessage("api/dimensions/operators");

        String content = await response.Content.ReadAsStringAsync(CancellationToken.None);
        List<Operator> operators = JsonConvert.DeserializeObject<List<Operator>>(content);
        operators.Count.ShouldBe(3);
        operators.Any(o => o.Name == "Operator1").ShouldBeTrue();
        operators.Any(o => o.Name == "Operator2").ShouldBeTrue();
        operators.Any(o => o.Name == "Operator3").ShouldBeTrue();
    }

    #endregion

    //public void Dispose(){
    //    EstateManagementGenericContext context = new EstateManagementSqlServerContext(GetLocalConnectionString($"EstateReportingReadModel{this.TestId.ToString()}"));

    //    Console.WriteLine($"About to delete database EstateReportingReadModel{this.TestId.ToString()}");
    //    Boolean result = context.Database.EnsureDeleted();
    //    Console.WriteLine($"Delete result is {result}");
    //    result.ShouldBeTrue();
    //}
}