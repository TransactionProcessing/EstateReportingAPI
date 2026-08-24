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

    private void AssertContractMatchesDatabase(Contract contract, Guid contractId, bool includeProducts) {
        var sourceContract = this.context.Contracts.Single(c => c.ContractId == contractId);
        var sourceOperator = this.context.Operators.Single(o => o.OperatorId == sourceContract.OperatorId);
        var sourceEstate = this.context.Estates.Single(e => e.EstateId == sourceContract.EstateId);

        contract.EstateId.ShouldBe(sourceContract.EstateId);
        contract.EstateReportingId.ShouldBe(sourceEstate.EstateReportingId);
        contract.ContractId.ShouldBe(sourceContract.ContractId);
        contract.ContractReportingId.ShouldBe(sourceContract.ContractReportingId);
        contract.Description.ShouldBe(sourceContract.Description);
        contract.OperatorName.ShouldBe(sourceOperator.Name);
        contract.OperatorId.ShouldBe(sourceOperator.OperatorId);
        contract.OperatorReportingId.ShouldBe(sourceOperator.OperatorReportingId);

        if (includeProducts) {
            var sourceProducts = this.context.ContractProducts
                .Where(p => p.ContractId == contractId)
                .OrderBy(p => p.ProductName)
                .ToList();

            contract.Products.ShouldNotBeNull();
            contract.Products.Count.ShouldBe(sourceProducts.Count);

            foreach (var sourceProduct in sourceProducts) {
                var actualProduct = contract.Products.Single(p => p.ProductId == sourceProduct.ContractProductId);
                actualProduct.ContractId.ShouldBe(sourceProduct.ContractId);
                actualProduct.ContractProductReportingId.ShouldBe(sourceProduct.ContractProductReportingId);
                actualProduct.ProductName.ShouldBe(sourceProduct.ProductName);
                actualProduct.DisplayText.ShouldBe(sourceProduct.DisplayText);
                actualProduct.ProductType.ShouldBe(sourceProduct.ProductType);
                actualProduct.Value.ShouldBe(sourceProduct.Value);

                var sourceFees = this.context.ContractProductTransactionFees
                    .Where(f => f.ContractProductId == sourceProduct.ContractProductId)
                    .OrderBy(f => f.ContractProductTransactionFeeReportingId)
                    .ToList();

                actualProduct.TransactionFees.Count.ShouldBe(sourceFees.Count);
                foreach (var sourceFee in sourceFees) {
                    var actualFee = actualProduct.TransactionFees.Single(f => f.TransactionFeeId == sourceFee.ContractProductTransactionFeeId);
                    actualFee.ContractProductTransactionFeeReportingId.ShouldBe(sourceFee.ContractProductTransactionFeeReportingId);
                    actualFee.Description.ShouldBe(sourceFee.Description);
                    actualFee.CalculationType.ShouldBe(sourceFee.CalculationType);
                    actualFee.FeeType.ShouldBe(sourceFee.FeeType);
                    actualFee.Value.ShouldBe(sourceFee.Value);
                }
            }
        }
        else {
            contract.Products.ShouldBeNull();
        }
    }

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
        AssertContractMatchesDatabase(contracts.Single(c => c.Description == "Healthcare Centre 1 Contract"), contracts.Single(c => c.Description == "Healthcare Centre 1 Contract").ContractId, false);
        AssertContractMatchesDatabase(contracts.Single(c => c.Description == "PataPawa PostPay Contract"), contracts.Single(c => c.Description == "PataPawa PostPay Contract").ContractId, false);
        AssertContractMatchesDatabase(contracts.Single(c => c.Description == "PataPawa PrePay Contract"), contracts.Single(c => c.Description == "PataPawa PrePay Contract").ContractId, false);

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
        AssertContractMatchesDatabase(contracts.Single(c => c.Description == "Safaricom Contract"), contracts.Single(c => c.Description == "Safaricom Contract").ContractId, true);
        AssertContractMatchesDatabase(contracts.Single(c => c.Description == "Healthcare Centre 1 Contract"), contracts.Single(c => c.Description == "Healthcare Centre 1 Contract").ContractId, true);
        AssertContractMatchesDatabase(contracts.Single(c => c.Description == "PataPawa PostPay Contract"), contracts.Single(c => c.Description == "PataPawa PostPay Contract").ContractId, true);
        AssertContractMatchesDatabase(contracts.Single(c => c.Description == "PataPawa PrePay Contract"), contracts.Single(c => c.Description == "PataPawa PrePay Contract").ContractId, true);
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
        AssertContractMatchesDatabase(contract, ppprepayContractId, true);
    }

    protected override async Task ClearStandingData() {

    }

    protected override async Task SetupStandingData() {

    }
}
