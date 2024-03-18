namespace EstateReportingAPI.IntegrationTests;

using Common;
using DataTrasferObjects;
using EstateManagement.Database.Contexts;
using EstateManagement.Database.Entities;
using EstateReportingAPI.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using Shouldly;
using Xunit;
using Merchant = DataTrasferObjects.Merchant;
using Operator = DataTrasferObjects.Operator;
using ResponseCode = DataTrasferObjects.ResponseCode;

public class DimensionsControllerTests :ControllerTestsBase{

    [Fact]
    public async Task DimensionsController_GetCalendarYears_NoDataInDatabase(){
        List<CalendarYear>? years = await this.CreateAndSendHttpRequestMessage<List<CalendarYear>>("api/dimensions/calendar/years", CancellationToken.None);
        years.ShouldNotBeNull();
        years.Count.ShouldBe(0);
    }
    
    [Fact]
    public async Task DimensionsController_GetCalendarYears_YearsReturned(){
        
        List<Int32> yearList = new(){
                                        2024,
                                        2023,
                                        2022,
                                        2021
                                    };

        foreach (Int32 year in yearList){
            await helper.AddCalendarYear(year);
        }

        List<CalendarYear>? years= await this.CreateAndSendHttpRequestMessage<List<CalendarYear>>("api/dimensions/calendar/years", CancellationToken.None);

        years.ShouldNotBeNull();
        years.Count.ShouldBe(yearList.Count);
    }
    
