using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace EstateReportingAPI.IntegrationTests;

public class SettlmentsEndpointTests : ControllerTestsBase {
    private String BaseRoute = "api/settlements";

    public SettlmentsEndpointTests(ITestOutputHelper testOutputHelper) {
        this.TestOutputHelper = testOutputHelper;
    }


    protected override async Task ClearStandingData() {

    }

    protected override async Task SetupStandingData() {
        Stopwatch sw = Stopwatch.StartNew();
        this.TestOutputHelper.WriteLine("Setting up standing data");

        // Estates
        await this.helper.AddEstate("Test Estate", "Ref1");
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
        await this.helper.AddMerchant("Test Estate", "Test Merchant 1",100, DateTime.MinValue, DateTime.MinValue, default, default);
        await this.helper.AddMerchant("Test Estate", "Test Merchant 2",100, DateTime.MinValue, DateTime.MinValue, default, default);
        await this.helper.AddMerchant("Test Estate", "Test Merchant 3",100, DateTime.MinValue, DateTime.MinValue, default, default);
        await this.helper.AddMerchant("Test Estate", "Test Merchant 4",100, DateTime.MinValue, DateTime.MinValue, default, default);
        sw.Stop();
        this.TestOutputHelper.WriteLine($"Setup Merchants {sw.ElapsedMilliseconds}ms");
        sw.Restart();

        // Contracts & Products
        List<(string productName, int productType, decimal? value)> safaricomProductList = new() { ("200 KES Topup", 0, 200.00m), ("100 KES Topup", 0, 100.00m), ("50 KES Topup", 0, 50.00m), ("Custom", 0, null) };
        await this.helper.AddContractWithProducts("Test Estate", "Safaricom Contract", "Safaricom", safaricomProductList);

        List<(string productName, int productType, decimal? value)> voucherProductList = new() { ("10 KES Voucher", 0, 10.00m), ("Custom", 0, null) };
        await this.helper.AddContractWithProducts("Test Estate", "Healthcare Centre 1 Contract", "Voucher", voucherProductList);

        List<(string productName, int productType, decimal? value)> postPayProductList = new() { ("Post Pay Bill Pay", 0, null) };
        await this.helper.AddContractWithProducts("Test Estate", "PataPawa PostPay Contract", "PataPawa PostPay", postPayProductList);

        List<(string productName, int productType, decimal? value)> prePayProductList = new() { ("Pre Pay Bill Pay", 0, null) };
        await this.helper.AddContractWithProducts("Test Estate", "PataPawa PrePay Contract", "PataPawa PrePay", prePayProductList);

        sw.Stop();
        this.TestOutputHelper.WriteLine($"Setup Contracts {sw.ElapsedMilliseconds}ms");
        sw.Restart();

        // Response Codes
        await this.helper.AddResponseCode(0, "Success");
        await this.helper.AddResponseCode(1000, "Unknown Device");
        await this.helper.AddResponseCode(1001, "Unknown Estate");
        await this.helper.AddResponseCode(1002, "Unknown Merchant");
        await this.helper.AddResponseCode(1003, "No Devices Configured");

        sw.Stop();
        this.TestOutputHelper.WriteLine($"Setup Response Codes {sw.ElapsedMilliseconds}ms");
        sw.Restart();

        this.merchantsList = this.context.Merchants.Select(m => m).ToList();

        this.contractList = this.context.Contracts.Join(this.context.Operators, c => c.OperatorId, o => o.OperatorId, (c,
                                                                                                                       o) => new { c.ContractId, c.Description, o.OperatorId, o.Name }).ToList().Select(x => (x.ContractId, x.Description, x.OperatorId, x.Name)).ToList();

        var query1 = this.context.Contracts.GroupJoin(this.context.ContractProducts, c => c.ContractId, cp => cp.ContractId, (c,
                                                                                                                              productGroup) => new { c.ContractId, Products = productGroup.Select(p => new { p.ContractProductReportingId, p.ContractProductId, p.ProductName, p.Value }).OrderBy(p => p.ContractProductId).Select(p => new { p.ContractProductId, p.ProductName, p.Value, p.ContractProductReportingId }).ToList() }).ToList();

        this.contractProducts = query1.ToDictionary(item => item.ContractId, item => item.Products.Select(i => (i.ContractProductId, i.ProductName, i.Value, i.ContractProductReportingId)).ToList());

        this.operatorsList = this.context.Operators.ToList();

        sw.Stop();
        this.TestOutputHelper.WriteLine($"Data Caching {sw.ElapsedMilliseconds}ms");
        sw.Restart();
    }

