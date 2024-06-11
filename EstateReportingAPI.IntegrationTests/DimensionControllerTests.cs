namespace EstateReportingAPI.IntegrationTests;

using EstateManagement.Database.Contexts;
using EstateReportingAPI.DataTrasferObjects;
using Shouldly;
using Xunit;
using Merchant = DataTrasferObjects.Merchant;
using Operator = DataTrasferObjects.Operator;
using ResponseCode = DataTrasferObjects.ResponseCode;

public class DimensionsControllerTests : ControllerTestsBase
{
    [Theory]
    [InlineData(ClientType.Api)]
    [InlineData(ClientType.Direct)]
    public async Task DimensionsController_GetCalendarYears_NoDataInDatabase(ClientType clientType)
    {
        Func<Task<List<CalendarYear>?>> asyncFunction = async () =>
                                                        {
                                                            List<CalendarYear>? result = clientType switch
                                                            {
                                                                ClientType.Api => await ApiClient.GetCalendarYears(string.Empty, Guid.NewGuid(), CancellationToken.None),
                                                                _ => await CreateAndSendHttpRequestMessage<List<CalendarYear>>("api/dimensions/calendar/years", CancellationToken.None)
                                                            };
                                                            return result;
                                                        };
        List<CalendarYear> years = await ExecuteAsyncFunction(asyncFunction);

        years.ShouldNotBeNull();
        years.Count.ShouldBe(0);
    }

    [Theory]
    [InlineData(ClientType.Api)]
    [InlineData(ClientType.Direct)]
    public async Task DimensionsController_GetCalendarYears_YearsReturned(ClientType clientType)
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

        Func<Task<List<CalendarYear>?>> asyncFunction = async () =>
                                                        {
                                                            List<CalendarYear>? result = clientType switch
                                                            {
                                                                ClientType.Api => await ApiClient.GetCalendarYears(string.Empty, Guid.NewGuid(), CancellationToken.None),
                                                                _ => await CreateAndSendHttpRequestMessage<List<CalendarYear>>("api/dimensions/calendar/years", CancellationToken.None)
                                                            };
                                                            return result;
                                                        };
        List<CalendarYear> years = await ExecuteAsyncFunction(asyncFunction);

