using EstateReportingAPI.DataTransferObjects;
using EstateReportingAPI.DataTrasferObjects;
using EstateReportingAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using SimpleResults;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TransactionProcessor.Database.Contexts;
using TransactionProcessor.Database.Entities;
using Xunit;
using Xunit.Abstractions;
using Contract = EstateReportingAPI.DataTransferObjects.Contract;
using Estate = EstateReportingAPI.DataTransferObjects.Estate;
using EstateOperator = EstateReportingAPI.DataTransferObjects.EstateOperator;
using Merchant = EstateReportingAPI.DataTransferObjects.Merchant;
using MerchantContract = EstateReportingAPI.DataTransferObjects.MerchantContract;
using MerchantDevice = EstateReportingAPI.DataTransferObjects.MerchantDevice;
using MerchantKpi = EstateReportingAPI.DataTransferObjects.MerchantKpi;
using MerchantOperator = EstateReportingAPI.DataTransferObjects.MerchantOperator;
using Operator = EstateReportingAPI.DataTransferObjects.Operator;
using TodaysSales = EstateReportingAPI.DataTransferObjects.TodaysSales;

namespace EstateReportingAPI.IntegrationTests {
    public class CalendarEndpointTests : ControllerTestsBase {
        private String BaseRoute = "api/calendars";

        [Fact]
        public async Task CalendarEndpoint_GetComparisonDates_DatesReturned() {
            List<DateTime> datesInPreviousYear = helper.GetDatesForYear(DateTime.Now.Year - 1);
            await helper.AddCalendarDates(datesInPreviousYear);
            List<DateTime> datesInYear = helper.GetDatesForYear(DateTime.Now.Year);
            await helper.AddCalendarDates(datesInYear);

            Result<List<ComparisonDate>> result = await this.CreateAndSendHttpRequestMessage<List<ComparisonDate>>($"{this.BaseRoute}/comparisondates", CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();

            List<ComparisonDate> dates = result.Data;
            List<DateTime> expectedDates = datesInYear.Where(d => d <= DateTime.Now.Date.AddDays(-1)).ToList();
            dates.ShouldNotBeNull();
            foreach (DateTime date in expectedDates) {
                dates.Select(d => d.Date).Contains(date.Date).ShouldBeTrue();
            }

            dates.Select(d => d.Description).Contains("Yesterday");
            dates.Select(d => d.Description).Contains("Last Week");
            dates.Select(d => d.Description).Contains("Last Month");
        }

        protected override async Task ClearStandingData() {

        }

        protected override async Task SetupStandingData() {

        }
    }

    public class ContractEndPointTests : ControllerTestsBase {
        private String BaseRoute = "api/contracts";

