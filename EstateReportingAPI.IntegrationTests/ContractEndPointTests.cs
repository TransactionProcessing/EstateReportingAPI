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

public class ContractEndPointTests : ControllerTestsBase {
    private String BaseRoute = "api/contracts";

    [Fact]
    public async Task ContractEndpoint_GetRecentContracts_ContractsReturned() {
        await this.helper.AddEstate("Test Estate", "Ref1");

        await this.helper.AddOperator("Test Estate", "Safaricom");
        await this.helper.AddOperator("Test Estate", "Voucher");
        await this.helper.AddOperator("Test Estate", "PataPawa PostPay");
        await this.helper.AddOperator("Test Estate", "PataPawa PrePay");

        // Contracts & Products
        List<(string productName, int productType, decimal? value)> safaricomProductList = new() { ("200 KES Topup", 0, 200.00m), ("100 KES Topup", 0, 100.00m), ("50 KES Topup", 0, 50.00m), ("Custom", 0, null) };
        await this.helper.AddContractWithProducts("Test Estate", "Safaricom Contract", "Safaricom", safaricomProductList);

        List<(string productName, int productType, decimal? value)> voucherProductList = new() { ("10 KES Voucher", 0, 10.00m), ("Custom", 0, null) };
        await this.helper.AddContractWithProducts("Test Estate", "Healthcare Centre 1 Contract", "Voucher", voucherProductList);

        List<(string productName, int productType, decimal? value)> postPayProductList = new() { ("Post Pay Bill Pay", 0, null) };
        await this.helper.AddContractWithProducts("Test Estate", "PataPawa PostPay Contract", "PataPawa PostPay", postPayProductList);

        List<(string productName, int productType, decimal? value)> prePayProductList = new() { ("Pre Pay Bill Pay", 0, null) };
        await this.helper.AddContractWithProducts("Test Estate", "PataPawa PrePay Contract", "PataPawa PrePay", prePayProductList);

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
        await this.helper.AddEstate("Test Estate", "Ref1");

        await this.helper.AddOperator("Test Estate", "Safaricom");
        await this.helper.AddOperator("Test Estate", "Voucher");
        await this.helper.AddOperator("Test Estate", "PataPawa PostPay");
        await this.helper.AddOperator("Test Estate", "PataPawa PrePay");

        // Contracts & Products
        List<(string productName, int productType, decimal? value)> safaricomProductList = new() { ("200 KES Topup", 0, 200.00m), ("100 KES Topup", 0, 100.00m), ("50 KES Topup", 0, 50.00m), ("Custom", 0, null) };
        await this.helper.AddContractWithProducts("Test Estate", "Safaricom Contract", "Safaricom", safaricomProductList);

        List<(string productName, int productType, decimal? value)> voucherProductList = new() { ("10 KES Voucher", 0, 10.00m), ("Custom", 0, null) };
        await this.helper.AddContractWithProducts("Test Estate", "Healthcare Centre 1 Contract", "Voucher", voucherProductList);

        List<(string productName, int productType, decimal? value)> postPayProductList = new() { ("Post Pay Bill Pay", 0, null) };
        await this.helper.AddContractWithProducts("Test Estate", "PataPawa PostPay Contract", "PataPawa PostPay", postPayProductList);

        List<(string productName, int productType, decimal? value)> prePayProductList = new() { ("Pre Pay Bill Pay", 0, null) };
        await this.helper.AddContractWithProducts("Test Estate", "PataPawa PrePay Contract", "PataPawa PrePay", prePayProductList);

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
        await this.helper.AddEstate("Test Estate", "Ref1");

        await this.helper.AddOperator("Test Estate", "Safaricom");
        await this.helper.AddOperator("Test Estate", "Voucher");
        await this.helper.AddOperator("Test Estate", "PataPawa PostPay");
        await this.helper.AddOperator("Test Estate", "PataPawa PrePay");

        // Contracts & Products
        List<(string productName, int productType, decimal? value)> safaricomProductList = new() { ("200 KES Topup", 0, 200.00m), ("100 KES Topup", 0, 100.00m), ("50 KES Topup", 0, 50.00m), ("Custom", 0, null) };
        await this.helper.AddContractWithProducts("Test Estate", "Safaricom Contract", "Safaricom", safaricomProductList);

        List<(string productName, int productType, decimal? value)> voucherProductList = new() { ("10 KES Voucher", 0, 10.00m), ("Custom", 0, null) };
        await this.helper.AddContractWithProducts("Test Estate", "Healthcare Centre 1 Contract", "Voucher", voucherProductList);

        List<(string productName, int productType, decimal? value)> postPayProductList = new() { ("Post Pay Bill Pay", 0, null) };
        await this.helper.AddContractWithProducts("Test Estate", "PataPawa PostPay Contract", "PataPawa PostPay", postPayProductList);

        List<(string productName, int productType, decimal? value)> prePayProductList = new() { ("Pre Pay Bill Pay", 0, null) };
        var ppprepayContractId = await this.helper.AddContractWithProducts("Test Estate", "PataPawa PrePay Contract", "PataPawa PrePay", prePayProductList);

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