    [Fact]
    public async Task SettlementEndpoints_TodaysSettlement_SettlementReturned()
    {
        int overallTodaysSettlementTransactionCount = 0;
        int overallTodaysPendingSettlementTransactionCount = 0;

        int overallComparisonSettlementTransactionCount = 0;
        int overallComparisonPendingSettlementTransactionCount = 0;
        List<(decimal settledTransactions, decimal pendingSettlementTransactions, decimal settlementFees, decimal pendingSettlementFees)> todayOverallTotals = new();
        List<(decimal settledTransactions, decimal pendingSettlementTransactions, decimal settlementFees, decimal pendingSettlementFees)> comparisonOverallTotals = new();

        DateTime todaysDate = DateTime.Now;
        DateTime comparisonDate = DateTime.Now.AddDays(-1);
        foreach (var merchant in this.merchantsList)
        {
            int todaysSettlementTransactionCount = 5;
            int todaysPendingSettlementTransactionCount = 9;
            var contract = this.contractList.Single(c => c.operatorName == "Safaricom");
            (decimal settledTransactions, decimal pendingSettlementTransactions, decimal settlementFees, decimal pendingSettlementFees) todayTotals = await this.helper.AddSettlementRecord(merchant.EstateId, merchant.MerchantId, contract.operatorId, todaysDate, todaysSettlementTransactionCount, todaysPendingSettlementTransactionCount);
            todayOverallTotals.Add(todayTotals);

            overallTodaysSettlementTransactionCount += todaysSettlementTransactionCount;
            ;
            overallTodaysPendingSettlementTransactionCount += todaysPendingSettlementTransactionCount;

            int comparisonSettlementTransactionCount = 12;
            int comparisonPendingSettlementTransactionCount = 15;
            var comparisonTotals = await this.helper.AddSettlementRecord(merchant.EstateId, merchant.MerchantId, contract.operatorId, comparisonDate, comparisonSettlementTransactionCount, comparisonPendingSettlementTransactionCount);
            comparisonOverallTotals.Add(comparisonTotals);

            overallComparisonSettlementTransactionCount += comparisonSettlementTransactionCount;
            overallComparisonPendingSettlementTransactionCount += comparisonPendingSettlementTransactionCount;
        }

        await this.helper.RunTodaysTransactionsSummaryProcessing(comparisonDate.Date.AddDays(-1));
        await this.helper.RunHistoricTransactionsSummaryProcessing(comparisonDate.Date.AddDays(-1));
        await this.helper.RunTodaysTransactionsSummaryProcessing(todaysDate.Date.AddDays(-1));
        await this.helper.RunSettlementSummaryProcessing(comparisonDate.Date);


        var result = await this.CreateAndSendHttpRequestMessage<DataTransferObjects.TodaysSettlement>($"{this.BaseRoute}/todayssettlements?comparisondate={DateTime.Now.AddDays(-1):yyyy-MM-dd}", CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        var todaysSettlement = result.Data;
        todaysSettlement.ShouldNotBeNull();
        todaysSettlement.ComparisonSettlementCount.ShouldBe(overallComparisonSettlementTransactionCount);
        todaysSettlement.ComparisonSettlementValue.ShouldBe(comparisonOverallTotals.Sum(c => c.settlementFees));
        todaysSettlement.ComparisonPendingSettlementCount.ShouldBe(overallComparisonPendingSettlementTransactionCount);
        todaysSettlement.ComparisonPendingSettlementValue.ShouldBe(comparisonOverallTotals.Sum(c => c.pendingSettlementFees));

        todaysSettlement.TodaysSettlementCount.ShouldBe(overallTodaysSettlementTransactionCount);
        todaysSettlement.TodaysSettlementValue.ShouldBe(todayOverallTotals.Sum(c => c.settlementFees));
        todaysSettlement.TodaysPendingSettlementCount.ShouldBe(overallTodaysPendingSettlementTransactionCount);
        todaysSettlement.TodaysPendingSettlementValue.ShouldBe(todayOverallTotals.Sum(c => c.pendingSettlementFees));
    }
}