        [Fact]
        public async Task ContractEndpoint_GetRecentContracts_ContractsReturned() {
            await helper.AddEstate("Test Estate", "Ref1");

            await this.helper.AddOperator("Test Estate", "Safaricom");
            await this.helper.AddOperator("Test Estate", "Voucher");
            await this.helper.AddOperator("Test Estate", "PataPawa PostPay");
            await this.helper.AddOperator("Test Estate", "PataPawa PrePay");

            // Contracts & Products
            List<(string productName, int productType, decimal? value)> safaricomProductList = new() { ("200 KES Topup", 0, 200.00m), ("100 KES Topup", 0, 100.00m), ("50 KES Topup", 0, 50.00m), ("Custom", 0, null) };
            await helper.AddContractWithProducts("Test Estate", "Safaricom Contract", "Safaricom", safaricomProductList);

            List<(string productName, int productType, decimal? value)> voucherProductList = new() { ("10 KES Voucher", 0, 10.00m), ("Custom", 0, null) };
            await helper.AddContractWithProducts("Test Estate", "Healthcare Centre 1 Contract", "Voucher", voucherProductList);

            List<(string productName, int productType, decimal? value)> postPayProductList = new() { ("Post Pay Bill Pay", 0, null) };
            await helper.AddContractWithProducts("Test Estate", "PataPawa PostPay Contract", "PataPawa PostPay", postPayProductList);

            List<(string productName, int productType, decimal? value)> prePayProductList = new() { ("Pre Pay Bill Pay", 0, null) };
            await helper.AddContractWithProducts("Test Estate", "PataPawa PrePay Contract", "PataPawa PrePay", prePayProductList);

            Result<List<Contract>> result = await this.CreateAndSendHttpRequestMessage<List<Contract>>($"{this.BaseRoute}/recent", CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();

            List<Contract> contracts = result.Data;
            contracts.Count.ShouldBe(3);
            contracts.SingleOrDefault(c => c.Description == "Safaricom Contract").ShouldNotBeNull();
            contracts.SingleOrDefault(c => c.Description == "PataPawa PostPay Contract").ShouldNotBeNull();
            contracts.SingleOrDefault(c => c.Description == "PataPawa PrePay Contract").ShouldNotBeNull();

        }

        [Fact]
        public async Task ContractEndpoint_GetContracts_ContractsReturned() {
            await helper.AddEstate("Test Estate", "Ref1");

            await this.helper.AddOperator("Test Estate", "Safaricom");
            await this.helper.AddOperator("Test Estate", "Voucher");
            await this.helper.AddOperator("Test Estate", "PataPawa PostPay");
            await this.helper.AddOperator("Test Estate", "PataPawa PrePay");

            // Contracts & Products
            List<(string productName, int productType, decimal? value)> safaricomProductList = new() { ("200 KES Topup", 0, 200.00m), ("100 KES Topup", 0, 100.00m), ("50 KES Topup", 0, 50.00m), ("Custom", 0, null) };
            await helper.AddContractWithProducts("Test Estate", "Safaricom Contract", "Safaricom", safaricomProductList);

            List<(string productName, int productType, decimal? value)> voucherProductList = new() { ("10 KES Voucher", 0, 10.00m), ("Custom", 0, null) };
            await helper.AddContractWithProducts("Test Estate", "Healthcare Centre 1 Contract", "Voucher", voucherProductList);

            List<(string productName, int productType, decimal? value)> postPayProductList = new() { ("Post Pay Bill Pay", 0, null) };
            await helper.AddContractWithProducts("Test Estate", "PataPawa PostPay Contract", "PataPawa PostPay", postPayProductList);

            List<(string productName, int productType, decimal? value)> prePayProductList = new() { ("Pre Pay Bill Pay", 0, null) };
            await helper.AddContractWithProducts("Test Estate", "PataPawa PrePay Contract", "PataPawa PrePay", prePayProductList);

            Result<List<Contract>> result = await this.CreateAndSendHttpRequestMessage<List<Contract>>($"{this.BaseRoute}", CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();

            List<Contract> contracts = result.Data;
            contracts.Count.ShouldBe(4);
            contracts.SingleOrDefault(c => c.Description == "Safaricom Contract").ShouldNotBeNull();
            contracts.SingleOrDefault(c => c.Description == "Healthcare Centre 1 Contract").ShouldNotBeNull();
            contracts.SingleOrDefault(c => c.Description == "PataPawa PostPay Contract").ShouldNotBeNull();
            contracts.SingleOrDefault(c => c.Description == "PataPawa PrePay Contract").ShouldNotBeNull();

        }

        [Fact]
        public async Task ContractEndpoint_GetContract_ContractReturned() {
            await helper.AddEstate("Test Estate", "Ref1");

            await this.helper.AddOperator("Test Estate", "Safaricom");
            await this.helper.AddOperator("Test Estate", "Voucher");
            await this.helper.AddOperator("Test Estate", "PataPawa PostPay");
            await this.helper.AddOperator("Test Estate", "PataPawa PrePay");

            // Contracts & Products
            List<(string productName, int productType, decimal? value)> safaricomProductList = new() { ("200 KES Topup", 0, 200.00m), ("100 KES Topup", 0, 100.00m), ("50 KES Topup", 0, 50.00m), ("Custom", 0, null) };
            await helper.AddContractWithProducts("Test Estate", "Safaricom Contract", "Safaricom", safaricomProductList);

            List<(string productName, int productType, decimal? value)> voucherProductList = new() { ("10 KES Voucher", 0, 10.00m), ("Custom", 0, null) };
            await helper.AddContractWithProducts("Test Estate", "Healthcare Centre 1 Contract", "Voucher", voucherProductList);

            List<(string productName, int productType, decimal? value)> postPayProductList = new() { ("Post Pay Bill Pay", 0, null) };
            await helper.AddContractWithProducts("Test Estate", "PataPawa PostPay Contract", "PataPawa PostPay", postPayProductList);

            List<(string productName, int productType, decimal? value)> prePayProductList = new() { ("Pre Pay Bill Pay", 0, null) };
            var ppprepayContractId = await helper.AddContractWithProducts("Test Estate", "PataPawa PrePay Contract", "PataPawa PrePay", prePayProductList);

            Result<Contract> result = await this.CreateAndSendHttpRequestMessage<Contract>($"{this.BaseRoute}/{ppprepayContractId}", CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();

            Contract contract = result.Data;
            contract.ShouldNotBeNull();
            contract.Description.ShouldBe("PataPawa PrePay Contract");

        }

        protected override async Task ClearStandingData() {

        }

        protected override async Task SetupStandingData() {

        }
    }

    public class EstateEndpointTests : ControllerTestsBase {
        private String BaseRoute = "api/estates";

        [Fact]
        public async Task EstateEndpoint_GetEstates_EstateReturned() {
            await helper.AddEstate("Test Estate", "Ref1");

            Result<Estate> result = await this.CreateAndSendHttpRequestMessage<Estate>($"{this.BaseRoute}", CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();

            Estate estate = result.Data;
            estate.ShouldNotBeNull();
            estate.EstateName.ShouldBe("Test Estate");
            estate.Reference.ShouldBe("Ref1");
        }

        [Fact]
        public async Task EstateEndpoint_GetEstateOperator_EstateOperatorsReturned() {
            await helper.AddEstate("Test Estate", "Ref1");
            await this.helper.AddOperator("Test Estate", "Safaricom");
            await this.helper.AddOperator("Test Estate", "Voucher");

            await this.helper.AddEstateOperators("Test Estate", ["Safaricom", "Voucher"]);

            Result<List<EstateOperator>> result = await this.CreateAndSendHttpRequestMessage<List<EstateOperator>>($"{this.BaseRoute}/operators", CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();

            List<EstateOperator> estateOperators = result.Data;
            estateOperators.Count.ShouldBe(2);
            estateOperators.SingleOrDefault(e => e.Name == "Safaricom").ShouldNotBeNull();
            estateOperators.SingleOrDefault(e => e.Name == "Voucher").ShouldNotBeNull();
        }

        protected override async Task ClearStandingData() {

        }

        protected override async Task SetupStandingData() {

        }
    }

    public class OperatorEndpointTests : ControllerTestsBase {
        private String BaseRoute = "api/operators";

        [Fact]
        public async Task OperatorEndpoint_GetOperators_OperatorsReturned() {
            await helper.AddEstate("Test Estate", "Ref1");
            await this.helper.AddOperator("Test Estate", "Safaricom");
            await this.helper.AddOperator("Test Estate", "Voucher");

            Result<List<Operator>> result = await this.CreateAndSendHttpRequestMessage<List<Operator>>($"{this.BaseRoute}", CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            List<Operator> operators = result.Data;
            operators.Count.ShouldBe(2);
            operators.SingleOrDefault(o => o.Name == "Safaricom").ShouldNotBeNull();
            operators.SingleOrDefault(o => o.Name == "Voucher").ShouldNotBeNull();
        }

        [Fact]
        public async Task OperatorEndpoint_GetOperator_OperatorReturned() {
            await helper.AddEstate("Test Estate", "Ref1");
            var operatorId = await this.helper.AddOperator("Test Estate", "Safaricom");
            Result<Operator> result = await this.CreateAndSendHttpRequestMessage<Operator>($"{this.BaseRoute}/{operatorId}", CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            Operator operatorData = result.Data;
            operatorData.ShouldNotBeNull();
            operatorData.Name.ShouldBe("Safaricom");
        }

        protected override async Task ClearStandingData() {

        }

        protected override async Task SetupStandingData() {

        }
    }

    public class MerchantEndpointTests : ControllerTestsBase {
        private String BaseRoute = "api/merchants";

        [Fact]
        public async Task MerchantEndpoint_GetMerchants_MerchantsReturned() {
            await helper.AddEstate("Test Estate", "Ref1");
            for (int i = 0; i < 10; i++) {
                await helper.AddMerchant("Test Estate", $"Test Merchant {i}", DateTime.Now, DateTime.Now,
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
            }
        }

        [Fact]
        public async Task MerchantEndpoint_GetMerchant_MerchantReturned()
        {
            await helper.AddEstate("Test Estate", "Ref1");
            var merchantId = await helper.AddMerchant("Test Estate", $"Test Merchant 1", DateTime.Now, DateTime.Now,
                    ("Address Line 1", $"Test Town", $"TE57 1NG", $"Region"), 
                    ("Contact 1", "1@2.com", "123456"));

            Result<Merchant> result = await this.CreateAndSendHttpRequestMessage<Merchant>($"{this.BaseRoute}/{merchantId}", CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            var merchant = result.Data;

            merchant.ShouldNotBeNull();
            merchant.Name.ShouldBe("Test Merchant 1");
        }

        [Fact]
        public async Task MerchantEndpoint_GetRecentMerchants_MerchantsReturned()
        {
            await helper.AddEstate("Test Estate", "Ref1");
            for (int i = 0; i < 10; i++)
            {
                await helper.AddMerchant("Test Estate", $"Test Merchant {i}", DateTime.Now.AddDays(i*-1), DateTime.Now,
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
            await helper.AddEstate("Test Estate", "Ref1");

            await this.helper.AddOperator("Test Estate", "Safaricom");
            await this.helper.AddOperator("Test Estate", "Voucher");

            var merchantId = await helper.AddMerchant("Test Estate", $"Test Merchant 1", DateTime.Now, DateTime.Now,
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
            await helper.AddEstate("Test Estate", "Ref1");

            await this.helper.AddOperator("Test Estate", "Safaricom");
            await this.helper.AddOperator("Test Estate", "Voucher");

            List<(string productName, int productType, decimal? value)> safaricomProductList = new() { ("200 KES Topup", 0, 200.00m), ("100 KES Topup", 0, 100.00m), ("50 KES Topup", 0, 50.00m), ("Custom", 0, null) };
            await helper.AddContractWithProducts("Test Estate", "Safaricom Contract", "Safaricom", safaricomProductList);

            List<(string productName, int productType, decimal? value)> voucherProductList = new() { ("10 KES Voucher", 0, 10.00m), ("Custom", 0, null) };
            await helper.AddContractWithProducts("Test Estate", "Healthcare Centre 1 Contract", "Voucher", voucherProductList);

            var merchantId = await helper.AddMerchant("Test Estate", $"Test Merchant 1", DateTime.Now, DateTime.Now,
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
            await helper.AddEstate("Test Estate", "Ref1");

            var merchantId = await helper.AddMerchant("Test Estate", $"Test Merchant 1", DateTime.Now, DateTime.Now,
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
        public async Task MerchantEndpoint_GetMerchantKpis_MerchantKpisReturned()
        {
            await helper.AddEstate("Test Estate", "Ref1");

            await helper.AddMerchant("Test Estate", $"Test Merchant 1", DateTime.Now, DateTime.Now,
                ("Address Line 1", $"Test Town", $"TE57 1NG", $"Region"),
                ("Contact 1", "1@2.com", "123456"), devices: ["123456"]);
            await helper.AddMerchant("Test Estate", $"Test Merchant 2", DateTime.Now, DateTime.Now.AddMinutes(-10),
                ("Address Line 1", $"Test Town", $"TE57 1NG", $"Region"),
                ("Contact 1", "1@2.com", "123456"), devices: ["123456"]);
            await helper.AddMerchant("Test Estate", $"Test Merchant 3", DateTime.Now, DateTime.Now.AddHours(-2),
                ("Address Line 1", $"Test Town", $"TE57 1NG", $"Region"),
                ("Contact 1", "1@2.com", "123456"), devices: ["123456"]);
            await helper.AddMerchant("Test Estate", $"Test Merchant 4", DateTime.Now, DateTime.Now.AddHours(-3),
                ("Address Line 1", $"Test Town", $"TE57 1NG", $"Region"),
                ("Contact 1", "1@2.com", "123456"), devices: ["123456"]);
            await helper.AddMerchant("Test Estate", $"Test Merchant 5", DateTime.Now, DateTime.Now.AddDays(-2),
                ("Address Line 1", $"Test Town", $"TE57 1NG", $"Region"),
                ("Contact 1", "1@2.com", "123456"), devices: ["123456"]);
            await helper.AddMerchant("Test Estate", $"Test Merchant 6", DateTime.Now, DateTime.Now.AddDays(-1),
                ("Address Line 1", $"Test Town", $"TE57 1NG", $"Region"),
                ("Contact 1", "1@2.com", "123456"), devices: ["123456"]);
            await helper.AddMerchant("Test Estate", $"Test Merchant 7", DateTime.Now, DateTime.Now.AddDays(-3),
                ("Address Line 1", $"Test Town", $"TE57 1NG", $"Region"),
                ("Contact 1", "1@2.com", "123456"), devices: ["123456"]);
            await helper.AddMerchant("Test Estate", $"Test Merchant 8", DateTime.Now, DateTime.Now.AddDays(-10),
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

    public class TransactionsEndpointTests : ControllerTestsBase {
        private String BaseRoute = "api/transactions";

        public TransactionsEndpointTests(ITestOutputHelper testOutputHelper) {
            this.TestOutputHelper = testOutputHelper;
        }


        protected override async Task ClearStandingData() {
            throw new NotImplementedException();
        }

        protected override async Task SetupStandingData() {
            Stopwatch sw = Stopwatch.StartNew();
            this.TestOutputHelper.WriteLine("Setting up standing data");

            // Estates
            await helper.AddEstate("Test Estate", "Ref1");
            sw.Stop();
            this.TestOutputHelper.WriteLine($"Setup Estate {sw.ElapsedMilliseconds}ms");
            sw.Restart();
            
            // Operators
            await this.helper.AddOperator("Test Estate", "Safaricom");
            await this.helper.AddOperator("Test Estate", "Voucher");
            await this.helper.AddOperator("Test Estate", "PataPawa PostPay");
            await this.helper.AddOperator("Test Estate", "PataPawa PrePay");

            sw.Stop();
            this.TestOutputHelper.WriteLine($"Setup Operators {sw.ElapsedMilliseconds}ms");
            sw.Restart();
            // Merchants
            await helper.AddMerchant("Test Estate", "Test Merchant 1", DateTime.MinValue, DateTime.MinValue, default, default);
            await helper.AddMerchant("Test Estate", "Test Merchant 2", DateTime.MinValue, DateTime.MinValue, default, default);
            await helper.AddMerchant("Test Estate", "Test Merchant 3", DateTime.MinValue, DateTime.MinValue, default, default);
            await helper.AddMerchant("Test Estate", "Test Merchant 4", DateTime.MinValue, DateTime.MinValue, default, default);
            sw.Stop();
            this.TestOutputHelper.WriteLine($"Setup Merchants {sw.ElapsedMilliseconds}ms");
            sw.Restart();

            // Contracts & Products
            List<(string productName, int productType, decimal? value)> safaricomProductList = new(){
                                                                                                      ("200 KES Topup", 0, 200.00m),
                                                                                                      ("100 KES Topup", 0, 100.00m),
                                                                                                      ("50 KES Topup", 0, 50.00m),
                                                                                                      ("Custom", 0, null)
                                                                                                  };
            await helper.AddContractWithProducts("Test Estate", "Safaricom Contract", "Safaricom", safaricomProductList);

            List<(string productName, int productType, decimal? value)> voucherProductList = new(){
                                                                                                    ("10 KES Voucher", 0, 10.00m),
                                                                                                    ("Custom", 0, null)
                                                                                                };
            await helper.AddContractWithProducts("Test Estate", "Healthcare Centre 1 Contract", "Voucher", voucherProductList);

            List<(string productName, int productType, decimal? value)> postPayProductList = new(){
                                                                                                    ("Post Pay Bill Pay", 0, null)
                                                                                                };
            await helper.AddContractWithProducts("Test Estate", "PataPawa PostPay Contract", "PataPawa PostPay", postPayProductList);

            List<(string productName, int productType, decimal? value)> prePayProductList = new(){
                                                                                                   ("Pre Pay Bill Pay", 0, null)
                                                                                               };
            await helper.AddContractWithProducts("Test Estate", "PataPawa PrePay Contract", "PataPawa PrePay", prePayProductList);

            sw.Stop();
            this.TestOutputHelper.WriteLine($"Setup Contracts {sw.ElapsedMilliseconds}ms");
            sw.Restart();

            // Response Codes
            await helper.AddResponseCode(0, "Success");
            await helper.AddResponseCode(1000, "Unknown Device");
            await helper.AddResponseCode(1001, "Unknown Estate");
            await helper.AddResponseCode(1002, "Unknown Merchant");
            await helper.AddResponseCode(1003, "No Devices Configured");

            sw.Stop();
            this.TestOutputHelper.WriteLine($"Setup Response Codes {sw.ElapsedMilliseconds}ms");
            sw.Restart();

            merchantsList = context.Merchants.Select(m => m).ToList();

            contractList = context.Contracts
                .Join(
                    context.Operators,
                    c => c.OperatorId,
                    o => o.OperatorId,
                    (c, o) => new { c.ContractId, c.Description, o.OperatorId, o.Name }
                )
                .ToList().Select(x => (x.ContractId, x.Description, x.OperatorId, x.Name))
                .ToList();

            var query1 = context.Contracts
                .GroupJoin(
                    context.ContractProducts,
                    c => c.ContractId,
                    cp => cp.ContractId,
                    (c, productGroup) => new
                    {
                        c.ContractId,
                        Products = productGroup.Select(p => new { p.ContractProductId, p.ProductName, p.Value })
                            .OrderBy(p => p.ContractProductId)
                            .Select(p => new { p.ContractProductId, p.ProductName, p.Value })
                            .ToList()
                    })
                .ToList();

            contractProducts = query1.ToDictionary(
                item => item.ContractId,
                item => item.Products.Select(i => (i.ContractProductId, i.ProductName, i.Value)).ToList()
            );


            sw.Stop();
            this.TestOutputHelper.WriteLine($"Data Caching {sw.ElapsedMilliseconds}ms");
            sw.Restart();
        }

        [Fact]
        public async Task FactTransactionsControllerController_TodaysSales_SalesReturned()
        {
            List<Transaction>? todaysTransactions = new List<Transaction>();
            List<Transaction> comparisonDateTransactions = new List<Transaction>();

            DateTime todaysDateTime = DateTime.Now;
            DateTime comparisonDate = DateTime.Now.AddDays(-1).AddHours(-1);

            Dictionary<string, int> transactionCounts = new() { { "Test Merchant 1", 15 }, { "Test Merchant 2", 18 }, { "Test Merchant 3", 9 }, { "Test Merchant 4", 0 } };

            foreach (var merchant in merchantsList)
            {
                foreach (var contract in contractList)
                {
                    var productList = contractProducts.Single(cp => cp.Key == contract.contractId).Value;
                    foreach ((Guid productId, String productName, Decimal? productValue) product in productList)
                    {
                        var transactionCount = transactionCounts.Single(m => m.Key == merchant.Name).Value;
                        for (int i = 0; i < transactionCount; i++)
                        {
                            Transaction transaction = await helper.BuildTransactionX(todaysDateTime.AddHours(-1), merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", product.productValue);
                            todaysTransactions.Add(transaction);
                        }
                    }
                }
            }

            await this.helper.AddTransactionsX(todaysTransactions);

            // Comparison Date sales
            foreach (var merchant in merchantsList)
            {
                foreach (var contract in contractList)
                {
                    var productList = contractProducts.Single(cp => cp.Key == contract.contractId).Value;
                    foreach ((Guid productId, String productName, Decimal? productValue) product in productList)
                    {
                        var transactionCount = transactionCounts.Single(m => m.Key == merchant.Name).Value;
                        for (int i = 0; i < transactionCount; i++)
                        {
                            Transaction transaction = await helper.BuildTransactionX(comparisonDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", product.productValue);
                            comparisonDateTransactions.Add(transaction);
                        }
                    }
                }
            }

            await this.helper.AddTransactionsX(comparisonDateTransactions);

            await helper.RunTodaysTransactionsSummaryProcessing(comparisonDate.Date);
            await helper.RunHistoricTransactionsSummaryProcessing(comparisonDate.Date);
            await helper.RunTodaysTransactionsSummaryProcessing(todaysDateTime.Date);

            Result<TodaysSales> result = await this.CreateAndSendHttpRequestMessage<TodaysSales>($"{this.BaseRoute}/todayssales?comparisonDate={comparisonDate:yyyy-MM-dd}", CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            var todaysSales = result.Data;

            todaysSales.ShouldNotBeNull();
            todaysSales.ComparisonSalesCount.ShouldBe(comparisonDateTransactions.Count);
            todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Sum(c => c.TransactionAmount));

            todaysSales.TodaysSalesCount.ShouldBe(todaysTransactions.Count);
            todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Sum(c => c.TransactionAmount));
        }

        
        [Fact]
        public async Task FactTransactionsControllerController_TodaysSales_OperatorFilter_SalesReturned()
        {
            List<Transaction>? todaysTransactions = new List<Transaction>();
           List<Transaction> comparisonDateTransactions = new List<Transaction>();
           
           DateTime todaysDateTime = DateTime.Now;
           DateTime comparisonDate = DateTime.Now.AddDays(-1).AddHours(-1);
           
           Dictionary<string, int> transactionCounts = new() { { "Test Merchant 1", 15 }, { "Test Merchant 2", 18 }, { "Test Merchant 3", 9 }, { "Test Merchant 4", 0 } };
           
           foreach (var merchant in merchantsList)
           {
               foreach (var contract in contractList)
               {
                   var productList = contractProducts.Single(cp => cp.Key == contract.contractId).Value;
                   foreach ((Guid productId, String productName, Decimal? productValue) product in productList)
                   {
                       var transactionCount = transactionCounts.Single(m => m.Key == merchant.Name).Value;
                       for (int i = 0; i < transactionCount; i++)
                       {
                           Transaction transaction = await helper.BuildTransactionX(todaysDateTime.AddHours(-1), merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", product.productValue);
                           todaysTransactions.Add(transaction);
                       }
                   }
               }
           }
           
           await this.helper.AddTransactionsX(todaysTransactions);
           
           // Comparison Date sales
           foreach (var merchant in merchantsList)
           {
               foreach (var contract in contractList)
               {
                   var productList = contractProducts.Single(cp => cp.Key == contract.contractId).Value;
                   foreach ((Guid productId, String productName, Decimal? productValue) product in productList)
                   {
                       var transactionCount = transactionCounts.Single(m => m.Key == merchant.Name).Value;
                       for (int i = 0; i < transactionCount; i++)
                       {
                           Transaction transaction = await helper.BuildTransactionX(comparisonDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", product.productValue);
                           comparisonDateTransactions.Add(transaction);
                       }
                   }
               }
           }
           
           await this.helper.AddTransactionsX(comparisonDateTransactions);
           
           await helper.RunTodaysTransactionsSummaryProcessing(comparisonDate.Date);
           await helper.RunHistoricTransactionsSummaryProcessing(comparisonDate.Date);
           await helper.RunTodaysTransactionsSummaryProcessing(todaysDateTime.Date);
           
           Result<TodaysSales> result = await this.CreateAndSendHttpRequestMessage<TodaysSales>($"{this.BaseRoute}/todayssales?comparisonDate={comparisonDate:yyyy-MM-dd}&operatorReportingId=1", CancellationToken.None);
           result.IsSuccess.ShouldBeTrue();
           var todaysSales = result.Data;

            var operatorId = await this.helper.GetOperatorId(1, CancellationToken.None);
            todaysSales.ComparisonSalesCount.ShouldBe(comparisonDateTransactions.Count(c => c.OperatorId == operatorId));
            todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(c => c.OperatorId == operatorId).Sum(c => c.TransactionAmount));

            todaysSales.TodaysSalesCount.ShouldBe(todaysTransactions.Count(c => c.OperatorId == operatorId));
            todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(c => c.OperatorId == operatorId).Sum(c => c.TransactionAmount));
        }

        
        [Fact]
        public async Task FactTransactionsControllerController_TodaysSales_MerchantFilter_SalesReturned()
        {
             List<Transaction>? todaysTransactions = new List<Transaction>();
           List<Transaction> comparisonDateTransactions = new List<Transaction>();
           
           DateTime todaysDateTime = DateTime.Now;
           DateTime comparisonDate = DateTime.Now.AddDays(-1).AddHours(-1);
           
           Dictionary<string, int> transactionCounts = new() { { "Test Merchant 1", 15 }, { "Test Merchant 2", 18 }, { "Test Merchant 3", 9 }, { "Test Merchant 4", 0 } };
           
           foreach (var merchant in merchantsList)
           {
               foreach (var contract in contractList)
               {
                   var productList = contractProducts.Single(cp => cp.Key == contract.contractId).Value;
                   foreach ((Guid productId, String productName, Decimal? productValue) product in productList)
                   {
                       var transactionCount = transactionCounts.Single(m => m.Key == merchant.Name).Value;
                       for (int i = 0; i < transactionCount; i++)
                       {
                           Transaction transaction = await helper.BuildTransactionX(todaysDateTime.AddHours(-1), merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", product.productValue);
                           todaysTransactions.Add(transaction);
                       }
                   }
               }
           }
           
           await this.helper.AddTransactionsX(todaysTransactions);
           
           // Comparison Date sales
           foreach (var merchant in merchantsList)
           {
               foreach (var contract in contractList)
               {
                   var productList = contractProducts.Single(cp => cp.Key == contract.contractId).Value;
                   foreach ((Guid productId, String productName, Decimal? productValue) product in productList)
                   {
                       var transactionCount = transactionCounts.Single(m => m.Key == merchant.Name).Value;
                       for (int i = 0; i < transactionCount; i++)
                       {
                           Transaction transaction = await helper.BuildTransactionX(comparisonDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", product.productValue);
                           comparisonDateTransactions.Add(transaction);
                       }
                   }
               }
           }
           
           await this.helper.AddTransactionsX(comparisonDateTransactions);
           
           await helper.RunTodaysTransactionsSummaryProcessing(comparisonDate.Date);
           await helper.RunHistoricTransactionsSummaryProcessing(comparisonDate.Date);
           await helper.RunTodaysTransactionsSummaryProcessing(todaysDateTime.Date);
           
           Result<TodaysSales> result = await this.CreateAndSendHttpRequestMessage<TodaysSales>($"{this.BaseRoute}/todayssales?comparisonDate={comparisonDate:yyyy-MM-dd}&merchantReportingId=1", CancellationToken.None);
           result.IsSuccess.ShouldBeTrue();
           var todaysSales = result.Data;

            var merchantId = await this.helper.GetMerchantId(1, CancellationToken.None);
            todaysSales.ComparisonSalesCount.ShouldBe(comparisonDateTransactions.Count(c => c.MerchantId == merchantId));
            todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(c => c.MerchantId == merchantId).Sum(c => c.TransactionAmount));

            todaysSales.TodaysSalesCount.ShouldBe(todaysTransactions.Count(c => c.MerchantId == merchantId));
            todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(c => c.MerchantId == merchantId).Sum(c => c.TransactionAmount));
        }

        [Fact]
        public async Task FactTransactionsControllerController_TodaysFailedSales_SalesReturned()
        {

            //EstateManagementContext context = new EstateManagementContext(GetLocalConnectionString($"TransactionProcessorReadModel-{TestId.ToString()}"));
            var todaysTransactions = new List<Transaction>();
            var comparisonDateTransactions = new List<Transaction>();
            //DatabaseHelper helper1 = new DatabaseHelper(context);
            // TODO: make counts dynamic
            DateTime todaysDateTime = DateTime.Now;
            //todaysDateTime = todaysDateTime.AddHours(12).AddMinutes(30);

            Dictionary<string, int> transactionCounts = new() { { "Test Merchant 1", 3 }, { "Test Merchant 2", 6 }, { "Test Merchant 3", 2 }, { "Test Merchant 4", 0 } };

            DateTime comparisonDate = todaysDateTime.AddDays(-1);

            foreach (var merchant in merchantsList)
            {
                foreach (var contract in contractList)
                {
                    var productList = contractProducts.Single(cp => cp.Key == contract.contractId).Value;
                    foreach ((Guid productId, String productName, Decimal? productValue) product in productList)
                    {
                        var transactionCount = transactionCounts.Single(m => m.Key == merchant.Name).Value;
                        for (int i = 0; i < transactionCount; i++)
                        {
                            Transaction transaction = await helper.BuildTransactionX(todaysDateTime.AddHours(-1), merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "1009", product.productValue);
                            todaysTransactions.Add(transaction);
                        }
                    }
                }
            }

            await helper.AddTransactionsX(todaysTransactions);

            // Comparison Date sales
            foreach (var merchant in merchantsList)
            {
                foreach (var contract in contractList)
                {
                    var productList = contractProducts.Single(cp => cp.Key == contract.contractId).Value;
                    foreach ((Guid productId, String productName, Decimal? productValue) product in productList)
                    {
                        var transactionCount = transactionCounts.Single(m => m.Key == merchant.Name).Value;
                        for (int i = 0; i < transactionCount; i++)
                        {
                            Transaction transaction = await helper.BuildTransactionX(comparisonDate.AddHours(-1), merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "1009", product.productValue);
                            comparisonDateTransactions.Add(transaction);
                        }
                    }
                }
            }

            await helper.AddTransactionsX(comparisonDateTransactions);


            await helper.RunTodaysTransactionsSummaryProcessing(comparisonDate.Date);
            await helper.RunHistoricTransactionsSummaryProcessing(comparisonDate.Date);
            await helper.RunTodaysTransactionsSummaryProcessing(todaysDateTime.Date);

            Result<TodaysSales> result = await this.CreateAndSendHttpRequestMessage<TodaysSales>($"{this.BaseRoute}/todaysfailedsales?comparisonDate={comparisonDate:yyyy-MM-dd}&responseCode=1009", CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            DataTransferObjects.TodaysSales? todaysSales = result.Data;

            todaysSales.ShouldNotBeNull();
            todaysSales.ComparisonSalesCount.ShouldBe(comparisonDateTransactions.Count);
            todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Sum(c => c.TransactionAmount));

            todaysSales.TodaysSalesCount.ShouldBe(todaysTransactions.Count);
            todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Sum(c => c.TransactionAmount));
        }
    }
}

