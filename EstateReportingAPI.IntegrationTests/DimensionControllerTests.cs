using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SimpleResults;
using TransactionProcessor.Database.Contexts;

namespace EstateReportingAPI.IntegrationTests;

using EstateReportingAPI.DataTrasferObjects;
using Shouldly;
using Xunit;
using Merchant = DataTrasferObjects.Merchant;
using Operator = DataTrasferObjects.Operator;
using ResponseCode = DataTrasferObjects.ResponseCode;

public class DimensionsControllerTests : ControllerTestsBase
{
    [Fact]
    public async Task DimensionsController_GetCalendarYears_NoDataInDatabase()
    {
        var yearsResult = await ApiClient.GetCalendarYears(string.Empty, Guid.NewGuid(), CancellationToken.None);
        yearsResult.IsFailed.ShouldBeTrue();
    }

    [Fact]
    public async Task DimensionsController_GetCalendarYears_YearsReturned()
    {

        List<int> yearList = new(){
                                        2024,
                                        2023,
                                        2022,
                                        2021
                                    };

        foreach (int year in yearList)
        {
            await helper.AddCalendarYear(year);
        }

        var result = await ApiClient.GetCalendarYears(string.Empty, Guid.NewGuid(), CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        var years = result.Data;
        years.ShouldNotBeNull();
        years.Count.ShouldBe(yearList.Count);
    }

    [Fact]
    public async Task DimensionsController_GetCalendarComparisonDates_DatesReturned()
    {
        List<DateTime> datesInYear = helper.GetDatesForYear(DateTime.Now.Year);
        await helper.AddCalendarDates(datesInYear);

        Result<List<ComparisonDate>> result = await ApiClient.GetComparisonDates(string.Empty, Guid.NewGuid(), CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        List<ComparisonDate> dates = result.Data;
        List <DateTime> expectedDates = datesInYear.Where(d => d <= DateTime.Now.Date.AddDays(-1)).ToList();
        int expectedCount = expectedDates.Count + 2;
        dates.ShouldNotBeNull();
        dates.Count.ShouldBe(expectedCount);
        foreach (DateTime date in expectedDates)
        {
            dates.Select(d => d.Date).Contains(date.Date).ShouldBeTrue();
        }

        dates.Select(d => d.Description).Contains("Yesterday");
        dates.Select(d => d.Description).Contains("Last Week");
        dates.Select(d => d.Description).Contains("Last Month");
    }

    [Fact]
    public async Task DimensionsController_GetCalendarComparisonDates_NoDataInDatabase()
    {
        var result= await ApiClient.GetComparisonDates(string.Empty, Guid.NewGuid(), CancellationToken.None);
        result.IsFailed.ShouldBeTrue();;
    }

    [Fact]
    public async Task DimensionsController_GetCalendarDates_DatesReturned()
    {
        List<DateTime> datesInYear = helper.GetDatesForYear(2023);
        await helper.AddCalendarDates(datesInYear);

        var datesResult = await ApiClient.GetCalendarDates(string.Empty, Guid.NewGuid(), 2023, CancellationToken.None);

        datesResult.IsSuccess.ShouldBeTrue();
        var dates = datesResult.Data;
        dates.ShouldNotBeNull();
        dates.Count.ShouldBe(datesInYear.Where(d => d <= DateTime.Now.Date).ToList().Count);

        foreach (DateTime date in datesInYear.Where(d => d <= DateTime.Now.Date).ToList())
        {
            CalendarDate? x = dates.SingleOrDefault(d => d.Date == date);
            x.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task DimensionsController_GetCalendarDates_NoDataInDatabase()
    {
        var datesResult = await ApiClient.GetCalendarDates(string.Empty, Guid.NewGuid(), 2023, CancellationToken.None);
        datesResult.IsFailed.ShouldBeTrue();
    }

    [Fact]
    public async Task DimensionsController_GetMerchants_NoData_NoMerchantsReturned() {
        var result = await ApiClient.GetMerchants(string.Empty, Guid.NewGuid(), CancellationToken.None);
        result.IsFailed.ShouldBeTrue();
    }

    [Fact]
    public async Task DimensionsController_GetMerchants_NoAddresses_MerchantsReturned()
    {
        await helper.AddEstate("Test Estate", "Ref1");

        for (int i = 0; i < 10; i++)
        {
            await helper.AddMerchant("Test Estate", $"Test Merchant {i}", DateTime.Now);
        }

        var result = await ApiClient.GetMerchants(string.Empty, Guid.NewGuid(), CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        var merchants = result.Data;

        merchants.ShouldNotBeNull();
        merchants.Count.ShouldBe(10);
        merchants.Any(m => string.IsNullOrEmpty(m.Region) == false || string.IsNullOrEmpty(m.Town) == false ||
            string.IsNullOrEmpty(m.PostCode) == false).ShouldBeFalse();

        for (int i = 0; i < 10; i++)
        {
            Merchant? expected = merchants.SingleOrDefault(m => m.Name == $"Test Merchant {i}");
            expected.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task DimensionsController_GetMerchants_EachMerchantHasOneAddress_MerchantsReturned()
    {
        await helper.AddEstate("Test Estate", "Ref1");

        for (int i = 0; i < 10; i++)
        {
            List<(string addressLine1, string town, string postCode, string region)> addressList = [
                ("Address Line 1", $"Test Town {i}", $"TE57 {i}NG", $"Region {i}")
            ];

            await helper.AddMerchant("Test Estate", $"Test Merchant {i}", DateTime.Now, addressList);
        }

        var result = await ApiClient.GetMerchants(string.Empty, Guid.NewGuid(), CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        var merchants = result.Data;

        merchants.ShouldNotBeNull();
        merchants.Count.ShouldBe(10);
        merchants.Any(m => string.IsNullOrEmpty(m.Region) == false || string.IsNullOrEmpty(m.Town) == false ||
                           string.IsNullOrEmpty(m.PostCode) == false).ShouldBeTrue();

        for (int i = 0; i < 10; i++)
        {
            Merchant? expected = merchants.SingleOrDefault(m => m.Name == $"Test Merchant {i}");
            expected.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task DimensionsController_GetMerchants_EachMerchantHasTwoAddress_MerchantsReturned()
    {
        await helper.AddEstate("Test Estate", "Ref1");

        for (int i = 0; i < 10; i++)
        {
            List<(string addressLine1, string town, string postCode, string region)> addressList = [];
            for (int j = 0; j < 2; j++)
            {
                addressList.Add(("Address Line 1", $"Test Town {i}{j}", $"TE5{j} {i}NG", $"Region {i}{j}"));
            }
            await helper.AddMerchant("Test Estate", $"Test Merchant {i}", DateTime.Now, addressList);
        }

        var result = await ApiClient.GetMerchants(string.Empty, Guid.NewGuid(), CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        var merchants = result.Data;

        merchants.ShouldNotBeNull();
        merchants.Count.ShouldBe(10);
        merchants.Any(m => string.IsNullOrEmpty(m.Region) == false || string.IsNullOrEmpty(m.Town) == false ||
                           string.IsNullOrEmpty(m.PostCode) == false).ShouldBeTrue();

        for (int i = 0; i < 10; i++)
        {
            Merchant? expected = merchants.SingleOrDefault(m => m.Name == $"Test Merchant {i}");
            expected.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task DimensionsController_GetOperators_NoData_NoOperatorsReturned()
    {
        Result<List<Operator>> result = await ApiClient.GetOperators(string.Empty, Guid.NewGuid(), CancellationToken.None);
        result.IsFailed.ShouldBeTrue();
    }

    [Fact]
    public async Task DimensionsController_GetOperators_OperatorsReturned()
    {
        int estateReportingId = await helper.AddEstate("Test Estate", "Ref1");

        Int32 operator1ReportingId = await this.helper.AddOperator("Test Estate", "Operator1");
        Int32 operator2ReportingId = await this.helper.AddOperator("Test Estate", "Operator2");
        Int32 operator3ReportingId = await this.helper.AddOperator("Test Estate", "Operator3");
        
        var result = await ApiClient.GetOperators(string.Empty, Guid.NewGuid(), CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        
        var operators = result.Data;
        operators.ShouldNotBeNull();
        operators.Count.ShouldBe(3);
        var operator1 = operators.SingleOrDefault(o => o.Name == "Operator1");
        operator1.ShouldNotBeNull();
        operator1.EstateReportingId.ShouldBe(estateReportingId);
        operator1.OperatorReportingId.ShouldBe(operator1ReportingId);
        var operator2 = operators.SingleOrDefault(o => o.Name == "Operator2");
        operator2.ShouldNotBeNull();
        operator2.EstateReportingId.ShouldBe(estateReportingId);
        operator2.OperatorReportingId.ShouldBe(operator2ReportingId);
        var operator3 = operators.SingleOrDefault(o => o.Name == "Operator3");
        operator3.ShouldNotBeNull();
        operator3.EstateReportingId.ShouldBe(estateReportingId);
        operator3.OperatorReportingId.ShouldBe(operator3ReportingId);


    }

    [Fact]
    public async Task DimensionsController_GetResponseCodes_ResponseCodesReturned()
    {
        await helper.AddResponseCode(0, "Success");
        await helper.AddResponseCode(1000, "Unknown Device");
        await helper.AddResponseCode(1001, "Unknown Estate");
        await helper.AddResponseCode(1002, "Unknown Merchant");
        await helper.AddResponseCode(1003, "No Devices Configured");

        var result = await ApiClient.GetResponseCodes(string.Empty, Guid.NewGuid(), CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();

        var responseCodes = result.Data;

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
        var result = await ApiClient.GetResponseCodes(string.Empty, Guid.NewGuid(), CancellationToken.None);
        result.IsFailed.ShouldBeTrue();
    }

    protected override async Task ClearStandingData()
    {

    }

    protected override async Task SetupStandingData()
    {

    }

    public void Dispose()
    {
        EstateManagementGenericContext context = new EstateManagementSqlServerContext(GetLocalConnectionString($"EstateReportingReadModel{TestId.ToString()}"));

        Console.WriteLine($"About to delete database EstateReportingReadModel{TestId.ToString()}");
        bool result = context.Database.EnsureDeleted();
        Console.WriteLine($"Delete result is {result}");
        result.ShouldBeTrue();
    }
}

public enum ClientType
{
    Api,
    Direct
}
