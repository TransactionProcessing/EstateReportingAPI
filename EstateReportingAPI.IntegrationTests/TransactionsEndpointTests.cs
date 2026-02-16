using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EstateReportingAPI.DataTransferObjects;
using Newtonsoft.Json;
using Shouldly;
using SimpleResults;
using TransactionProcessor.Database.Entities;
using Xunit;
using Xunit.Abstractions;

namespace EstateReportingAPI.IntegrationTests;

public class TransactionsEndpointTests : ControllerTestsBase {
    private String BaseRoute = "api/transactions";

    public TransactionsEndpointTests(ITestOutputHelper testOutputHelper) {
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
        await this.helper.AddMerchant("Test Estate", "Test Merchant 1", DateTime.MinValue, DateTime.MinValue, default, default);
        await this.helper.AddMerchant("Test Estate", "Test Merchant 2", DateTime.MinValue, DateTime.MinValue, default, default);
        await this.helper.AddMerchant("Test Estate", "Test Merchant 3", DateTime.MinValue, DateTime.MinValue, default, default);
        await this.helper.AddMerchant("Test Estate", "Test Merchant 4", DateTime.MinValue, DateTime.MinValue, default, default);
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
    public async Task TransactionsEndpoint_TodaysSales_SalesReturned() {
        List<Transaction>? todaysTransactions = new List<Transaction>();
        List<Transaction> comparisonDateTransactions = new List<Transaction>();

        DateTime todaysDateTime = DateTime.Now;
        DateTime comparisonDate = DateTime.Now.AddDays(-1).AddHours(-1);

        Dictionary<string, int> transactionCounts = new() { { "Test Merchant 1", 15 }, { "Test Merchant 2", 18 }, { "Test Merchant 3", 9 }, { "Test Merchant 4", 0 } };

        foreach (var merchant in this.merchantsList) {
            foreach (var contract in this.contractList) {
                var productList = this.contractProducts.Single(cp => cp.Key == contract.contractId).Value;
                foreach ((Guid productId, String productName, Decimal? productValue, Int32 contractProductReportingId) product in productList) {
                    var transactionCount = transactionCounts.Single(m => m.Key == merchant.Name).Value;
                    for (int i = 0; i < transactionCount; i++) {
                        Transaction transaction = await this.helper.BuildTransactionX(todaysDateTime.AddHours(-1), merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", product.productValue);
                        todaysTransactions.Add(transaction);
                    }
                }
            }
        }

        await this.helper.AddTransactionsX(todaysTransactions);

        // Comparison Date sales
        foreach (var merchant in this.merchantsList) {
            foreach (var contract in this.contractList) {
                var productList = this.contractProducts.Single(cp => cp.Key == contract.contractId).Value;
                foreach ((Guid productId, String productName, Decimal? productValue, Int32 contractProductReportingId) product in productList) {
                    var transactionCount = transactionCounts.Single(m => m.Key == merchant.Name).Value;
                    for (int i = 0; i < transactionCount; i++) {
                        Transaction transaction = await this.helper.BuildTransactionX(comparisonDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", product.productValue);
                        comparisonDateTransactions.Add(transaction);
                    }
                }
            }
        }

        await this.helper.AddTransactionsX(comparisonDateTransactions);

        await this.helper.RunTodaysTransactionsSummaryProcessing(comparisonDate.Date);
        await this.helper.RunHistoricTransactionsSummaryProcessing(comparisonDate.Date);
        await this.helper.RunTodaysTransactionsSummaryProcessing(todaysDateTime.Date);

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
    public async Task TransactionsEndpoint_TodaysSales_OperatorFilter_SalesReturned() {
        List<Transaction>? todaysTransactions = new List<Transaction>();
        List<Transaction> comparisonDateTransactions = new List<Transaction>();

        DateTime todaysDateTime = DateTime.Now;
        DateTime comparisonDate = DateTime.Now.AddDays(-1).AddHours(-1);

        Dictionary<string, int> transactionCounts = new() { { "Test Merchant 1", 15 }, { "Test Merchant 2", 18 }, { "Test Merchant 3", 9 }, { "Test Merchant 4", 0 } };

        foreach (var merchant in this.merchantsList) {
            foreach (var contract in this.contractList) {
                var productList = this.contractProducts.Single(cp => cp.Key == contract.contractId).Value;
                foreach ((Guid productId, String productName, Decimal? productValue, Int32 contractProductReportingId) product in productList) {
                    var transactionCount = transactionCounts.Single(m => m.Key == merchant.Name).Value;
                    for (int i = 0; i < transactionCount; i++) {
                        Transaction transaction = await this.helper.BuildTransactionX(todaysDateTime.AddHours(-1), merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", product.productValue);
                        todaysTransactions.Add(transaction);
                    }
                }
            }
        }

        await this.helper.AddTransactionsX(todaysTransactions);

        // Comparison Date sales
        foreach (var merchant in this.merchantsList) {
            foreach (var contract in this.contractList) {
                var productList = this.contractProducts.Single(cp => cp.Key == contract.contractId).Value;
                foreach ((Guid productId, String productName, Decimal? productValue, Int32 contractProductReportingId) product in productList) {
                    var transactionCount = transactionCounts.Single(m => m.Key == merchant.Name).Value;
                    for (int i = 0; i < transactionCount; i++) {
                        Transaction transaction = await this.helper.BuildTransactionX(comparisonDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", product.productValue);
                        comparisonDateTransactions.Add(transaction);
                    }
                }
            }
        }

        await this.helper.AddTransactionsX(comparisonDateTransactions);

        await this.helper.RunTodaysTransactionsSummaryProcessing(comparisonDate.Date);
        await this.helper.RunHistoricTransactionsSummaryProcessing(comparisonDate.Date);
        await this.helper.RunTodaysTransactionsSummaryProcessing(todaysDateTime.Date);

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
    public async Task TransactionsEndpoint_TodaysSales_MerchantFilter_SalesReturned() {
        List<Transaction>? todaysTransactions = new List<Transaction>();
        List<Transaction> comparisonDateTransactions = new List<Transaction>();

        DateTime todaysDateTime = DateTime.Now;
        DateTime comparisonDate = DateTime.Now.AddDays(-1).AddHours(-1);

        Dictionary<string, int> transactionCounts = new() { { "Test Merchant 1", 15 }, { "Test Merchant 2", 18 }, { "Test Merchant 3", 9 }, { "Test Merchant 4", 0 } };

        foreach (var merchant in this.merchantsList) {
            foreach (var contract in this.contractList) {
                var productList = this.contractProducts.Single(cp => cp.Key == contract.contractId).Value;
                foreach ((Guid productId, String productName, Decimal? productValue, Int32 contractProductReportingId) product in productList) {
                    var transactionCount = transactionCounts.Single(m => m.Key == merchant.Name).Value;
                    for (int i = 0; i < transactionCount; i++) {
                        Transaction transaction = await this.helper.BuildTransactionX(todaysDateTime.AddHours(-1), merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", product.productValue);
                        todaysTransactions.Add(transaction);
                    }
                }
            }
        }

        await this.helper.AddTransactionsX(todaysTransactions);

        // Comparison Date sales
        foreach (var merchant in this.merchantsList) {
            foreach (var contract in this.contractList) {
                var productList = this.contractProducts.Single(cp => cp.Key == contract.contractId).Value;
                foreach ((Guid productId, String productName, Decimal? productValue, Int32 contractProductReportingId) product in productList) {
                    var transactionCount = transactionCounts.Single(m => m.Key == merchant.Name).Value;
                    for (int i = 0; i < transactionCount; i++) {
                        Transaction transaction = await this.helper.BuildTransactionX(comparisonDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", product.productValue);
                        comparisonDateTransactions.Add(transaction);
                    }
                }
            }
        }

        await this.helper.AddTransactionsX(comparisonDateTransactions);

        await this.helper.RunTodaysTransactionsSummaryProcessing(comparisonDate.Date);
        await this.helper.RunHistoricTransactionsSummaryProcessing(comparisonDate.Date);
        await this.helper.RunTodaysTransactionsSummaryProcessing(todaysDateTime.Date);

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
    public async Task TransactionsEndpoint_TodaysFailedSales_SalesReturned() {

        //EstateManagementContext context = new EstateManagementContext(GetLocalConnectionString($"TransactionProcessorReadModel-{TestId.ToString()}"));
        var todaysTransactions = new List<Transaction>();
        var comparisonDateTransactions = new List<Transaction>();
        //DatabaseHelper helper1 = new DatabaseHelper(context);
        // TODO: make counts dynamic
        DateTime todaysDateTime = DateTime.Now;
        //todaysDateTime = todaysDateTime.AddHours(12).AddMinutes(30);

        Dictionary<string, int> transactionCounts = new() { { "Test Merchant 1", 3 }, { "Test Merchant 2", 6 }, { "Test Merchant 3", 2 }, { "Test Merchant 4", 0 } };

        DateTime comparisonDate = todaysDateTime.AddDays(-1);

        foreach (var merchant in this.merchantsList) {
            foreach (var contract in this.contractList) {
                var productList = this.contractProducts.Single(cp => cp.Key == contract.contractId).Value;
                foreach ((Guid productId, String productName, Decimal? productValue, Int32 contractProductReportingId) product in productList) {
                    var transactionCount = transactionCounts.Single(m => m.Key == merchant.Name).Value;
                    for (int i = 0; i < transactionCount; i++) {
                        Transaction transaction = await this.helper.BuildTransactionX(todaysDateTime.AddHours(-1), merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "1009", product.productValue);
                        todaysTransactions.Add(transaction);
                    }
                }
            }
        }

        await this.helper.AddTransactionsX(todaysTransactions);

        // Comparison Date sales
        foreach (var merchant in this.merchantsList) {
            foreach (var contract in this.contractList) {
                var productList = this.contractProducts.Single(cp => cp.Key == contract.contractId).Value;
                foreach ((Guid productId, String productName, Decimal? productValue, Int32 contractProductReportingId) product in productList) {
                    var transactionCount = transactionCounts.Single(m => m.Key == merchant.Name).Value;
                    for (int i = 0; i < transactionCount; i++) {
                        Transaction transaction = await this.helper.BuildTransactionX(comparisonDate.AddHours(-1), merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "1009", product.productValue);
                        comparisonDateTransactions.Add(transaction);
                    }
                }
            }
        }

        await this.helper.AddTransactionsX(comparisonDateTransactions);


        await this.helper.RunTodaysTransactionsSummaryProcessing(comparisonDate.Date);
        await this.helper.RunHistoricTransactionsSummaryProcessing(comparisonDate.Date);
        await this.helper.RunTodaysTransactionsSummaryProcessing(todaysDateTime.Date);

        Result<TodaysSales> result = await this.CreateAndSendHttpRequestMessage<TodaysSales>($"{this.BaseRoute}/todaysfailedsales?comparisonDate={comparisonDate:yyyy-MM-dd}&responseCode=1009", CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        DataTransferObjects.TodaysSales? todaysSales = result.Data;

        todaysSales.ShouldNotBeNull();
        todaysSales.ComparisonSalesCount.ShouldBe(comparisonDateTransactions.Count);
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Sum(c => c.TransactionAmount));

        todaysSales.TodaysSalesCount.ShouldBe(todaysTransactions.Count);
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Sum(c => c.TransactionAmount));
    }

    [Fact]
    public async Task TransactionsEndpoint_TransactionDetailReport_NoFilters_TransactionsReturned() {

        var transactions = new List<Transaction>();
            
        Dictionary<string, int> transactionCounts = new() { { "Test Merchant 1", 3 }, { "Test Merchant 2", 6 }, { "Test Merchant 3", 2 }, { "Test Merchant 4", 0 } };

        // Get a set of dates for the transactions
        List<DateTime> transactionDates = new() {
            DateTime.Now.Date,
            DateTime.Now.Date.AddDays(-1),
            DateTime.Now.Date.AddDays(-2),
            DateTime.Now.Date.AddDays(-3),
            DateTime.Now.Date.AddDays(-4),
            DateTime.Now.Date.AddDays(-5),
        };

        foreach (DateTime transactionDate in transactionDates) {
            foreach (var merchant in this.merchantsList) {
                foreach (var contract in this.contractList) {
                    var productList = this.contractProducts.Single(cp => cp.Key == contract.contractId).Value;
                    foreach ((Guid productId, String productName, Decimal? productValue, Int32 contractProductReportingId) product in productList) {
                        var transactionCount = transactionCounts.Single(m => m.Key == merchant.Name).Value;
                        for (int i = 0; i < transactionCount; i++) {
                            Transaction transaction = await this.helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", product.productValue);
                            transactions.Add(transaction);
                        }
                    }
                }
            }
        }
            
        await this.helper.AddTransactionsX(transactions);
            
        List<DateTime> orderedDates = transactionDates.OrderBy(x => x).ToList();
        TransactionDetailReportRequest request = new TransactionDetailReportRequest {
            StartDate = orderedDates.First(),
            EndDate = orderedDates.Last(),
            Merchants = [],
            Operators = [],
            Products = []
        };

        String payload = JsonConvert.SerializeObject(request);


        Result<TransactionDetailReportResponse> result = await this.CreateAndSendHttpRequestMessage<TransactionDetailReportResponse>($"{this.BaseRoute}/transactionDetailReport", payload, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        var transactionDetailReportResponse = result.Data;

        transactionDetailReportResponse.ShouldNotBeNull();
        transactionDetailReportResponse.Summary.ShouldNotBeNull();
        transactionDetailReportResponse.Summary.TransactionCount.ShouldBe(transactions.Count);
        transactionDetailReportResponse.Summary.TotalValue.ShouldBe(transactions.Sum(t=> t.TransactionAmount));

        foreach (Transaction transaction in transactions) {
            var foundTxn = transactionDetailReportResponse.Transactions.SingleOrDefault(t => t.Id == transaction.TransactionId);
            foundTxn.ShouldNotBeNull(transaction.TransactionId.ToString());
        }
    }

    [Fact]
    public async Task TransactionsEndpoint_TransactionDetailReport_MerchantFilter_TransactionsReturned()
    {

        var transactions = new List<Transaction>();

        Dictionary<string, int> transactionCounts = new() { { "Test Merchant 1", 3 }, { "Test Merchant 2", 6 }, { "Test Merchant 3", 2 }, { "Test Merchant 4", 0 } };

        // Get a set of dates for the transactions
        List<DateTime> transactionDates = new() {
            DateTime.Now.Date,
            DateTime.Now.Date.AddDays(-1),
            DateTime.Now.Date.AddDays(-2),
            DateTime.Now.Date.AddDays(-3),
            DateTime.Now.Date.AddDays(-4),
            DateTime.Now.Date.AddDays(-5),
        };

        foreach (DateTime transactionDate in transactionDates)
        {
            foreach (var merchant in this.merchantsList)
            {
                foreach (var contract in this.contractList)
                {
                    var productList = this.contractProducts.Single(cp => cp.Key == contract.contractId).Value;
                    foreach ((Guid productId, String productName, Decimal? productValue, Int32 contractProductReportingId) product in productList)
                    {
                        var transactionCount = transactionCounts.Single(m => m.Key == merchant.Name).Value;
                        for (int i = 0; i < transactionCount; i++)
                        {
                            Transaction transaction = await this.helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", product.productValue);
                            transactions.Add(transaction);
                        }
                    }
                }
            }
        }

        await this.helper.AddTransactionsX(transactions);

        var merchantsForFilter = this.merchantsList.Where(m => m.Name == "Test Merchant 1" || m.Name == "Test Merchant 2");

        List<DateTime> orderedDates = transactionDates.OrderBy(x => x).ToList();
        TransactionDetailReportRequest request = new TransactionDetailReportRequest
        {
            StartDate = orderedDates.First(),
            EndDate = orderedDates.Last(),
            Merchants = merchantsForFilter.Select(m=> m.MerchantReportingId).ToList(),
            Operators = [],
            Products = []
        };

        String payload = JsonConvert.SerializeObject(request);
            
        Result<TransactionDetailReportResponse> result = await this.CreateAndSendHttpRequestMessage<TransactionDetailReportResponse>($"{this.BaseRoute}/transactionDetailReport", payload, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        var transactionDetailReportResponse = result.Data;

        // filter transactions for verification
        var filteredTransactions = transactions.Where(t => merchantsForFilter.Select(m => m.MerchantId).Contains(t.MerchantId));

        transactionDetailReportResponse.ShouldNotBeNull();
        transactionDetailReportResponse.Summary.ShouldNotBeNull();
        transactionDetailReportResponse.Summary.TransactionCount.ShouldBe(filteredTransactions.Count());
        transactionDetailReportResponse.Summary.TotalValue.ShouldBe(filteredTransactions.Sum(t => t.TransactionAmount));

        foreach (Transaction transaction in filteredTransactions)
        {
            var foundTxn = transactionDetailReportResponse.Transactions.SingleOrDefault(t => t.Id == transaction.TransactionId);
            foundTxn.ShouldNotBeNull(transaction.TransactionId.ToString());
        }
    }

    [Fact]
    public async Task TransactionsEndpoint_TransactionDetailReport_OperatorFilter_TransactionsReturned()
    {

        var transactions = new List<Transaction>();

        Dictionary<string, int> transactionCounts = new() { { "Test Merchant 1", 3 }, { "Test Merchant 2", 6 }, { "Test Merchant 3", 2 }, { "Test Merchant 4", 0 } };

        // Get a set of dates for the transactions
        List<DateTime> transactionDates = new() {
            DateTime.Now.Date,
            DateTime.Now.Date.AddDays(-1),
            DateTime.Now.Date.AddDays(-2),
            DateTime.Now.Date.AddDays(-3),
            DateTime.Now.Date.AddDays(-4),
            DateTime.Now.Date.AddDays(-5),
        };

        foreach (DateTime transactionDate in transactionDates)
        {
            foreach (var merchant in this.merchantsList)
            {
                foreach (var contract in this.contractList)
                {
                    var productList = this.contractProducts.Single(cp => cp.Key == contract.contractId).Value;
                    foreach ((Guid productId, String productName, Decimal? productValue, Int32 contractProductReportingId) product in productList)
                    {
                        var transactionCount = transactionCounts.Single(m => m.Key == merchant.Name).Value;
                        for (int i = 0; i < transactionCount; i++)
                        {
                            Transaction transaction = await this.helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", product.productValue);
                            transactions.Add(transaction);
                        }
                    }
                }
            }
        }

        await this.helper.AddTransactionsX(transactions);

        var operatorsForFilter = this.operatorsList.Where(m => m.Name == "Safaricom");

        List<DateTime> orderedDates = transactionDates.OrderBy(x => x).ToList();
        TransactionDetailReportRequest request = new TransactionDetailReportRequest
        {
            StartDate = orderedDates.First(),
            EndDate = orderedDates.Last(),
            Merchants = [],
            Operators = operatorsForFilter.Select(o=> o.OperatorReportingId).ToList(),
            Products = []
        };

        String payload = JsonConvert.SerializeObject(request);

        Result<TransactionDetailReportResponse> result = await this.CreateAndSendHttpRequestMessage<TransactionDetailReportResponse>($"{this.BaseRoute}/transactionDetailReport", payload, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        var transactionDetailReportResponse = result.Data;

        // filter transactions for verification
        var filteredTransactions = transactions.Where(t => operatorsForFilter.Select(m => m.OperatorId).Contains(t.OperatorId));

        transactionDetailReportResponse.ShouldNotBeNull();
        transactionDetailReportResponse.Summary.ShouldNotBeNull();
        transactionDetailReportResponse.Summary.TransactionCount.ShouldBe(filteredTransactions.Count());
        transactionDetailReportResponse.Summary.TotalValue.ShouldBe(filteredTransactions.Sum(t => t.TransactionAmount));

        foreach (Transaction transaction in filteredTransactions)
        {
            var foundTxn = transactionDetailReportResponse.Transactions.SingleOrDefault(t => t.Id == transaction.TransactionId);
            foundTxn.ShouldNotBeNull(transaction.TransactionId.ToString());
        }
    }

    [Fact]
    public async Task TransactionsEndpoint_TransactionDetailReport_ProductFilter_TransactionsReturned()
    {
        var transactions = new List<Transaction>();

        Dictionary<string, int> transactionCounts = new() { { "Test Merchant 1", 3 }, { "Test Merchant 2", 6 }, { "Test Merchant 3", 2 }, { "Test Merchant 4", 0 } };

        // Get a set of dates for the transactions
        List<DateTime> transactionDates = new() {
            DateTime.Now.Date,
            DateTime.Now.Date.AddDays(-1),
            DateTime.Now.Date.AddDays(-2),
            DateTime.Now.Date.AddDays(-3),
            DateTime.Now.Date.AddDays(-4),
            DateTime.Now.Date.AddDays(-5),
        };

        foreach (DateTime transactionDate in transactionDates)
        {
            foreach (var merchant in this.merchantsList)
            {
                foreach (var contract in this.contractList)
                {
                    var productList = this.contractProducts.Single(cp => cp.Key == contract.contractId).Value;
                    foreach ((Guid productId, String productName, Decimal? productValue, Int32 contractProductReportingId) product in productList)
                    {
                        var transactionCount = transactionCounts.Single(m => m.Key == merchant.Name).Value;
                        for (int i = 0; i < transactionCount; i++)
                        {
                            Transaction transaction = await this.helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", product.productValue);
                            transactions.Add(transaction);
                        }
                    }
                }
            }
        }

        await this.helper.AddTransactionsX(transactions);

        IEnumerable<(Guid productId, String productName, Decimal? productValue, Int32 contractProductReportingId)> productsForFilter = this.contractProducts.SelectMany(cp => cp.Value).Where(p => p.productName == "100 KES Topup");

        List<DateTime> orderedDates = transactionDates.OrderBy(x => x).ToList();
        TransactionDetailReportRequest request = new TransactionDetailReportRequest
        {
            StartDate = orderedDates.First(),
            EndDate = orderedDates.Last(),
            Merchants = [],
            Operators = [],
            Products = productsForFilter.Select(c => c.contractProductReportingId).ToList(),
        };

        String payload = JsonConvert.SerializeObject(request);

        Result<TransactionDetailReportResponse> result = await this.CreateAndSendHttpRequestMessage<TransactionDetailReportResponse>($"{this.BaseRoute}/transactionDetailReport", payload, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        var transactionDetailReportResponse = result.Data;

        // filter transactions for verification
        var filteredTransactions = transactions.Where(t => productsForFilter.Select(m => m.productId).Contains(t.ContractProductId));

        transactionDetailReportResponse.ShouldNotBeNull();
        transactionDetailReportResponse.Summary.ShouldNotBeNull();
        transactionDetailReportResponse.Summary.TransactionCount.ShouldBe(filteredTransactions.Count());
        transactionDetailReportResponse.Summary.TotalValue.ShouldBe(filteredTransactions.Sum(t => t.TransactionAmount));

        foreach (Transaction transaction in filteredTransactions)
        {
            var foundTxn = transactionDetailReportResponse.Transactions.SingleOrDefault(t => t.Id == transaction.TransactionId);
            foundTxn.ShouldNotBeNull(transaction.TransactionId.ToString());
        }
    }


    [Fact]
    public async Task TransactionsEndpoint_TransactionSummaryByMerchantReport_NoFilters_SummaryDataReturned()
    {
        var transactions = new List<Transaction>();

        TransactionProcessor.Database.Entities.Merchant merchant1 = this.merchantsList.SingleOrDefault(m => m.Name == "Test Merchant 1");
        var merchant2 = this.merchantsList.SingleOrDefault(m => m.Name == "Test Merchant 2");
        var merchant3 = this.merchantsList.SingleOrDefault(m => m.Name == "Test Merchant 3");
        var safaricomContract = this.contractList.SingleOrDefault(c => c.contractName == "Safaricom Contract");
        var voucherContract = this.contractList.SingleOrDefault(c => c.contractName == "Healthcare Centre 1 Contract");
        var safaricomProduct = this.contractProducts.Single(cp => cp.Key == safaricomContract.contractId).Value.First();
        var voucherProduct = this.contractProducts.Single(cp => cp.Key == voucherContract.contractId).Value.First();


        List<DateTime> transactionDates = [
            DateTime.Now.Date,
            DateTime.Now.Date.AddDays(-1),
            DateTime.Now.Date.AddDays(-2),
            DateTime.Now.Date.AddDays(-3)
        ];

        var merchants = new[] { merchant1, merchant2, merchant3 };
            
        Dictionary<(DateTime, TransactionProcessor.Database.Entities.Merchant), Int32> salesConfig = new Dictionary<(DateTime, TransactionProcessor.Database.Entities.Merchant), Int32>();
        salesConfig.Add((DateTime.Now.Date, merchant1), 15);
        salesConfig.Add((DateTime.Now.Date.AddDays(-1), merchant1), 25);
        salesConfig.Add((DateTime.Now.Date.AddDays(-2), merchant1), 24);
        salesConfig.Add((DateTime.Now.Date.AddDays(-3), merchant1), 19);
        salesConfig.Add((DateTime.Now.Date, merchant2), 15);
        salesConfig.Add((DateTime.Now.Date.AddDays(-1), merchant2), 25);
        salesConfig.Add((DateTime.Now.Date.AddDays(-2), merchant2), 24);
        salesConfig.Add((DateTime.Now.Date.AddDays(-3), merchant2), 19);
        salesConfig.Add((DateTime.Now.Date, merchant3), 15);
        salesConfig.Add((DateTime.Now.Date.AddDays(-1), merchant3), 25);
        salesConfig.Add((DateTime.Now.Date.AddDays(-2), merchant3), 24);
        salesConfig.Add((DateTime.Now.Date.AddDays(-3), merchant3), 19);

        Dictionary<TransactionProcessor.Database.Entities.Merchant, Int32> totalCountsByMerchant = new();
        Dictionary<TransactionProcessor.Database.Entities.Merchant, Int32> authorisedCountsByMerchant = new();
        Dictionary<TransactionProcessor.Database.Entities.Merchant, Int32> declinedCountsByMerchant = new();

        foreach (DateTime transactionDate in transactionDates) {

            foreach (TransactionProcessor.Database.Entities.Merchant merchant in merchants) {

                // Build merchant sales
                KeyValuePair<(DateTime, TransactionProcessor.Database.Entities.Merchant), Int32> config = salesConfig.SingleOrDefault(c => c.Key == (transactionDate, merchant));

                for (int i = 0; i < config.Value; i++) {
                    string responseCode = i switch {
                        var n when n % 4 == 2 => "1009", // change value on every 4rd iteration
                        _ => "0000"
                    };
                    if (!totalCountsByMerchant.ContainsKey(merchant))
                    {
                        totalCountsByMerchant.Add(merchant, 0);
                        authorisedCountsByMerchant.Add(merchant, 0);
                        declinedCountsByMerchant.Add(merchant, 0);
                    }

                    Transaction transaction = await this.helper.BuildTransactionX(transactionDate, merchant.MerchantId, 
                        safaricomContract.operatorId, safaricomContract.contractId, safaricomProduct.productId, responseCode, safaricomProduct.productValue);
                    transactions.Add(transaction);
                        
                    totalCountsByMerchant[merchant]++;
                    if (responseCode == "0000") {
                        authorisedCountsByMerchant[merchant]++;
                    }
                    else {
                        declinedCountsByMerchant[merchant]++;
                    }

                    Transaction transaction1 = await this.helper.BuildTransactionX(transactionDate, merchant.MerchantId,
                        voucherContract.operatorId, voucherContract.contractId, voucherProduct.productId, responseCode, voucherProduct.productValue);
                    transactions.Add(transaction1);

                    totalCountsByMerchant[merchant]++;
                    if (responseCode == "0000")
                    {
                        authorisedCountsByMerchant[merchant]++;
                    }
                    else
                    {
                        declinedCountsByMerchant[merchant]++;
                    }
                }
            }
        }
        await this.helper.AddTransactionsX(transactions);
            
        List<DateTime> orderedDates = transactionDates.OrderBy(x => x).ToList();
        TransactionDetailReportRequest request = new TransactionDetailReportRequest
        {
            StartDate = orderedDates.First(),
            EndDate = orderedDates.Last(),
            Merchants = [],
            Operators = [],
            Products = []
        };

        String payload = JsonConvert.SerializeObject(request);
            
        var result = await this.CreateAndSendHttpRequestMessage<DataTransferObjects.TransactionSummaryByMerchantResponse>($"{this.BaseRoute}/transactionsummarybymerchantreport", payload, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        DataTransferObjects.TransactionSummaryByMerchantResponse transactionSummaryByMerchantResponse = result.Data;

        transactionSummaryByMerchantResponse.ShouldNotBeNull();
        transactionSummaryByMerchantResponse.Summary.ShouldNotBeNull();
        transactionSummaryByMerchantResponse.Summary.TotalMerchants.ShouldBe(3);
        transactionSummaryByMerchantResponse.Summary.TotalCount.ShouldBe(transactions.Count);
        transactionSummaryByMerchantResponse.Summary.TotalValue.ShouldBe(transactions.Sum(t => t.TransactionAmount));

        transactionSummaryByMerchantResponse.Merchants.Count.ShouldBe(3);
        var operators = new List<Guid> { safaricomContract.operatorId, voucherContract.operatorId };
        foreach (var mr in merchants) {
            var row = transactionSummaryByMerchantResponse.Merchants.SingleOrDefault(m => m.MerchantId == mr.MerchantId);
            row.ShouldNotBeNull();
            row.MerchantId.ShouldBe(mr.MerchantId);
            row.TotalValue.ShouldBe(transactions.Where(t => t.MerchantId == mr.MerchantId).Sum(t => t.TransactionAmount));
            row.TotalCount.ShouldBe(transactions.Count(t => t.MerchantId == mr.MerchantId));
            row.AuthorisedCount.ShouldBe(authorisedCountsByMerchant[mr]);
            row.DeclinedCount.ShouldBe(declinedCountsByMerchant[mr]);
        }
    }

    [Fact]
    public async Task TransactionsEndpoint_TransactionSummaryByOperatorReport_NoFilters_SummaryDataReturned()
    {

        var transactions = new List<Transaction>();

        TransactionProcessor.Database.Entities.Merchant merchant1 = this.merchantsList.SingleOrDefault(m => m.Name == "Test Merchant 1");
        var merchant2 = this.merchantsList.SingleOrDefault(m => m.Name == "Test Merchant 2");
        var merchant3 = this.merchantsList.SingleOrDefault(m => m.Name == "Test Merchant 3");
        var safaricomContract = this.contractList.SingleOrDefault(c => c.contractName == "Safaricom Contract");
        var voucherContract = this.contractList.SingleOrDefault(c => c.contractName == "Healthcare Centre 1 Contract");
        var safaricomProduct = this.contractProducts.Single(cp => cp.Key == safaricomContract.contractId).Value.First();
        var voucherProduct = this.contractProducts.Single(cp => cp.Key == voucherContract.contractId).Value.First();


        List<DateTime> transactionDates = [
            DateTime.Now.Date,
            DateTime.Now.Date.AddDays(-1),
            DateTime.Now.Date.AddDays(-2),
            DateTime.Now.Date.AddDays(-3)
        ];

        var merchants = new[] { merchant1, merchant2, merchant3 };

        Dictionary<(DateTime, TransactionProcessor.Database.Entities.Merchant), Int32> salesConfig = new Dictionary<(DateTime, TransactionProcessor.Database.Entities.Merchant), Int32>();
        salesConfig.Add((DateTime.Now.Date, merchant1), 15);
        salesConfig.Add((DateTime.Now.Date.AddDays(-1), merchant1), 25);
        salesConfig.Add((DateTime.Now.Date.AddDays(-2), merchant1), 24);
        salesConfig.Add((DateTime.Now.Date.AddDays(-3), merchant1), 19);
        salesConfig.Add((DateTime.Now.Date, merchant2), 15);
        salesConfig.Add((DateTime.Now.Date.AddDays(-1), merchant2), 25);
        salesConfig.Add((DateTime.Now.Date.AddDays(-2), merchant2), 24);
        salesConfig.Add((DateTime.Now.Date.AddDays(-3), merchant2), 19);
        salesConfig.Add((DateTime.Now.Date, merchant3), 15);
        salesConfig.Add((DateTime.Now.Date.AddDays(-1), merchant3), 25);
        salesConfig.Add((DateTime.Now.Date.AddDays(-2), merchant3), 24);
        salesConfig.Add((DateTime.Now.Date.AddDays(-3), merchant3), 19);

        Dictionary<Guid, Int32> totalCountsByOperator = new();
        Dictionary<Guid, Int32> authorisedCountsByOperator = new();
        Dictionary<Guid, Int32> declinedCountsByOperator = new();

        foreach (DateTime transactionDate in transactionDates)
        {

            foreach (TransactionProcessor.Database.Entities.Merchant merchant in merchants)
            {

                // Build merchant sales
                KeyValuePair<(DateTime, TransactionProcessor.Database.Entities.Merchant), Int32> config = salesConfig.SingleOrDefault(c => c.Key == (transactionDate, merchant));

                for (int i = 0; i < config.Value; i++)
                {
                    string responseCode = i switch
                    {
                        var n when n % 4 == 2 => "1009", // change value on every 4rd iteration
                        _ => "0000"
                    };
                    if (!totalCountsByOperator.ContainsKey(safaricomContract.operatorId))
                    {
                        totalCountsByOperator.Add(safaricomContract.operatorId, 0);
                        authorisedCountsByOperator.Add(safaricomContract.operatorId, 0);
                        declinedCountsByOperator.Add(safaricomContract.operatorId, 0);
                    }

                    if (!totalCountsByOperator.ContainsKey(voucherContract.operatorId))
                    {
                        totalCountsByOperator.Add(voucherContract.operatorId, 0);
                        authorisedCountsByOperator.Add(voucherContract.operatorId, 0);
                        declinedCountsByOperator.Add(voucherContract.operatorId, 0);
                    }

                    Transaction transaction = await this.helper.BuildTransactionX(transactionDate, merchant.MerchantId,
                        safaricomContract.operatorId, safaricomContract.contractId, safaricomProduct.productId, responseCode, safaricomProduct.productValue);
                    transactions.Add(transaction);

                    totalCountsByOperator[safaricomContract.operatorId]++;
                    if (responseCode == "0000")
                    {
                        authorisedCountsByOperator[safaricomContract.operatorId]++;
                    }
                    else
                    {
                        declinedCountsByOperator[safaricomContract.operatorId]++;
                    }

                    Transaction transaction1 = await this.helper.BuildTransactionX(transactionDate, merchant.MerchantId,
                        voucherContract.operatorId, voucherContract.contractId, voucherProduct.productId, responseCode, voucherProduct.productValue);
                    transactions.Add(transaction1);

                    totalCountsByOperator[voucherContract.operatorId]++;
                    if (responseCode == "0000")
                    {
                        authorisedCountsByOperator[voucherContract.operatorId]++;
                    }
                    else
                    {
                        declinedCountsByOperator[voucherContract.operatorId]++;
                    }

                }
            }
        }
        await this.helper.AddTransactionsX(transactions);

        List<DateTime> orderedDates = transactionDates.OrderBy(x => x).ToList();
        TransactionDetailReportRequest request = new TransactionDetailReportRequest
        {
            StartDate = orderedDates.First(),
            EndDate = orderedDates.Last(),
            Merchants = [],
            Operators = [],
            Products = []
        };

        String payload = JsonConvert.SerializeObject(request);

        var result = await this.CreateAndSendHttpRequestMessage<DataTransferObjects.TransactionSummaryByOperatorResponse>($"{this.BaseRoute}/transactionsummarybyoperatorreport", payload, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        DataTransferObjects.TransactionSummaryByOperatorResponse transactionSummaryByOperatorResponse = result.Data;

        transactionSummaryByOperatorResponse.ShouldNotBeNull();
        transactionSummaryByOperatorResponse.Summary.ShouldNotBeNull();
        transactionSummaryByOperatorResponse.Summary.TotalOperators.ShouldBe(2);
        transactionSummaryByOperatorResponse.Summary.TotalCount.ShouldBe(transactions.Count);
        transactionSummaryByOperatorResponse.Summary.TotalValue.ShouldBe(transactions.Sum(t => t.TransactionAmount));

        transactionSummaryByOperatorResponse.Operators.Count.ShouldBe(2);
        var operators = new List<Guid> { safaricomContract.operatorId, voucherContract.operatorId };
        foreach (var op in operators)
        {
            var row = transactionSummaryByOperatorResponse.Operators.SingleOrDefault(o => o.OperatorId == op);
            row.ShouldNotBeNull();
            row.OperatorId.ShouldBe(op);
            row.TotalValue.ShouldBe(transactions.Where(t => t.OperatorId == op).Sum(t => t.TransactionAmount));
            row.TotalCount.ShouldBe(transactions.Count(t => t.OperatorId == op));
            row.AuthorisedCount.ShouldBe(authorisedCountsByOperator[op]);
            row.DeclinedCount.ShouldBe(declinedCountsByOperator[op]);
        }
    }


    [Fact]
    public async Task TransactionsEndpoint_ProductPerformanceReport_SummaryDataReturned() {
        Dictionary<(String  @operator, String product), Int32> productPerformanceData = new();
        productPerformanceData.Add(("Safaricom", "200 KES Topup"), 6);
        productPerformanceData.Add(("Safaricom", "100 KES Topup"), 10);
        productPerformanceData.Add(("Safaricom", "50 KES Topup"), 20);
        productPerformanceData.Add(("Safaricom", "Custom"), 5);
        productPerformanceData.Add(("Voucher", "10 KES Voucher"), 15);
        productPerformanceData.Add(("Voucher", "Custom"), 8);
        productPerformanceData.Add(("PataPawa PostPay", "Post Pay Bill Pay"), 12);
        productPerformanceData.Add(("PataPawa PrePay", "Pre Pay Bill Pay"), 18);

        var transactions = new List<Transaction>();

        TransactionProcessor.Database.Entities.Merchant merchant = this.merchantsList.SingleOrDefault(m => m.Name == "Test Merchant 1");

        List<DateTime> transactionDates = [
            DateTime.Now.Date,
            DateTime.Now.Date.AddDays(-1),
            DateTime.Now.Date.AddDays(-2),
            DateTime.Now.Date.AddDays(-3)
        ];

        foreach (DateTime transactionDate in transactionDates) {
            foreach (var productData in productPerformanceData) {
                var transactionCount = productData.Value;
                var contract = this.contractList.SingleOrDefault(c => c.operatorName == productData.Key.@operator);
                var productList = this.contractProducts.Where(cp => cp.Key == contract.contractId).SelectMany(cp => cp.Value).ToList();
                var product = productList.SingleOrDefault(p => p.productName == productData.Key.product);
                for (int i = 0; i < transactionCount; i++) {
                    Transaction transaction = await this.helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", product.productValue);
                    transactions.Add(transaction);
                }
            }
        }

        await this.helper.AddTransactionsX(transactions);

        var result = await this.CreateAndSendHttpRequestMessage<DataTransferObjects.ProductPerformanceResponse>($"{this.BaseRoute}/productperformancereport?startDate={DateTime.Now.Date.AddDays(-3):yyyy-MM-dd}&endDate={DateTime.Now.Date:yyyy-MM-dd}",  CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        var productPerformanceResponse= result.Data;

        productPerformanceResponse.ShouldNotBeNull();
        productPerformanceResponse.Summary.ShouldNotBeNull();
        productPerformanceResponse.Summary.TotalProducts.ShouldBe(8);
        productPerformanceResponse.Summary.TotalCount.ShouldBe(transactions.Count);
        productPerformanceResponse.Summary.TotalValue.ShouldBe(transactions.Sum(t => t.TransactionAmount));

        foreach (KeyValuePair<(String @operator, String product), Int32> productData in productPerformanceData) {
            var contract = this.contractList.SingleOrDefault(c => c.operatorName == productData.Key.@operator);
            var productList = this.contractProducts.Single(cp => cp.Key == contract.contractId).Value;
            var product = productList.SingleOrDefault(p => p.productName == productData.Key.product);
            var productPerformanceResponseDetail = productPerformanceResponse.ProductDetails.SingleOrDefault(p => p.ProductId == product.productId);
            productPerformanceResponseDetail.ShouldNotBeNull();
            productPerformanceResponseDetail.TransactionCount.ShouldBe(transactions.Count(t => t.ContractProductId == product.productId));
            productPerformanceResponseDetail.TransactionValue.ShouldBe(transactions.Where(t => t.ContractProductId == product.productId).Sum(t => t.TransactionAmount));
        }
    }

    private static int RandomizeCount(int baseCount, Random rnd, double variability = 0.3)
    {
        if (baseCount <= 0) return 0;
        // factor in [-variability, +variability]
        double factor = 1.0 + (rnd.NextDouble() * 2.0 - 1.0) * variability;
        int result = (int)Math.Round(baseCount * factor);
        return Math.Max(0, result);
    }

    [Fact]
    public async Task TransactionsEndpoint_TodaysSalesByHour_SummaryDataReturned()
    {
        Stopwatch sw = Stopwatch.StartNew();

        List<Transaction> todaysTransactions = new List<Transaction>();
        List<Transaction> comparisonDateTransactions = new List<Transaction>();

        Dictionary<string, int> transactionCounts = new() { { "Test Merchant 1", 3 }, { "Test Merchant 2", 6 }, { "Test Merchant 3", 2 }, { "Test Merchant 4", 0 } };

        // TODO: make counts dynamic
        DateTime todaysDateTime = DateTime.Now;

        for (int hour = 0; hour < 24; hour++)
        {
            List<Transaction> localList = new List<Transaction>();
            DateTime date = new DateTime(todaysDateTime.Year, todaysDateTime.Month, todaysDateTime.Day, hour, 0, 0);

            // Seed per-hour RNG deterministically so test results are reproducible per-day/per-hour
            var hourSeed = todaysDateTime.Date.GetHashCode() ^ hour;
            var hourRnd = new Random(hourSeed);

            foreach (var merchant in merchantsList)
            {
                foreach (var contract in contractList)
                {
                    var productList = contractProducts.Single(cp => cp.Key == contract.contractId).Value;
                    foreach ((Guid productId, String productName, Decimal? productValue, Int32 contractProductReportingId) product in productList)
                    {
                        var baseCount = transactionCounts.Single(m => m.Key == merchant.Name).Value;

                        // keep the original hour-based multipliers, but apply random variation to the final count
                        int hourMultiplierCount = hour switch
                        {
                            _ when hour >= 9 && hour < 18 => baseCount * hour, // business hours
                            _ when hour >= 18 && hour < 21 => baseCount * (24 - hour), // evening spike
                            _ => baseCount // off hours
                        };

                        int transactionCount = RandomizeCount(hourMultiplierCount, hourRnd, variability: 0.3);

                        for (int i = 0; i < transactionCount; i++)
                        {
                            Transaction transaction = await helper.BuildTransactionX(date, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", product.productValue);
                            todaysTransactions.Add(transaction);
                        }
                    }
                }
            }

            todaysTransactions.AddRange(localList);
        }

        await this.helper.AddTransactionsX(todaysTransactions);

        sw.Stop();
        this.TestOutputHelper.WriteLine($"Setup Todays Txns {sw.ElapsedMilliseconds}ms");
        sw.Restart();

        DateTime comparisonDate = todaysDateTime.AddDays(-1);
        for (int hour = 0; hour < 24; hour++)
        {
            List<Transaction> localList = new List<Transaction>();
            DateTime date = new DateTime(comparisonDate.Year, comparisonDate.Month, comparisonDate.Day, hour, 0, 0);

            // Separate deterministic seed for comparison date hour
            var compHourSeed = comparisonDate.Date.GetHashCode() ^ hour;
            var compHourRnd = new Random(compHourSeed);

            foreach (var merchant in merchantsList)
            {
                foreach (var contract in contractList)
                {
                    var productList = contractProducts.Single(cp => cp.Key == contract.contractId).Value;
                    foreach (var product in productList)
                    {
                        var baseCount = transactionCounts.Single(m => m.Key == merchant.Name).Value;

                        int hourMultiplierCount = hour switch
                        {
                            _ when hour >= 12 && hour < 18 => baseCount * hour, // business hours
                            _ when hour >= 18 && hour < 21 => baseCount * (24 - hour), // evening spike
                            _ => baseCount // off hours
                        };

                        int transactionCount = RandomizeCount(hourMultiplierCount, compHourRnd, variability: 0.3);

                        for (int i = 0; i < transactionCount; i++)
                        {
                            Transaction transaction = await helper.BuildTransactionX(date, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", product.productValue);
                            comparisonDateTransactions.Add(transaction);
                        }
                    }
                }
            }

            comparisonDateTransactions.AddRange(localList);

        }

        await this.helper.AddTransactionsX(comparisonDateTransactions);

        await this.helper.RunTodaysTransactionsSummaryProcessing(comparisonDate.Date);
        await this.helper.RunHistoricTransactionsSummaryProcessing(comparisonDate.Date);
        await this.helper.RunTodaysTransactionsSummaryProcessing(todaysDateTime.Date);

        var result = await this.CreateAndSendHttpRequestMessage<List<DataTransferObjects.TodaysSalesByHour>>($"{this.BaseRoute}/todayssalesbyhour?comparisondate={comparisonDate:yyyy-MM-dd}", CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        var todaysSalesByHour = result.Data;
        todaysSalesByHour.ShouldNotBeNull();

        foreach (var hour in todaysSalesByHour) {
            hour.ShouldNotBeNull();
            hour.TodaysSalesCount.ShouldBe(todaysTransactions.Count(t => t.TransactionDateTime.Hour == hour.Hour && t.TransactionTime <= DateTime.Now.TimeOfDay), hour.Hour.ToString());
            hour.TodaysSalesValue.ShouldBe(todaysTransactions.Where(t => t.TransactionDateTime.Hour == hour.Hour && t.TransactionTime <= DateTime.Now.TimeOfDay).Sum(t => t.TransactionAmount), hour.Hour.ToString());
            hour.ComparisonSalesCount.ShouldBe(comparisonDateTransactions.Count(t => t.TransactionDateTime.Hour == hour.Hour && t.TransactionTime <= DateTime.Now.TimeOfDay), hour.Hour.ToString());
            hour.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(t => t.TransactionDateTime.Hour == hour.Hour && t.TransactionTime <= DateTime.Now.TimeOfDay).Sum(t => t.TransactionAmount), hour.Hour.ToString());

        }
    }
}