        years.ShouldNotBeNull();
        years.Count.ShouldBe(yearList.Count);
    }

    [Theory]
    [InlineData(ClientType.Api)]
    [InlineData(ClientType.Direct)]
    public async Task DimensionsController_GetCalendarComparisonDates_DatesReturned(ClientType clientType)
    {
        List<DateTime> datesInYear = helper.GetDatesForYear(DateTime.Now.Year);
        await helper.AddCalendarDates(datesInYear);

        Func<Task<List<ComparisonDate>?>> asyncFunction = async () =>
                                                          {
                                                              List<ComparisonDate>? result = clientType switch
                                                              {
                                                                  ClientType.Api => await ApiClient.GetComparisonDates(string.Empty, Guid.NewGuid(), CancellationToken.None),
                                                                  _ => await CreateAndSendHttpRequestMessage<List<ComparisonDate>>("api/dimensions/calendar/comparisondates", CancellationToken.None)
                                                              };
                                                              return result;
                                                          };
        List<ComparisonDate> dates = await ExecuteAsyncFunction(asyncFunction);


        List<DateTime> expectedDates = datesInYear.Where(d => d <= DateTime.Now.Date.AddDays(-1)).ToList();
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

    [Theory]
    [InlineData(ClientType.Api)]
    [InlineData(ClientType.Direct)]
    public async Task DimensionsController_GetCalendarDates_DatesReturned(ClientType clientType)
    {
        List<DateTime> datesInYear = helper.GetDatesForYear(2023);
        await helper.AddCalendarDates(datesInYear);

        Func<Task<List<CalendarDate>?>> asyncFunction = async () =>
                                                        {
                                                            List<CalendarDate>? result = clientType switch
                                                            {
                                                                ClientType.Api => await ApiClient.GetCalendarDates(string.Empty, Guid.NewGuid(), 2023, CancellationToken.None),
                                                                _ => await CreateAndSendHttpRequestMessage<List<CalendarDate>>("api/dimensions/calendar/2023/dates", CancellationToken.None)
                                                            };
                                                            return result;
                                                        };
        List<CalendarDate> dates = await ExecuteAsyncFunction(asyncFunction);
        
        dates.ShouldNotBeNull();
        dates.Count.ShouldBe(datesInYear.Where(d => d <= DateTime.Now.Date).ToList().Count);

        foreach (DateTime date in datesInYear.Where(d => d <= DateTime.Now.Date).ToList())
        {
            CalendarDate? x = dates.SingleOrDefault(d => d.Date == date);
            x.ShouldNotBeNull();
        }
    }

    [Theory]
    [InlineData(ClientType.Api)]
    [InlineData(ClientType.Direct)]
    public async Task DimensionsController_GetCalendarDates_NoDataInDatabase(ClientType clientType)
    {
        Func<Task<List<CalendarDate>?>> asyncFunction = async () =>
                                                        {
                                                            List<CalendarDate>? result = clientType switch
                                                            {
                                                                ClientType.Api => await ApiClient.GetCalendarDates(string.Empty, Guid.NewGuid(), 2023, CancellationToken.None),
                                                                _ => await CreateAndSendHttpRequestMessage<List<CalendarDate>>("api/dimensions/calendar/2023/dates", CancellationToken.None)
                                                            };
                                                            return result;
                                                        };
        List<CalendarDate> dates = await ExecuteAsyncFunction(asyncFunction);

        dates.ShouldNotBeNull();
        dates.Count.ShouldBe(0);
    }

    [Theory]
    [InlineData(ClientType.Api)]
    [InlineData(ClientType.Direct)]
    public async Task DimensionsController_GetMerchants_NoData_NoMerchantsReturned(ClientType clientType)
    {
        Func<Task<List<Merchant>?>> asyncFunction = async () =>
                                                    {
                                                        List<Merchant>? result = clientType switch
                                                        {
                                                            ClientType.Api => await ApiClient.GetMerchants(string.Empty, Guid.NewGuid(), CancellationToken.None),
                                                            _ => await CreateAndSendHttpRequestMessage<List<Merchant>>("api/dimensions/merchants", CancellationToken.None)
                                                        };
                                                        return result;
                                                    };
        List<Merchant> merchants = await ExecuteAsyncFunction(asyncFunction);


        merchants.ShouldNotBeNull();
        merchants.Count.ShouldBe(0);
    }

    [Theory]
    [InlineData(ClientType.Api)]
    [InlineData(ClientType.Direct)]
    public async Task DimensionsController_GetMerchants_NoAddresses_MerchantsReturned(ClientType clientType)
    {
        int estateReportingId = await helper.AddEstate("Test Estate", "Ref1");

        for (int i = 0; i < 10; i++)
        {
            await helper.AddMerchant("Test Estate", $"Test Merchant {i}", DateTime.Now);
        }

        Func<Task<List<Merchant>?>> asyncFunction = async () =>
                                                    {
                                                        List<Merchant>? result = clientType switch
                                                        {
                                                            ClientType.Api => await ApiClient.GetMerchants(string.Empty, Guid.NewGuid(), CancellationToken.None),
                                                            _ => await CreateAndSendHttpRequestMessage<List<Merchant>>("api/dimensions/merchants", CancellationToken.None)
                                                        };
                                                        return result;
                                                    };
        List<Merchant> merchants = await ExecuteAsyncFunction(asyncFunction);

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

    [Theory]
    [InlineData(ClientType.Api)]
    [InlineData(ClientType.Direct)]
    public async Task DimensionsController_GetMerchants_EachMerchantHasOneAddress_MerchantsReturned(ClientType clientType)
    {
        int estateReportingId = await helper.AddEstate("Test Estate", "Ref1");

        for (int i = 0; i < 10; i++)
        {
            List<(string addressLine1, string town, string postCode, string region)> addressList = new List<(string addressLine1, string town, string postCode, string region)>();

            addressList.Add(("Address Line 1", $"Test Town {i}", $"TE57 {i}NG", $"Region {i}"));
            await helper.AddMerchant("Test Estate", $"Test Merchant {i}", DateTime.Now, addressList);
        }

        Func<Task<List<Merchant>?>> asyncFunction = async () =>
                                                    {
                                                        List<Merchant>? result = clientType switch
                                                        {
                                                            ClientType.Api => await ApiClient.GetMerchants(string.Empty, Guid.NewGuid(), CancellationToken.None),
                                                            _ => await CreateAndSendHttpRequestMessage<List<Merchant>>("api/dimensions/merchants", CancellationToken.None)
                                                        };
                                                        return result;
                                                    };
        List<Merchant> merchants = await ExecuteAsyncFunction(asyncFunction);

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

    [Theory]
    [InlineData(ClientType.Api)]
    [InlineData(ClientType.Direct)]
    public async Task DimensionsController_GetMerchants_EachMerchantHasTwoAddress_MerchantsReturned(ClientType clientType)
    {
        int estateReportingId = await helper.AddEstate("Test Estate", "Ref1");

        for (int i = 0; i < 10; i++)
        {
            List<(string addressLine1, string town, string postCode, string region)> addressList = new List<(string addressLine1, string town, string postCode, string region)>();
            for (int j = 0; j < 2; j++)
            {
                addressList.Add(("Address Line 1", $"Test Town {i}{j}", $"TE5{j} {i}NG", $"Region {i}{j}"));
            }
            await helper.AddMerchant("Test Estate", $"Test Merchant {i}", DateTime.Now, addressList);
        }

        Func<Task<List<Merchant>?>> asyncFunction = async () =>
                                                    {
                                                        List<Merchant>? result = clientType switch
                                                        {
                                                            ClientType.Api => await ApiClient.GetMerchants(string.Empty, Guid.NewGuid(), CancellationToken.None),
                                                            _ => await CreateAndSendHttpRequestMessage<List<Merchant>>("api/dimensions/merchants", CancellationToken.None)
                                                        };
                                                        return result;
                                                    };
        List<Merchant> merchants = await ExecuteAsyncFunction(asyncFunction);

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

    [Theory]
    [InlineData(ClientType.Api)]
    [InlineData(ClientType.Direct)]
    public async Task DimensionsController_GetOperators_NoData_NoOperatorsReturned(ClientType clientType)
    {
        Func<Task<List<Operator>?>> asyncFunction = async () =>
                                                    {
                                                        List<Operator>? result = clientType switch
                                                        {
                                                            ClientType.Api => await ApiClient.GetOperators(string.Empty, Guid.NewGuid(), CancellationToken.None),
                                                            _ => await CreateAndSendHttpRequestMessage<List<Operator>>("api/dimensions/operators", CancellationToken.None)
                                                        };
                                                        return result;
                                                    };
        List<Operator> operators = await ExecuteAsyncFunction(asyncFunction);

        operators.ShouldNotBeNull();
        operators.Count.ShouldBe(0);
    }

    [Theory]
    [InlineData(ClientType.Api)]
    [InlineData(ClientType.Direct)]
    public async Task DimensionsController_GetOperators_OperatorsReturned(ClientType clientType)
    {
        int estateReportingId = await helper.AddEstate("Test Estate", "Ref1");

        Int32 operator1ReportingId = await this.helper.AddOperator("Test Estate", "Operator1");
        Int32 operator2ReportingId = await this.helper.AddOperator("Test Estate", "Operator2");
        Int32 operator3ReportingId = await this.helper.AddOperator("Test Estate", "Operator3");
        
        Func<Task<List<Operator>?>> asyncFunction = async () =>
                                                    {
                                                        List<Operator>? result = clientType switch
                                                        {
                                                            ClientType.Api => await ApiClient.GetOperators(string.Empty, Guid.NewGuid(), CancellationToken.None),
                                                            _ => await CreateAndSendHttpRequestMessage<List<Operator>>("api/dimensions/operators", CancellationToken.None)
                                                        };
                                                        return result;
                                                    };
        List<Operator> operators = await ExecuteAsyncFunction(asyncFunction);

        operators.ShouldNotBeNull();
        operators.Count.ShouldBe(3);
        operators.Any(o => o.Name == "Operator1").ShouldBeTrue();
        operators.Any(o => o.Name == "Operator2").ShouldBeTrue();
        operators.Any(o => o.Name == "Operator3").ShouldBeTrue();
    }

    [Theory]
    [InlineData(ClientType.Api)]
    [InlineData(ClientType.Direct)]
    public async Task DimensionsController_GetResponseCodes_ResponseCodesReturned(ClientType clientType)
    {
        await helper.AddResponseCode(0, "Success");
        await helper.AddResponseCode(1000, "Unknown Device");
        await helper.AddResponseCode(1001, "Unknown Estate");
        await helper.AddResponseCode(1002, "Unknown Merchant");
        await helper.AddResponseCode(1003, "No Devices Configured");

        Func<Task<List<ResponseCode>?>> asyncFunction = async () =>
                                                        {
                                                            List<ResponseCode>? result = clientType switch
                                                            {
                                                                ClientType.Api => await ApiClient.GetResponseCodes(string.Empty, Guid.NewGuid(), CancellationToken.None),
                                                                _ => await CreateAndSendHttpRequestMessage<List<ResponseCode>>("api/dimensions/responsecodes", CancellationToken.None)
                                                            };
                                                            return result;
                                                        };
        List<ResponseCode> responseCodes = await ExecuteAsyncFunction(asyncFunction);

        responseCodes.ShouldNotBeNull();
        responseCodes.Count.ShouldBe(5);
        responseCodes.Any(o => o.Code == 0).ShouldBeTrue();
        responseCodes.Any(o => o.Code == 1000).ShouldBeTrue();
        responseCodes.Any(o => o.Code == 1001).ShouldBeTrue();
        responseCodes.Any(o => o.Code == 1002).ShouldBeTrue();
        responseCodes.Any(o => o.Code == 1003).ShouldBeTrue();
    }

    [Theory]
    [InlineData(ClientType.Api)]
    [InlineData(ClientType.Direct)]
    public async Task DimensionsController_GetResponseCodes_NoData_NoResponseCodesReturned(ClientType clientType)
    {
        Func<Task<List<ResponseCode>?>> asyncFunction = async () =>
                                                        {
                                                            List<ResponseCode>? result = clientType switch
                                                            {
                                                                ClientType.Api => await ApiClient.GetResponseCodes(string.Empty, Guid.NewGuid(), CancellationToken.None),
                                                                _ => await CreateAndSendHttpRequestMessage<List<ResponseCode>>("api/dimensions/responsecodes", CancellationToken.None)
                                                            };
                                                            return result;
                                                        };
        List<ResponseCode> responseCodes = await ExecuteAsyncFunction(asyncFunction);

        responseCodes.ShouldBeEmpty();
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
