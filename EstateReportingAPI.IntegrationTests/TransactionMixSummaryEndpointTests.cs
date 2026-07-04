using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EstateReportingAPI.DataTransferObjects;
using Shared.Serialisation;
using Shouldly;
using TransactionProcessor.Database.Entities;
using Xunit;

namespace EstateReportingAPI.IntegrationTests;

public class TransactionMixSummaryEndpointTests : ControllerTestsBase
{
    private const string BaseRoute = "api/transactions";

    public TransactionMixSummaryEndpointTests(ITestOutputHelper testOutputHelper)
    {
        this.TestOutputHelper = testOutputHelper;
    }

    protected override async Task ClearStandingData()
    {
    }

    protected override async Task SetupStandingData()
    {
        Stopwatch sw = Stopwatch.StartNew();
        this.TestOutputHelper.WriteLine("Setting up standing data");

        await this.helper.AddEstate("Test Estate", "Ref1");
        await this.helper.AddOperator("Test Estate", "Safaricom");
        await this.helper.AddOperator("Test Estate", "Voucher");
        await this.helper.AddMerchant("Test Estate", "Test Merchant 1", 100, DateTime.MinValue, DateTime.MinValue, default, default);

        List<(string productName, int productType, decimal? value)> productList = new()
        {
            ("200 KES Topup", 0, 200.00m),
            ("100 KES Topup", 0, 100.00m)
        };

        await this.helper.AddContractWithProducts("Test Estate", "Safaricom Contract", "Safaricom", productList);
        await this.helper.AddResponseCode(0, "Success");
        await this.helper.AddResponseCode(1000, "Unknown Device");

        this.merchantsList = this.context.Merchants.Select(m => m).ToList();
        this.contractList = this.context.Contracts.Join(this.context.Operators, c => c.OperatorId, o => o.OperatorId, (c, o) => new { c.ContractId, c.Description, o.OperatorId, o.Name }).ToList().Select(x => (x.ContractId, x.Description, x.OperatorId, x.Name)).ToList();

        var query1 = this.context.Contracts.GroupJoin(this.context.ContractProducts, c => c.ContractId, cp => cp.ContractId, (c, productGroup) => new { c.ContractId, Products = productGroup.Select(p => new { p.ContractProductReportingId, p.ContractProductId, p.ProductName, p.Value }).OrderBy(p => p.ContractProductId).Select(p => new { p.ContractProductId, p.ProductName, p.Value, p.ContractProductReportingId }).ToList() }).ToList();
        this.contractProducts = query1.ToDictionary(item => item.ContractId, item => item.Products.Select(i => (i.ContractProductId, i.ProductName, i.Value, i.ContractProductReportingId)).ToList());
        this.operatorsList = this.context.Operators.ToList();
    }

    [Fact]
    public async Task TransactionMixSummary_ProductBreakdown_ReturnsGroupedResults()
    {
        var merchant = this.merchantsList.Single();
        var contract = this.contractList.Single();
        var product = this.contractProducts.Single(x => x.Key == contract.contractId).Value.First();
        DateTime startDate = DateTime.Now.Date.AddDays(-2);
        DateTime endDate = DateTime.Now.Date;

        var transactions = new List<Transaction>();
        for (int i = 0; i < 3; i++)
        {
            transactions.Add(await this.helper.BuildTransactionX(DateTime.Now.Date, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, i == 2 ? "1000" : "0000", product.productValue));
        }

        await this.helper.AddTransactionsX(transactions);

        var request = new TransactionMixSummaryRequest
        {
            MerchantReportingId = 1,
            StartDate = startDate,
            EndDate = endDate,
            Breakdown = TransactionMixBreakdown.Product,
            Measure = TransactionMixMeasure.Count,
            TopN = 5
        };

        String payload = StringSerialiser.Serialise(request);
        var result = await this.CreateAndSendHttpRequestMessage<TransactionMixSummaryResponse>($"{BaseRoute}/transactionmixsummary", payload, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data.TotalCount.ShouldBe(3);
        result.Data.Groups.Count.ShouldBe(1);
        result.Data.Groups[0].TransactionCount.ShouldBe(3);
    }

    [Fact]
    public async Task TransactionMixSummary_InvalidDateRange_ReturnsBadRequest()
    {
        var request = new TransactionMixSummaryRequest
        {
            MerchantReportingId = 1,
            StartDate = DateTime.Now.Date,
            EndDate = DateTime.Now.Date.AddDays(-1),
            Breakdown = TransactionMixBreakdown.Operator,
            Measure = TransactionMixMeasure.Value,
            TopN = 5
        };

        String payload = StringSerialiser.Serialise(request);
        HttpRequestMessage requestMessage = new(HttpMethod.Post, $"{BaseRoute}/transactionmixsummary");
        requestMessage.Headers.Add("estateId", this.TestId.ToString());
        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Test");
        requestMessage.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await this.Client.SendAsync(requestMessage, CancellationToken.None);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
