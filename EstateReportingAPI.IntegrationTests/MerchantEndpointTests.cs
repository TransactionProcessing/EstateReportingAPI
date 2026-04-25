using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EstateReportingAPI.DataTransferObjects;
using Shouldly;
using SimpleResults;
using Xunit;

namespace EstateReportingAPI.IntegrationTests;

public class MerchantEndpointTests : ControllerTestsBase {
    private String BaseRoute = "api/merchants";

    [Fact]
    public async Task MerchantEndpoint_GetMerchants_MerchantsReturned() {
        await this.helper.AddEstate("Test Estate", "Ref1");
        for (int i = 0; i < 10; i++) {
            await this.helper.AddMerchant("Test Estate", $"Test Merchant {i}",100 * i, DateTime.Now, DateTime.Now,
                ("Address Line 1", $"Test Town {i}", $"TE57 {i}NG", $"Region {i}"),
                ($"Contact {i}", @"{i}@2.com", $"{i}23456"));
        }

        Result<List<Merchant>> result = await this.CreateAndSendHttpRequestMessage<List<Merchant>>($"{this.BaseRoute}", CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        var merchants = result.Data;

        merchants.ShouldNotBeNull();
        merchants.Count.ShouldBe(10);
            
        for (int i = 0; i < 10; i++) {
            Merchant? expected = merchants.SingleOrDefault(m => m.Name == $"Test Merchant {i}");
            expected.ShouldNotBeNull();
            expected.Balance.ShouldBe(100 * i);
        }
    }

    [Fact]
    public async Task MerchantEndpoint_GetMerchant_MerchantReturned()
    {
        await this.helper.AddEstate("Test Estate", "Ref1");
        var merchantId = await this.helper.AddMerchant("Test Estate", $"Test Merchant 1",100, DateTime.Now, DateTime.Now,
            ("Address Line 1", $"Test Town", $"TE57 1NG", $"Region"), 
            ("Contact 1", "1@2.com", "123456"));

        Result<Merchant> result = await this.CreateAndSendHttpRequestMessage<Merchant>($"{this.BaseRoute}/{merchantId}", CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        var merchant = result.Data;

        merchant.ShouldNotBeNull();
        merchant.Name.ShouldBe("Test Merchant 1");
        merchant.Balance.ShouldBe(100);
    }

    [Fact]
    public async Task MerchantEndpoint_GetRecentMerchants_MerchantsReturned()
    {
        await this.helper.AddEstate("Test Estate", "Ref1");
        for (int i = 0; i < 10; i++)
        {
            await this.helper.AddMerchant("Test Estate", $"Test Merchant {i}",100, DateTime.Now.AddDays(i*-1), DateTime.Now,
                ("Address Line 1", $"Test Town {i}", $"TE57 {i}NG", $"Region {i}"),
                ($"Contact {i}", @"{i}@2.com", $"{i}23456"));
        }

        Result<List<Merchant>> result = await this.CreateAndSendHttpRequestMessage<List<Merchant>>($"{this.BaseRoute}/recent", CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        var merchants = result.Data;

        merchants.ShouldNotBeNull();
        merchants.Count.ShouldBe(3);
        merchants.SingleOrDefault(m => m.Name == "Test Merchant 0").ShouldNotBeNull();
        merchants.SingleOrDefault(m => m.Name == "Test Merchant 1").ShouldNotBeNull();
        merchants.SingleOrDefault(m => m.Name == "Test Merchant 2").ShouldNotBeNull();
    }


    [Fact]
    public async Task MerchantEndpoint_GetMerchantOperators_MerchantOperatorsReturned()
    {
        await this.helper.AddEstate("Test Estate", "Ref1");

        await this.helper.AddOperator("Test Estate", "Safaricom");
        await this.helper.AddOperator("Test Estate", "Voucher");

        var merchantId = await this.helper.AddMerchant("Test Estate", $"Test Merchant 1",100, DateTime.Now, DateTime.Now,
            ("Address Line 1", $"Test Town", $"TE57 1NG", $"Region"),
            ("Contact 1", "1@2.com", "123456"), operators: ["Safaricom", "Voucher"]);

        Result<List<MerchantOperator>> result = await this.CreateAndSendHttpRequestMessage<List<MerchantOperator>>($"{this.BaseRoute}/{merchantId}/operators", CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        var merchantOperators = result.Data;

        merchantOperators.ShouldNotBeNull();
        merchantOperators.Count.ShouldBe(2);
        merchantOperators.SingleOrDefault(m => m.OperatorName == "Safaricom").ShouldNotBeNull();
        merchantOperators.SingleOrDefault(m => m.OperatorName == "Voucher").ShouldNotBeNull();
    }

    [Fact]
    public async Task MerchantEndpoint_GetMerchantContracts_MerchantContractsReturned()
    {
        await this.helper.AddEstate("Test Estate", "Ref1");

        await this.helper.AddOperator("Test Estate", "Safaricom");
        await this.helper.AddOperator("Test Estate", "Voucher");

        List<(string productName, int productType, decimal? value)> safaricomProductList = new() { ("200 KES Topup", 0, 200.00m), ("100 KES Topup", 0, 100.00m), ("50 KES Topup", 0, 50.00m), ("Custom", 0, null) };
        await this.helper.AddContractWithProducts("Test Estate", "Safaricom Contract", "Safaricom", safaricomProductList);

        List<(string productName, int productType, decimal? value)> voucherProductList = new() { ("10 KES Voucher", 0, 10.00m), ("Custom", 0, null) };
        await this.helper.AddContractWithProducts("Test Estate", "Healthcare Centre 1 Contract", "Voucher", voucherProductList);

        var merchantId = await this.helper.AddMerchant("Test Estate", $"Test Merchant 1", 100, DateTime.Now, DateTime.Now,
            ("Address Line 1", $"Test Town", $"TE57 1NG", $"Region"),
            ("Contact 1", "1@2.com", "123456"), operators: ["Safaricom", "Voucher"],
            ["Safaricom Contract", "Healthcare Centre 1 Contract"]);

        Result<List<MerchantContract>> result = await this.CreateAndSendHttpRequestMessage<List<MerchantContract>>($"{this.BaseRoute}/{merchantId}/contracts", CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        var merchantContracts = result.Data;

        merchantContracts.ShouldNotBeNull();
        merchantContracts.Count.ShouldBe(2);
        merchantContracts.SingleOrDefault(m => m.ContractName == "Safaricom Contract").ShouldNotBeNull();
        merchantContracts.SingleOrDefault(m => m.ContractName == "Healthcare Centre 1 Contract").ShouldNotBeNull();
    }

    [Fact]
    public async Task MerchantEndpoint_GetMerchantDevices_MerchantDevicesReturned()
    {
        await this.helper.AddEstate("Test Estate", "Ref1");

        var merchantId = await this.helper.AddMerchant("Test Estate", $"Test Merchant 1",100, DateTime.Now, DateTime.Now,
            ("Address Line 1", $"Test Town", $"TE57 1NG", $"Region"),
            ("Contact 1", "1@2.com", "123456"), devices: ["123456"]);

        Result<List<MerchantDevice>> result = await this.CreateAndSendHttpRequestMessage<List<MerchantDevice>>($"{this.BaseRoute}/{merchantId}/devices", CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        var merchantDevices = result.Data;

        merchantDevices.ShouldNotBeNull();
        merchantDevices.Count.ShouldBe(1);
        merchantDevices.SingleOrDefault(m => m.DeviceIdentifier == "123456").ShouldNotBeNull();
    }

    [Fact]
    public async Task MerchantEndpoint_GetMerchantOpeningHours_MerchantOpeningHoursReturned()
    {
        await this.helper.AddEstate("Test Estate", "Ref1");

        Dictionary<DayOfWeek, Models.MerchantOpeningHour> openingHours = new();
        openingHours.Add(DayOfWeek.Monday, new Models.MerchantOpeningHour() { DayOfWeek = DayOfWeek.Monday, OpeningTime = "09:00", ClosingTime = "17:00" });
        openingHours.Add(DayOfWeek.Tuesday, new Models.MerchantOpeningHour() { DayOfWeek = DayOfWeek.Tuesday, OpeningTime = "09:00", ClosingTime = "17:00" });
        openingHours.Add(DayOfWeek.Wednesday, new Models.MerchantOpeningHour() { DayOfWeek = DayOfWeek.Wednesday, OpeningTime = "09:00", ClosingTime = "17:00" });
        openingHours.Add(DayOfWeek.Thursday, new Models.MerchantOpeningHour() { DayOfWeek = DayOfWeek.Thursday, OpeningTime = "09:00", ClosingTime = "17:00" });
        openingHours.Add(DayOfWeek.Friday, new Models.MerchantOpeningHour() { DayOfWeek = DayOfWeek.Friday, OpeningTime = "09:00", ClosingTime = "17:00" });
        openingHours.Add(DayOfWeek.Saturday, new Models.MerchantOpeningHour() { DayOfWeek = DayOfWeek.Saturday, OpeningTime = "10:00", ClosingTime = "16:00" });
        openingHours.Add(DayOfWeek.Sunday, new Models.MerchantOpeningHour() { DayOfWeek = DayOfWeek.Sunday, OpeningTime = "09:00", ClosingTime = "18:00" });
        
        var merchantId = await this.helper.AddMerchant("Test Estate", $"Test Merchant 1", 100, DateTime.Now, DateTime.Now,
            ("Address Line 1", $"Test Town", $"TE57 1NG", $"Region"),
            ("Contact 1", "1@2.com", "123456"), devices: ["123456"],
            openingHours:openingHours);
        
        Result<List<MerchantOpeningHour>> result = await this.CreateAndSendHttpRequestMessage<List<MerchantOpeningHour>>($"{this.BaseRoute}/{merchantId}/openinghours", CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        var merchantOpeningHours = result.Data;

        merchantOpeningHours.ShouldNotBeNull();
        merchantOpeningHours.Count.ShouldBe(7);
        merchantOpeningHours.SingleOrDefault(m => m.DayOfWeek == DayOfWeek.Monday && m.OpeningTime == "09:00" && m.ClosingTime == "17:00").ShouldNotBeNull();
        merchantOpeningHours.SingleOrDefault(m => m.DayOfWeek == DayOfWeek.Tuesday && m.OpeningTime == "09:00" && m.ClosingTime == "17:00").ShouldNotBeNull();
        merchantOpeningHours.SingleOrDefault(m => m.DayOfWeek == DayOfWeek.Wednesday && m.OpeningTime == "09:00" && m.ClosingTime == "17:00").ShouldNotBeNull();
        merchantOpeningHours.SingleOrDefault(m => m.DayOfWeek == DayOfWeek.Thursday && m.OpeningTime == "09:00" && m.ClosingTime == "17:00").ShouldNotBeNull();
        merchantOpeningHours.SingleOrDefault(m => m.DayOfWeek == DayOfWeek.Friday && m.OpeningTime == "09:00" && m.ClosingTime == "17:00").ShouldNotBeNull();
        merchantOpeningHours.SingleOrDefault(m => m.DayOfWeek == DayOfWeek.Saturday && m.OpeningTime == "10:00" && m.ClosingTime == "16:00").ShouldNotBeNull();
        merchantOpeningHours.SingleOrDefault(m => m.DayOfWeek == DayOfWeek.Sunday && m.OpeningTime == "09:00" && m.ClosingTime == "18:00").ShouldNotBeNull();
    }

    [Fact]
    public async Task MerchantEndpoint_GetMerchantSchedule_MerchantScheduleReturned()
    {
        await this.helper.AddEstate("Test Estate", "Ref1");
        
        var merchantId = await this.helper.AddMerchant("Test Estate", $"Test Merchant 1", 100, DateTime.Now, DateTime.Now,
            ("Address Line 1", $"Test Town", $"TE57 1NG", $"Region"),
            ("Contact 1", "1@2.com", "123456"), devices: ["123456"]);

        Result<MerchantScheduleResponse> result = await this.CreateAndSendHttpRequestMessage<MerchantScheduleResponse>($"{this.BaseRoute}/{merchantId}/schedule/2026", CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        var merchantSchedule = result.Data;

        merchantSchedule.ShouldNotBeNull();
        merchantSchedule.Year.ShouldBe(2026);
        merchantSchedule.Months.ShouldNotBeNull();
        merchantSchedule.Months.Count.ShouldBe(3);
        merchantSchedule.Months.SingleOrDefault(m => m.Month == 1).ShouldNotBeNull();
        merchantSchedule.Months.SingleOrDefault(m => m.Month == 1).ClosedDays.Contains(1).ShouldBeTrue();
        merchantSchedule.Months.SingleOrDefault(m => m.Month == 1).ClosedDays.Contains(2).ShouldBeTrue();
        merchantSchedule.Months.SingleOrDefault(m => m.Month == 1).ClosedDays.Contains(15).ShouldBeTrue();

        merchantSchedule.Months.SingleOrDefault(m => m.Month == 2).ShouldNotBeNull();
        merchantSchedule.Months.SingleOrDefault(m => m.Month == 2).ClosedDays.Contains(10).ShouldBeTrue();
        merchantSchedule.Months.SingleOrDefault(m => m.Month == 2).ClosedDays.Contains(14).ShouldBeTrue();
        merchantSchedule.Months.SingleOrDefault(m => m.Month == 2).ClosedDays.Contains(28).ShouldBeTrue();

        merchantSchedule.Months.SingleOrDefault(m => m.Month == 12).ShouldNotBeNull();
        merchantSchedule.Months.SingleOrDefault(m => m.Month == 12).ClosedDays.Contains(24).ShouldBeTrue();
        merchantSchedule.Months.SingleOrDefault(m => m.Month == 12).ClosedDays.Contains(25).ShouldBeTrue();
        merchantSchedule.Months.SingleOrDefault(m => m.Month == 12).ClosedDays.Contains(26).ShouldBeTrue();
        merchantSchedule.Months.SingleOrDefault(m => m.Month == 12).ClosedDays.Contains(31).ShouldBeTrue();
    }

    [Fact]
    public async Task MerchantEndpoint_GetMerchantKpis_MerchantKpisReturned()
    {
        DateTime now = DateTime.Now;

        await this.helper.AddEstate("Test Estate", "Ref1");
        
        await this.helper.AddMerchant("Test Estate", $"Test Merchant 1",100, now, now,
            ("Address Line 1", $"Test Town", $"TE57 1NG", $"Region"),
            ("Contact 1", "1@2.com", "123456"), devices: ["123456"]);
        await this.helper.AddMerchant("Test Estate", $"Test Merchant 2", 200, now, now,
            ("Address Line 1", $"Test Town", $"TE57 1NG", $"Region"),
            ("Contact 1", "1@2.com", "123456"), devices: ["123456"]);
        await this.helper.AddMerchant("Test Estate", $"Test Merchant 3",300, now, now.AddDays(-1),
            ("Address Line 1", $"Test Town", $"TE57 1NG", $"Region"),
            ("Contact 1", "1@2.com", "123456"), devices: ["123456"]);
        await this.helper.AddMerchant("Test Estate", $"Test Merchant 4",400, now, now.AddDays(-2),
            ("Address Line 1", $"Test Town", $"TE57 1NG", $"Region"),
            ("Contact 1", "1@2.com", "123456"), devices: ["123456"]);
        await this.helper.AddMerchant("Test Estate", $"Test Merchant 5",500, now, now.AddDays(-3),
            ("Address Line 1", $"Test Town", $"TE57 1NG", $"Region"),
            ("Contact 1", "1@2.com", "123456"), devices: ["123456"]);
        await this.helper.AddMerchant("Test Estate", $"Test Merchant 6",600, now, now.AddDays(-10),
            ("Address Line 1", $"Test Town", $"TE57 1NG", $"Region"),
            ("Contact 1", "1@2.com", "123456"), devices: ["123456"]);

        Result<MerchantKpi> result = await this.CreateAndSendHttpRequestMessage<MerchantKpi>($"{this.BaseRoute}/kpis", CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        var merchantKpis = result.Data;

        merchantKpis.MerchantsWithSaleInLastHour.ShouldBe(2);
        merchantKpis.MerchantsWithNoSaleToday.ShouldBe(3);
        merchantKpis.MerchantsWithNoSaleInLast7Days.ShouldBe(1);
    }


    protected override async Task ClearStandingData() {

    }

    protected override async Task SetupStandingData() {

    }
}