    [Fact]
    public async Task DimensionsController_GetCalendarComparisonDates_DatesReturned(){
        List<DateTime> datesInYear = helper.GetDatesForYear(DateTime.Now.Year);
        await helper.AddCalendarDates(datesInYear);

        List<ComparisonDate>? dates = await this.CreateAndSendHttpRequestMessage<List<ComparisonDate>>("api/dimensions/calendar/comparisondates", CancellationToken.None);
        
        List<DateTime> expectedDates = datesInYear.Where(d => d <= DateTime.Now.Date.AddDays(-1)).ToList();
        Int32 expectedCount = expectedDates.Count + 2;
        dates.ShouldNotBeNull();
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
        List<DateTime> datesInYear = helper.GetDatesForYear(2023);
        await helper.AddCalendarDates(datesInYear);

        List<CalendarDate>? dates = await this.CreateAndSendHttpRequestMessage<List<CalendarDate>>("api/dimensions/calendar/2023/dates", CancellationToken.None);
        dates.ShouldNotBeNull();
        dates.Count.ShouldBe(datesInYear.Where(d => d <= DateTime.Now.Date).ToList().Count);

        foreach (DateTime date in datesInYear.Where(d => d <= DateTime.Now.Date).ToList()){
            CalendarDate? x = dates.SingleOrDefault(d => d.Date == date);
            x.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task DimensionsController_GetCalendarDates_NoDataInDatabase(){
        List<CalendarDate>? dates = await this.CreateAndSendHttpRequestMessage<List<CalendarDate>>("api/dimensions/calendar/2023/dates", CancellationToken.None);
        dates.ShouldNotBeNull();
        dates.Count.ShouldBe(0);
    }

    [Fact]
    public async Task DimensionsController_GetMerchants_NoData_NoMerchantsReturned()
    {
        List<Merchant>? merchants = await this.CreateAndSendHttpRequestMessage<List<Merchant>>("api/dimensions/merchants", CancellationToken.None);
        merchants.ShouldNotBeNull();
        merchants.Count.ShouldBe(0);
    }
    [Fact]
    public async Task DimensionsController_GetMerchants_NoAddresses_MerchantsReturned()
    {
        Int32 estateReportingId = await helper.AddEstate("Test Estate", "Ref1");

        for (int i = 0; i < 10; i++){
            await helper.AddMerchant("Test Estate", $"Test Merchant {i}", DateTime.Now);
        }

        List<Merchant>? merchants = await this.CreateAndSendHttpRequestMessage<List<Merchant>?>("api/dimensions/merchants", CancellationToken.None);
        merchants.ShouldNotBeNull();
        merchants.Count.ShouldBe(10);
        merchants.Any(m => String.IsNullOrEmpty(m.Region) == false || String.IsNullOrEmpty(m.Town) == false ||
            String.IsNullOrEmpty(m.PostCode) == false).ShouldBeFalse();

        for (int i = 0; i < 10; i++){
            Merchant? expected = merchants.SingleOrDefault(m => m.Name == $"Test Merchant {i}");
            expected.ShouldNotBeNull();
        }
    }
    
    [Fact]
    public async Task DimensionsController_GetMerchants_EachMerchantHasOneAddress_MerchantsReturned()
    {
        Int32 estateReportingId = await helper.AddEstate("Test Estate", "Ref1");

        for (int i = 0; i < 10; i++){
            List<(String addressLine1, String town, String postCode, String region)> addressList = new List<(String addressLine1, String town, String postCode, String region)>();

            addressList.Add(("Address Line 1", $"Test Town {i}", $"TE57 {i}NG", $"Region {i}"));
            await helper.AddMerchant("Test Estate", $"Test Merchant {i}", DateTime.Now,addressList );
        }

        List<Merchant>? merchants = await this.CreateAndSendHttpRequestMessage<List<Merchant>?>("api/dimensions/merchants", CancellationToken.None);
        merchants.ShouldNotBeNull();
        merchants.Count.ShouldBe(10);
        merchants.Any(m => String.IsNullOrEmpty(m.Region) == false || String.IsNullOrEmpty(m.Town) == false ||
                           String.IsNullOrEmpty(m.PostCode) == false).ShouldBeTrue();

        for (int i = 0; i < 10; i++)
        {
            Merchant? expected = merchants.SingleOrDefault(m => m.Name == $"Test Merchant {i}");
            expected.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task DimensionsController_GetMerchants_EachMerchantHasTwoAddress_MerchantsReturned()
    {
        Int32 estateReportingId = await helper.AddEstate("Test Estate", "Ref1");

        for (int i = 0; i < 10; i++){
            List<(String addressLine1, String town, String postCode, String region)> addressList = new List<(String addressLine1, String town, String postCode, String region)>();
            for (int j = 0; j < 2; j++){
                addressList.Add(("Address Line 1", $"Test Town {i}{j}", $"TE5{j} {i}NG", $"Region {i}{j}"));
            }
            await helper.AddMerchant("Test Estate", $"Test Merchant {i}", DateTime.Now, addressList);
        }

        List<Merchant>? merchants = await this.CreateAndSendHttpRequestMessage<List<Merchant>>("api/dimensions/merchants", CancellationToken.None);

        merchants.ShouldNotBeNull();
        merchants.Count.ShouldBe(10);
        merchants.Any(m => String.IsNullOrEmpty(m.Region) == false || String.IsNullOrEmpty(m.Town) == false ||
                           String.IsNullOrEmpty(m.PostCode) == false).ShouldBeTrue();

        for (int i = 0; i < 10; i++)
        {
            Merchant? expected = merchants.SingleOrDefault(m => m.Name == $"Test Merchant {i}");
            expected.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task DimensionsController_GetOperators_NoData_NoOperatorsReturned()
    {
        List<Operator>? operators = await this.CreateAndSendHttpRequestMessage<List<Operator>>("api/dimensions/operators", CancellationToken.None);
        operators.ShouldNotBeNull();
        operators.Count.ShouldBe(0);
    }

    [Fact]
    public async Task DimensionsController_GetOperators_OperatorsReturned()
    {
        Int32 estateReportingId = await helper.AddEstate("Test Estate", "Ref1");

        await helper.AddEstateOperator("Test Estate", "Operator1");
        await helper.AddEstateOperator("Test Estate", "Operator2");
        await helper.AddEstateOperator("Test Estate", "Operator3");

        List<Operator>? operators = await this.CreateAndSendHttpRequestMessage<List<Operator>>("api/dimensions/operators", CancellationToken.None);
        operators.ShouldNotBeNull();
        operators.Count.ShouldBe(3);
        operators.Any(o => o.Name == "Operator1").ShouldBeTrue();
        operators.Any(o => o.Name == "Operator2").ShouldBeTrue();
        operators.Any(o => o.Name == "Operator3").ShouldBeTrue();
    }

    [Fact]
    public async Task DimensionsController_GetResponseCodes_ResponseCodesReturned()
    {
        await helper.AddResponseCode(0, "Success");
        await helper.AddResponseCode(1000, "Unknown Device");
        await helper.AddResponseCode(1001, "Unknown Estate");
        await helper.AddResponseCode(1002, "Unknown Merchant");
        await helper.AddResponseCode(1003, "No Devices Configured");

        List<ResponseCode>? responseCodes = await this.CreateAndSendHttpRequestMessage<List<ResponseCode>>("api/dimensions/responsecodes", CancellationToken.None);
        responseCodes.ShouldNotBeNull();
        responseCodes.Count.ShouldBe(5);
        responseCodes.Any(o => o.Code == 0).ShouldBeTrue();
        responseCodes.Any(o => o.Code == 1000).ShouldBeTrue();
        responseCodes.Any(o => o.Code == 1001).ShouldBeTrue();
        responseCodes.Any(o => o.Code == 1002).ShouldBeTrue();
        responseCodes.Any(o => o.Code == 1003).ShouldBeTrue();
    }

    [Fact]
    public async Task DimensionsController_GetResponseCodes_NoData_NoResponseCodesReturned()
    {
        List<ResponseCode>? responseCodes = await this.CreateAndSendHttpRequestMessage<List<ResponseCode>>("api/dimensions/responsecodes", CancellationToken.None);
        responseCodes.ShouldBeEmpty();
    }

    protected override async Task ClearStandingData(){
        
    }

    protected override async Task SetupStandingData(){
        
    }

    public void Dispose(){
        EstateManagementGenericContext context = new EstateManagementSqlServerContext(GetLocalConnectionString($"EstateReportingReadModel{this.TestId.ToString()}"));

        Console.WriteLine($"About to delete database EstateReportingReadModel{this.TestId.ToString()}");
        Boolean result = context.Database.EnsureDeleted();
        Console.WriteLine($"Delete result is {result}");
        result.ShouldBeTrue();
    }
}