using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Ductus.FluentDocker.Common;
using EstateReportingAPI.IntegrationTests;
using Microsoft.EntityFrameworkCore;
using TransactionProcessor.Database.Contexts;
using TransactionProcessor.Database.Entities;
using Xunit.Abstractions;

namespace EstateReportingAPI.IntegrationTests;

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using DataTrasferObjects;
using EstateReportingAPI.DataTransferObjects;
using EstateReportingAPI.Models;
using Microsoft.OpenApi.Services;
using Newtonsoft.Json;
using Shouldly;
using Xunit;
using Merchant = DataTrasferObjects.Merchant;
using SortDirection = DataTransferObjects.SortDirection;

public class FactTransactionsControllerTestsBase : ControllerTestsBase {
    
    protected override async Task ClearStandingData()
    {
        await helper.DeleteAllContracts();
        await helper.DeleteAllMerchants();
    }

    protected override async Task SetupStandingData()
    {
        Stopwatch sw = Stopwatch.StartNew();
        this.TestOutputHelper.WriteLine("Setting up standing data");

        // Estates
        await helper.AddEstate("Test Estate", "Ref1");
        sw.Stop();
        this.TestOutputHelper.WriteLine($"Setup Estate {sw.ElapsedMilliseconds}ms");
        sw.Restart();
        // Operators
        Int32 safaricomReportingId = await this.helper.AddOperator("Test Estate", "Safaricom");
        Int32 voucherReportingId = await this.helper.AddOperator("Test Estate", "Voucher");
        Int32 pataPawaPostPayReportingId = await this.helper.AddOperator("Test Estate", "PataPawa PostPay");
        Int32 pataPawaPrePay = await this.helper.AddOperator("Test Estate", "PataPawa PrePay");

        sw.Stop();
        this.TestOutputHelper.WriteLine($"Setup Operators {sw.ElapsedMilliseconds}ms");
        sw.Restart();
        // Merchants
        await helper.AddMerchant("Test Estate", "Test Merchant 1", DateTime.MinValue);
        await helper.AddMerchant("Test Estate", "Test Merchant 2", DateTime.MinValue);
        await helper.AddMerchant("Test Estate", "Test Merchant 3", DateTime.MinValue);
        await helper.AddMerchant("Test Estate", "Test Merchant 4", DateTime.MinValue);
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
                (c, o) => new { c.ContractId, c.Description, o.OperatorId, o.Name}
            )
            .ToList().Select(x => (x.ContractId, x.Description, x.OperatorId,x.Name))
            .ToList();
        
        var query1 = context.Contracts
            .GroupJoin(
                context.ContractProducts,
                c => c.ContractId,
                cp => cp.ContractId,
                (c, productGroup) => new
                {
                    c.ContractId,
                    Products = productGroup.Select(p => new { p.ContractProductId, p.ProductName, p.Value})
                        .OrderBy(p => p.ContractProductId)
                        .Select(p => new {p.ContractProductId,p.ProductName, p.Value})
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

    public FactTransactionsControllerTestsBase(ITestOutputHelper testOutputHelper) {
        this.TestOutputHelper = testOutputHelper;
    }
}

public class FactTransactionsControllerTests_OperatorsTests : FactTransactionsControllerTestsBase {
    public FactTransactionsControllerTests_OperatorsTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        
    }

    [Fact]
    public async Task FactTransactionsController_GetTopBottomOperatorsByValue_BottomOperators_OperatorsReturned() {
        DateTime todaysDateTime = DateTime.Now;

        Dictionary<string, int> transactionCounts = new() {
            { "Safaricom", 25 }, // 5000
            { "Voucher", 15 }, // 150 
            { "PataPawa PostPay", 45 }, // 3375
            { "PataPawa PrePay", 8 } // 600
        };

        Dictionary<string, List<Transaction>> transactionsDictionary = new();
        var merchant = merchantsList.First();
        foreach (KeyValuePair<string, int> transactionCount in transactionCounts) {
            var contract = contractList.Single(s => s.operatorName == transactionCount.Key);
            var products = contractProducts.Single(p => p.Key == contract.contractId);
            var product = products.Value.OrderByDescending(p => p.productValue.GetValueOrDefault()).First();
            for (int i = 0; i < transactionCount.Value; i++) {
                Transaction transaction = await helper.BuildTransactionX(todaysDateTime.AddHours(-1), merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", product.productValue);
                if (transactionsDictionary.ContainsKey(transactionCount.Key) == false) {
                    transactionsDictionary.Add(transactionCount.Key, new List<Transaction>());
                }

                transactionsDictionary[transactionCount.Key].Add(transaction);
            }
        }

        await this.helper.AddTransactionsX(transactionsDictionary.Values.SelectMany(t=> t).ToList());

        await helper.RunTodaysTransactionsSummaryProcessing(todaysDateTime.Date);

        var result = await ApiClient.GetTopBottomOperatorData(string.Empty, Guid.NewGuid(), DataTransferObjects.TopBottom.Bottom, 3, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        List<TopBottomOperatorData>? topBottomOperatorData = result.Data;
        topBottomOperatorData.ShouldNotBeNull();
        topBottomOperatorData[0].OperatorName.ShouldBe("Voucher");
        topBottomOperatorData[0].SalesValue.ShouldBe(transactionsDictionary["Voucher"].Sum(p => p.TransactionAmount));
        topBottomOperatorData[1].OperatorName.ShouldBe("PataPawa PrePay");
        topBottomOperatorData[1].SalesValue.ShouldBe(transactionsDictionary["PataPawa PrePay"].Sum(p => p.TransactionAmount));
        topBottomOperatorData[2].OperatorName.ShouldBe("PataPawa PostPay");
        topBottomOperatorData[2].SalesValue.ShouldBe(transactionsDictionary["PataPawa PostPay"].Sum(p => p.TransactionAmount));
    }

    
    [Fact]
    public async Task FactTransactionsController_GetTopBottomOperatorsByValue_TopOperators_OperatorsReturned() {
        DateTime todaysDateTime = DateTime.Now;

        Dictionary<string, int> transactionCounts = new() {
            { "Safaricom", 25 }, // 5000
            { "Voucher", 15 }, // 150 
            { "PataPawa PostPay", 45 }, // 3375
            { "PataPawa PrePay", 8 } // 600
        };

        Dictionary<string, List<Transaction>> transactionsDictionary = new();
        var merchant = merchantsList.First();
        foreach (KeyValuePair<string, int> transactionCount in transactionCounts)
        {
            var contract = contractList.Single(s => s.operatorName == transactionCount.Key);
            var products = contractProducts.Single(p => p.Key == contract.contractId);
            var product = products.Value.OrderByDescending(p => p.productValue.GetValueOrDefault()).First();
            for (int i = 0; i < transactionCount.Value; i++)
            {
                Transaction transaction = await helper.BuildTransactionX(todaysDateTime.AddHours(-1), merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", product.productValue);
                if (transactionsDictionary.ContainsKey(transactionCount.Key) == false)
                {
                    transactionsDictionary.Add(transactionCount.Key, new List<Transaction>());
                }

                transactionsDictionary[transactionCount.Key].Add(transaction);
            }
        }

        await this.helper.AddTransactionsX(transactionsDictionary.Values.SelectMany(t => t).ToList());

        await helper.RunTodaysTransactionsSummaryProcessing(todaysDateTime.Date);

        var result = await ApiClient.GetTopBottomOperatorData(string.Empty, Guid.NewGuid(), DataTransferObjects.TopBottom.Top, 3, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        List<TopBottomOperatorData>? topBottomOperatorData = result.Data;
        topBottomOperatorData[0].OperatorName.ShouldBe("Safaricom");
        topBottomOperatorData[0].SalesValue.ShouldBe(transactionsDictionary["Safaricom"].Sum(p => p.TransactionAmount));
        topBottomOperatorData[1].OperatorName.ShouldBe("PataPawa PostPay");
        topBottomOperatorData[1].SalesValue.ShouldBe(transactionsDictionary["PataPawa PostPay"].Sum(p => p.TransactionAmount));
        topBottomOperatorData[2].OperatorName.ShouldBe("PataPawa PrePay");
        topBottomOperatorData[2].SalesValue.ShouldBe(transactionsDictionary["PataPawa PrePay"].Sum(p => p.TransactionAmount));
    }
    
    [Fact]
    public async Task FactTransactionsControllerController_OperatorPerformance_SingleOperator_SalesReturned() {
        var todaysTransactions = new List<Transaction>();
        var comparisonDateTransactions = new List<Transaction>();

        DateTime todaysDateTime = DateTime.Now;
        DateTime comparisonDate = DateTime.Now.AddDays(-1).AddHours(-1);

        Dictionary<string, int> transactionCounts = new() {
            { "Safaricom", 25 }, // 5000
            { "Voucher", 15 }, // 150 
            { "PataPawa PostPay", 45 }, // 3375
            { "PataPawa PrePay", 8 } // 600
        };

        var merchant = merchantsList.First();
        foreach (KeyValuePair<string, int> transactionCount in transactionCounts)
        {
            var contract = contractList.Single(s => s.operatorName == transactionCount.Key);
            var products = contractProducts.Single(p => p.Key == contract.contractId);
            var product = products.Value.OrderByDescending(p => p.productValue.GetValueOrDefault()).First();
            for (int i = 0; i < transactionCount.Value; i++)
            {
                Transaction transaction = await helper.BuildTransactionX(todaysDateTime.AddHours(-1), merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", product.productValue);
                todaysTransactions.Add(transaction);
            }
        }

        await this.helper.AddTransactionsX(todaysTransactions);

        foreach (KeyValuePair<string, int> transactionCount in transactionCounts)
        {
            var contract = contractList.Single(s => s.operatorName == transactionCount.Key);
            var products = contractProducts.Single(p => p.Key == contract.contractId);
            var product = products.Value.OrderByDescending(p => p.productValue.GetValueOrDefault()).First();
            for (int i = 0; i < transactionCount.Value; i++)
            {
                Transaction transaction = await helper.BuildTransactionX(comparisonDate.AddHours(-1), merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", product.productValue);
                comparisonDateTransactions.Add(transaction);
            }
        }
        await this.helper.AddTransactionsX(comparisonDateTransactions);

        List<int> operatorFilterList = new List<int> { 2 };
        var operatorIdsForVerify = await context.Operators.Where(cp => cp.OperatorReportingId == 2).Select(cp => cp.OperatorId).ToListAsync(CancellationToken.None);


        string serializedArray = string.Join(",", operatorFilterList);

        await helper.RunTodaysTransactionsSummaryProcessing(comparisonDate.Date);
        await helper.RunHistoricTransactionsSummaryProcessing(comparisonDate.Date);
        await helper.RunTodaysTransactionsSummaryProcessing(todaysDateTime.Date);

        var result = await ApiClient.GetOperatorPerformance(string.Empty, Guid.NewGuid(), comparisonDate, operatorFilterList, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        DataTransferObjects.TodaysSales? todaysSales = result.Data;
        todaysSales.ShouldNotBeNull();
        todaysSales.ComparisonSalesCount.ShouldBe(comparisonDateTransactions.Count(c => operatorIdsForVerify.Contains(c.OperatorId)));
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(c => operatorIdsForVerify.Contains(c.OperatorId)).Sum(c => c.TransactionAmount));

        todaysSales.TodaysSalesCount.ShouldBe(todaysTransactions.Count(c => operatorIdsForVerify.Contains(c.OperatorId)));
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(c => operatorIdsForVerify.Contains(c.OperatorId)).Sum(c => c.TransactionAmount));
    }
    
    [Fact]
    public async Task FactTransactionsControllerController_OperatorPerformance_MultipleOperators_SalesReturned() {
        var todaysTransactions = new List<Transaction>();
        var comparisonDateTransactions = new List<Transaction>();

        DateTime todaysDateTime = DateTime.Now;
        DateTime comparisonDate = DateTime.Now.AddDays(-1).AddHours(-1);

        Dictionary<string, int> transactionCounts = new() {
            { "Safaricom", 25 }, // 5000
            { "Voucher", 15 }, // 150 
            { "PataPawa PostPay", 45 }, // 3375
            { "PataPawa PrePay", 8 } // 600
        };

        var merchant = merchantsList.First();
       foreach (KeyValuePair<string, int> transactionCount in transactionCounts)
       {
           var contract = contractList.Single(s => s.operatorName == transactionCount.Key);
           var products = contractProducts.Single(p => p.Key == contract.contractId);
           var product = products.Value.OrderByDescending(p => p.productValue.GetValueOrDefault()).First();
           for (int i = 0; i < transactionCount.Value; i++)
           {
               Transaction transaction = await helper.BuildTransactionX(todaysDateTime.AddHours(-1), merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", product.productValue);
               todaysTransactions.Add(transaction);
           }
       }
       
       await this.helper.AddTransactionsX(todaysTransactions);
       
       foreach (KeyValuePair<string, int> transactionCount in transactionCounts)
       {
           var contract = contractList.Single(s => s.operatorName == transactionCount.Key);
           var products = contractProducts.Single(p => p.Key == contract.contractId);
           var product = products.Value.OrderByDescending(p => p.productValue.GetValueOrDefault()).First();
           for (int i = 0; i < transactionCount.Value; i++)
           {
               Transaction transaction = await helper.BuildTransactionX(comparisonDate.AddHours(-1), merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", product.productValue);
               comparisonDateTransactions.Add(transaction);
           }
       }
       await this.helper.AddTransactionsX(comparisonDateTransactions);

        List<int> operatorFilterList = new List<int> { 2, 3 };

        var operatorIdsForVerify = await context.Operators.Where(cp => cp.OperatorReportingId >= 2 && cp.OperatorReportingId <= 3).Select(cp => cp.OperatorId).ToListAsync(CancellationToken.None);

        string serializedArray = string.Join(",", operatorFilterList);

        await helper.RunTodaysTransactionsSummaryProcessing(comparisonDate.Date);
        await helper.RunHistoricTransactionsSummaryProcessing(comparisonDate.Date);
        await helper.RunTodaysTransactionsSummaryProcessing(todaysDateTime.Date);

        var result = await ApiClient.GetOperatorPerformance(string.Empty, Guid.NewGuid(), comparisonDate, operatorFilterList, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        DataTransferObjects.TodaysSales? todaysSales = result.Data;
        todaysSales.ShouldNotBeNull();
        todaysSales.ComparisonSalesCount.ShouldBe(comparisonDateTransactions.Count(c => operatorIdsForVerify.Contains(c.OperatorId)));
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(c => operatorIdsForVerify.Contains(c.OperatorId)).Sum(c => c.TransactionAmount));

        todaysSales.TodaysSalesCount.ShouldBe(todaysTransactions.Count(c => operatorIdsForVerify.Contains(c.OperatorId)));
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(c => operatorIdsForVerify.Contains(c.OperatorId)).Sum(c => c.TransactionAmount));
    }
}

public class FactTransactionsControllerTests_ProductsTests : FactTransactionsControllerTestsBase {
    public FactTransactionsControllerTests_ProductsTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) {

    }


    [Fact]
    public async Task FactTransactionsControllerController_ProductPerformance_AllProducts_SalesReturned() {
        var todaysTransactions = new List<Transaction>();
        var comparisonDateTransactions = new List<Transaction>();

        DateTime todaysDateTime = DateTime.Now;
        DateTime comparisonDate = DateTime.Now.AddDays(-1).AddHours(-1);

        var merchant = merchantsList.First();
        var contract = contractList.Single(c => c.operatorName == "Safaricom");
        List<(Guid, String, Decimal?)> productList = contractProducts.Single(cp => cp.Key == contract.contractId).Value;

        Dictionary<string, int> transactionCounts = new() {
            { "200 KES Topup", 25 }, //5000
            { "100 KES Topup", 15 }, // 1500 
            { "50 KES Topup", 45 }, // 2250
            { "Custom", 8 } // 600
        };
        foreach ((Guid productId, String productName, Decimal? productValue) product in productList) {
            int transactionCount = transactionCounts.Single(m => m.Key == product.productName).Value;
            for (int i = 0; i < transactionCount; i++) {
                Transaction transaction = await helper.BuildTransactionX(todaysDateTime.AddHours(-1), merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", product.productValue);
                todaysTransactions.Add(transaction);
            }
        }

        await this.helper.AddTransactionsX(todaysTransactions);
        foreach ((Guid productId, String productName, Decimal? productValue) product in productList) {
            int transactionCount = transactionCounts.Single(m => m.Key == product.productName).Value;
            for (int i = 0; i < transactionCount; i++) {
                Transaction transaction = await helper.BuildTransactionX(comparisonDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", product.productValue);
                comparisonDateTransactions.Add(transaction);
            }
        }

        await this.helper.AddTransactionsX(comparisonDateTransactions);

        await helper.RunTodaysTransactionsSummaryProcessing(comparisonDate.Date);
        await helper.RunHistoricTransactionsSummaryProcessing(comparisonDate.Date);
        await helper.RunTodaysTransactionsSummaryProcessing(todaysDateTime.Date);

        var result = await ApiClient.GetProductPerformance(string.Empty, Guid.NewGuid(), comparisonDate, new List<Int32>(), CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        DataTransferObjects.TodaysSales? todaysSales = result.Data;
        todaysSales.ShouldNotBeNull();
        todaysSales.ComparisonSalesCount.ShouldBe(comparisonDateTransactions.Count);
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Sum(c => c.TransactionAmount));

        todaysSales.TodaysSalesCount.ShouldBe(todaysTransactions.Count);
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Sum(c => c.TransactionAmount));
    }


    [Fact]
    public async Task FactTransactionsControllerController_ProductPerformance_SingleProduct_SalesReturned() {
        var todaysTransactions = new List<Transaction>();
        var comparisonDateTransactions = new List<Transaction>();

        DateTime todaysDateTime = DateTime.Now;
        DateTime comparisonDate = DateTime.Now.AddDays(-1).AddHours(-1);

        var merchant = merchantsList.First();
        var contract = this.contractList.Single(c => c.operatorName == "Safaricom");
        var productList = contractProducts.Single(cp => cp.Key == contract.contractId).Value;

        Dictionary<string, int> transactionCounts = new() {
            { "200 KES Topup", 25 }, //5000
            { "100 KES Topup", 15 }, // 1500 
            { "50 KES Topup", 45 }, // 2250
            { "Custom", 8 } // 600
        };

        foreach ((Guid productId, String productName, Decimal? productValue) product in productList) {
                int transactionCount = transactionCounts.Single(m => m.Key == product.productName).Value;
                for (int i = 0; i < transactionCount; i++) {
                    Transaction transaction = await helper.BuildTransactionX(todaysDateTime.AddHours(-1), merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", product.productValue);
                    todaysTransactions.Add(transaction);
                }
            }

            await this.helper.AddTransactionsX(todaysTransactions);
            foreach ((Guid productId, String productName, Decimal? productValue) product in productList) {
                
                    int transactionCount = transactionCounts.Single(m => m.Key == product.productName).Value;
                    for (int i = 0; i < transactionCount; i++) {
                        Transaction transaction = await helper.BuildTransactionX(comparisonDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", product.productValue);
                        comparisonDateTransactions.Add(transaction);
                    }
                }

                await this.helper.AddTransactionsX(comparisonDateTransactions);

                List<int> productFilterList = new List<int> { 2 };

                var productIdsForVerify = await context.ContractProducts.Where(cp => cp.ContractProductReportingId == 2).Select(cp => cp.ContractProductId).ToListAsync(CancellationToken.None);

                await helper.RunTodaysTransactionsSummaryProcessing(comparisonDate.Date);
                await helper.RunHistoricTransactionsSummaryProcessing(comparisonDate.Date);
                await helper.RunTodaysTransactionsSummaryProcessing(todaysDateTime.Date);

                string serializedArray = string.Join(",", productFilterList);

                var result = await ApiClient.GetProductPerformance(string.Empty, Guid.NewGuid(), comparisonDate, productFilterList, CancellationToken.None);
                result.IsSuccess.ShouldBeTrue();
                DataTransferObjects.TodaysSales? todaysSales = result.Data;
                todaysSales.ShouldNotBeNull();
                todaysSales.ComparisonSalesCount.ShouldBe(comparisonDateTransactions.Count(c => productIdsForVerify.Contains(c.ContractProductId)));
                todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(c => productIdsForVerify.Contains(c.ContractProductId)).Sum(c => c.TransactionAmount));

                todaysSales.TodaysSalesCount.ShouldBe(todaysTransactions.Count(c => productIdsForVerify.Contains(c.ContractProductId)));
                todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(c => productIdsForVerify.Contains(c.ContractProductId)).Sum(c => c.TransactionAmount));
            }
        

        [Fact]
        public async Task FactTransactionsControllerController_ProductPerformance_MultipleProducts_SalesReturned() {
            var todaysTransactions = new List<Transaction>();
            var comparisonDateTransactions = new List<Transaction>();

            DateTime todaysDateTime = DateTime.Now;
            DateTime comparisonDate = DateTime.Now.AddDays(-1).AddHours(-1);

            var merchant = merchantsList.First();
            var contract = this.contractList.Single(c => c.operatorName == "Safaricom");
            var productList = contractProducts.Single(cp => cp.Key == contract.contractId).Value;

            Dictionary<string, int> transactionCounts = new() {
                { "200 KES Topup", 25 }, //5000
                { "100 KES Topup", 15 }, // 1500 
                { "50 KES Topup", 45 }, // 2250
                { "Custom", 8 } // 600
            };
            foreach ((Guid productId, String productName, Decimal? productValue) product in productList) {
                int transactionCount = transactionCounts.Single(m => m.Key == product.productName).Value;
                    for (int i = 0; i < transactionCount; i++) {
                        Transaction transaction = await helper.BuildTransactionX(todaysDateTime.AddHours(-1), merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", product.productValue);
                        todaysTransactions.Add(transaction);
                    }
                }

                await this.helper.AddTransactionsX(todaysTransactions);
                foreach ((Guid productId, String productName, Decimal? productValue) product in productList) {
                    int transactionCount = transactionCounts.Single(m => m.Key == product.productName).Value;
                        for (int i = 0; i < transactionCount; i++) {
                            Transaction transaction = await helper.BuildTransactionX(comparisonDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", product.productValue);
                            comparisonDateTransactions.Add(transaction);
                        }
                    }

                    await this.helper.AddTransactionsX(comparisonDateTransactions);

                    List<int> productFilterList = new List<int> { 2, 3 };
                    var productIdsForVerify = await context.ContractProducts.Where(cp => cp.ContractProductReportingId >= 2 && cp.ContractProductReportingId <= 3).Select(cp => cp.ContractProductId).ToListAsync(CancellationToken.None);

                    string serializedArray = string.Join(",", productFilterList);

                    await helper.RunTodaysTransactionsSummaryProcessing(comparisonDate.Date);
                    await helper.RunHistoricTransactionsSummaryProcessing(comparisonDate.Date);
                    await helper.RunTodaysTransactionsSummaryProcessing(todaysDateTime.Date);

                    var result = await ApiClient.GetProductPerformance(string.Empty, Guid.NewGuid(), comparisonDate, productFilterList, CancellationToken.None);
                    result.IsSuccess.ShouldBeTrue();
                    DataTransferObjects.TodaysSales? todaysSales = result.Data;
                    todaysSales.ShouldNotBeNull();
                    todaysSales.ComparisonSalesCount.ShouldBe(comparisonDateTransactions.Count(c => productIdsForVerify.Contains(c.ContractProductId)));
                    todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(c => productIdsForVerify.Contains(c.ContractProductId)).Sum(c => c.TransactionAmount));

                    todaysSales.TodaysSalesCount.ShouldBe(todaysTransactions.Count(c => productIdsForVerify.Contains(c.ContractProductId)));
                    todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(c => productIdsForVerify.Contains(c.ContractProductId)).Sum(c => c.TransactionAmount));
                }

                [Fact]
                public async Task FactTransactionsController_GetTopBottomProductsByValue_BottomProducts_ProductsReturned() {
                    DateTime todaysDateTime = DateTime.Now;

                    var merchant = merchantsList.First();
                    var contract = this.contractList.Single(c => c.operatorName == "Safaricom");
                    var productList = contractProducts.Single(cp => cp.Key == contract.contractId).Value;

                    Dictionary<string, int> transactionCounts = new() {
                        { "200 KES Topup", 25 }, //5000
                        { "100 KES Topup", 15 }, // 1500 
                        { "50 KES Topup", 45 }, // 2250
                        { "Custom", 8 } // 600
                    };
                    Dictionary<string, List<Transaction>> transactionsDictionary = new();
                    foreach ((Guid productId, String productName, Decimal? productValue) product in productList) {
                        int transactionCount = transactionCounts.Single(m => m.Key == product.productName).Value;
                        for (int i = 0; i < transactionCount; i++) {
                            Transaction transaction = await helper.BuildTransactionX(todaysDateTime.AddHours(-1), merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", product.productValue);
                            if (transactionsDictionary.ContainsKey(product.productName) == false) {
                                transactionsDictionary.Add(product.productName, new List<Transaction>());
                            }

                            transactionsDictionary[product.productName].Add(transaction);
                        }
                    }

                    await this.helper.AddTransactionsX(transactionsDictionary.Values.SelectMany(t => t).ToList());

                    await helper.RunTodaysTransactionsSummaryProcessing(todaysDateTime.Date);

                    var result = await ApiClient.GetTopBottomProductData(string.Empty, Guid.NewGuid(), DataTransferObjects.TopBottom.Bottom, 3, CancellationToken.None);
                    result.IsSuccess.ShouldBeTrue();
                    List<TopBottomProductData>? topBottomProductData = result.Data;

                    topBottomProductData[0].ProductName.ShouldBe("Custom");
                    topBottomProductData[0].SalesValue.ShouldBe(transactionsDictionary["Custom"].Sum(p => p.TransactionAmount));
                    topBottomProductData[1].ProductName.ShouldBe("100 KES Topup");
                    topBottomProductData[1].SalesValue.ShouldBe(transactionsDictionary["100 KES Topup"].Sum(p => p.TransactionAmount));
                    topBottomProductData[2].ProductName.ShouldBe("50 KES Topup");
                    topBottomProductData[2].SalesValue.ShouldBe(transactionsDictionary["50 KES Topup"].Sum(p => p.TransactionAmount));
                }


                [Fact]
                public async Task FactTransactionsController_GetTopBottomProductsByValue_TopProducts_ProductsReturned() {
                    DateTime todaysDateTime = DateTime.Now;

                    var merchant = merchantsList.First();
                    var contract = this.contractList.Single(c => c.operatorName == "Safaricom");
                    var productList = contractProducts.Single(cp => cp.Key == contract.contractId).Value;

                    Dictionary<string, int> transactionCounts = new() {
                        { "200 KES Topup", 25 }, //5000
                        { "100 KES Topup", 15 }, // 1500 
                        { "50 KES Topup", 45 }, // 2250
                        { "Custom", 8 } // 600
                    };

                    Dictionary<string, List<Transaction>> transactionsDictionary = new();
                    foreach ((Guid productId, String productName, Decimal? productValue) product in productList) {
                        
                            int transactionCount = transactionCounts.Single(m => m.Key == product.productName).Value;
                            for (int i = 0; i < transactionCount; i++) {
                                Transaction transaction = await helper.BuildTransactionX(todaysDateTime.AddHours(-1), merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", product.productValue);
                                if (transactionsDictionary.ContainsKey(product.productName) == false) {
                                    transactionsDictionary.Add(product.productName, new List<Transaction>());
                                }

                                transactionsDictionary[product.productName].Add(transaction);
                            }
                        }

                        await this.helper.AddTransactionsX(transactionsDictionary.Values.SelectMany(t => t).ToList());

                        await helper.RunTodaysTransactionsSummaryProcessing(todaysDateTime.Date);

                        var result = await ApiClient.GetTopBottomProductData(string.Empty, Guid.NewGuid(), DataTransferObjects.TopBottom.Top, 3, CancellationToken.None);
                        result.IsSuccess.ShouldBeTrue();
                        List<TopBottomProductData>? topBottomProductData = result.Data;
                        topBottomProductData[0].ProductName.ShouldBe("200 KES Topup");
                        topBottomProductData[0].SalesValue.ShouldBe(transactionsDictionary["200 KES Topup"].Sum(p => p.TransactionAmount));
                        topBottomProductData[1].ProductName.ShouldBe("50 KES Topup");
                        topBottomProductData[1].SalesValue.ShouldBe(transactionsDictionary["50 KES Topup"].Sum(p => p.TransactionAmount));
                        topBottomProductData[2].ProductName.ShouldBe("100 KES Topup");
                        topBottomProductData[2].SalesValue.ShouldBe(transactionsDictionary["100 KES Topup"].Sum(p => p.TransactionAmount));
                    }
                }

    public class FactTransactionsControllerTests_MerchantsTests : FactTransactionsControllerTestsBase {

        public FactTransactionsControllerTests_MerchantsTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) {

        }

        [Fact]
        public async Task FactTransactionsControllerController_GetMerchantsByLastDaleDate_MerchantsReturned() {
            DateTime todaysDateTime = DateTime.Now;

            await ClearStandingData();

            // Last Hour
            await helper.AddMerchant("Test Estate", "Merchant 1", todaysDateTime.AddMinutes(-10));
            await helper.AddMerchant("Test Estate", "Merchant 2", todaysDateTime.AddMinutes(-10));
            await helper.AddMerchant("Test Estate", "Merchant 3", todaysDateTime.AddMinutes(-10));
            await helper.AddMerchant("Test Estate", "Merchant 4", todaysDateTime.AddMinutes(-10));

            // Yesterday             
            await helper.AddMerchant("Test Estate", "Merchant 5", todaysDateTime.AddDays(-1));
            await helper.AddMerchant("Test Estate", "Merchant 6", todaysDateTime.AddDays(-1));
            await helper.AddMerchant("Test Estate", "Merchant 7", todaysDateTime.AddDays(-1));
            await helper.AddMerchant("Test Estate", "Merchant 8", todaysDateTime.AddDays(-1));
            await helper.AddMerchant("Test Estate", "Merchant 9", todaysDateTime.AddDays(-1));
            await helper.AddMerchant("Test Estate", "Merchant 10", todaysDateTime.AddDays(-1));

            // 5 days ago
            await helper.AddMerchant("Test Estate", "Merchant 11", todaysDateTime.AddDays(-5));
            await helper.AddMerchant("Test Estate", "Merchant 12", todaysDateTime.AddDays(-5));
            await helper.AddMerchant("Test Estate", "Merchant 13", todaysDateTime.AddDays(-5));

            // 10 Days Ago
            await helper.AddMerchant("Test Estate", "Merchant 14", todaysDateTime.AddDays(-10));
            await helper.AddMerchant("Test Estate", "Merchant 15", todaysDateTime.AddDays(-10));
            await helper.AddMerchant("Test Estate", "Merchant 16", todaysDateTime.AddDays(-10));
            await helper.AddMerchant("Test Estate", "Merchant 17", todaysDateTime.AddDays(-10));
            await helper.AddMerchant("Test Estate", "Merchant 18", todaysDateTime.AddDays(-10));

            DateTime startDate = DateTime.Now;
            DateTime endDate = DateTime.Now;

            // Test 1 - sale in last hour
            startDate = DateTime.Now.AddHours(-1);
            endDate = DateTime.Now;
            var result = await this.ApiClient.GetMerchantsByLastSaleDate(String.Empty, Guid.NewGuid(), startDate, endDate, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            var searchResult = result.Data;
            searchResult.ShouldNotBeNull();
            searchResult.Count.ShouldBe(4);
            searchResult.SingleOrDefault(s => s.Name == "Merchant 1").ShouldNotBeNull();
            searchResult.SingleOrDefault(s => s.Name == "Merchant 2").ShouldNotBeNull();
            searchResult.SingleOrDefault(s => s.Name == "Merchant 3").ShouldNotBeNull();
            searchResult.SingleOrDefault(s => s.Name == "Merchant 4").ShouldNotBeNull();

            // Test 2 - sale in last day but over an hour ago
            startDate = DateTime.Now.Date.AddDays(-1);
            endDate = DateTime.Now.AddHours(-1);
            result = await this.ApiClient.GetMerchantsByLastSaleDate(String.Empty, Guid.NewGuid(), startDate, endDate, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            searchResult = result.Data;
            searchResult.ShouldNotBeNull();
            searchResult.Count.ShouldBe(6);
            searchResult.SingleOrDefault(s => s.Name == "Merchant 5").ShouldNotBeNull();
            searchResult.SingleOrDefault(s => s.Name == "Merchant 6").ShouldNotBeNull();
            searchResult.SingleOrDefault(s => s.Name == "Merchant 7").ShouldNotBeNull();
            searchResult.SingleOrDefault(s => s.Name == "Merchant 8").ShouldNotBeNull();
            searchResult.SingleOrDefault(s => s.Name == "Merchant 9").ShouldNotBeNull();
            searchResult.SingleOrDefault(s => s.Name == "Merchant 10").ShouldNotBeNull();

            // Test 3 - sale in last  7 days but non yesterday
            startDate = DateTime.Now.Date.AddDays(-7);
            endDate = DateTime.Now.Date.AddDays(-1);
            result = await this.ApiClient.GetMerchantsByLastSaleDate(String.Empty, Guid.NewGuid(), startDate, endDate, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            searchResult = result.Data;
            searchResult.ShouldNotBeNull();
            searchResult.Count.ShouldBe(3);
            searchResult.SingleOrDefault(s => s.Name == "Merchant 11").ShouldNotBeNull();
            searchResult.SingleOrDefault(s => s.Name == "Merchant 12").ShouldNotBeNull();
            searchResult.SingleOrDefault(s => s.Name == "Merchant 13").ShouldNotBeNull();

            // Test 4 - sale more than 7 days ago 
            startDate = DateTime.Now.Date.AddYears(-1);
            endDate = DateTime.Now.Date.AddDays(-7);
            result = await this.ApiClient.GetMerchantsByLastSaleDate(String.Empty, Guid.NewGuid(), startDate, endDate, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            searchResult = result.Data;
            searchResult.ShouldNotBeNull();
            searchResult.Count.ShouldBe(5);
            searchResult.SingleOrDefault(s => s.Name == "Merchant 14").ShouldNotBeNull();
            searchResult.SingleOrDefault(s => s.Name == "Merchant 15").ShouldNotBeNull();
            searchResult.SingleOrDefault(s => s.Name == "Merchant 16").ShouldNotBeNull();
            searchResult.SingleOrDefault(s => s.Name == "Merchant 17").ShouldNotBeNull();
            searchResult.SingleOrDefault(s => s.Name == "Merchant 18").ShouldNotBeNull();
        }

        [Fact]
        public async Task FactTransactionsControllerController_GetMerchantsTransactionKpis_SalesReturned() {
            DateTime todaysDateTime = DateTime.Now;

            await ClearStandingData();

            // Last Hour
            await helper.AddMerchant("Test Estate", "Merchant 1", todaysDateTime.AddMinutes(-10));
            await helper.AddMerchant("Test Estate", "Merchant 2", todaysDateTime.AddMinutes(-10));
            await helper.AddMerchant("Test Estate", "Merchant 3", todaysDateTime.AddMinutes(-10));
            await helper.AddMerchant("Test Estate", "Merchant 4", todaysDateTime.AddMinutes(-10));

            // Yesterday             
            await helper.AddMerchant("Test Estate", "Merchant 5", todaysDateTime.AddDays(-1));
            await helper.AddMerchant("Test Estate", "Merchant 6", todaysDateTime.AddDays(-1));
            await helper.AddMerchant("Test Estate", "Merchant 7", todaysDateTime.AddDays(-1));
            await helper.AddMerchant("Test Estate", "Merchant 8", todaysDateTime.AddDays(-1));
            await helper.AddMerchant("Test Estate", "Merchant 9", todaysDateTime.AddDays(-1));
            await helper.AddMerchant("Test Estate", "Merchant 10", todaysDateTime.AddDays(-1));

            // 10 Days Ago
            await helper.AddMerchant("Test Estate", "Merchant 11", todaysDateTime.AddDays(-10));
            await helper.AddMerchant("Test Estate", "Merchant 12", todaysDateTime.AddDays(-10));
            await helper.AddMerchant("Test Estate", "Merchant 13", todaysDateTime.AddDays(-10));
            await helper.AddMerchant("Test Estate", "Merchant 14", todaysDateTime.AddDays(-10));
            await helper.AddMerchant("Test Estate", "Merchant 15", todaysDateTime.AddDays(-10));
            await helper.AddMerchant("Test Estate", "Merchant 16", todaysDateTime.AddDays(-10));
            await helper.AddMerchant("Test Estate", "Merchant 17", todaysDateTime.AddDays(-10));
            await helper.AddMerchant("Test Estate", "Merchant 18", todaysDateTime.AddDays(-10));

            var result = await ApiClient.GetMerchantKpi(string.Empty, Guid.NewGuid(), CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            DataTransferObjects.MerchantKpi? merchantKpi = result.Data;
            merchantKpi.ShouldNotBeNull();
            merchantKpi.MerchantsWithSaleInLastHour.ShouldBe(4);
            merchantKpi.MerchantsWithNoSaleToday.ShouldBe(6);
            merchantKpi.MerchantsWithNoSaleInLast7Days.ShouldBe(8);
        }

        [Fact]
        public async Task FactTransactionsController_GetTopBottomMerchantsByValue_BottomMerchants_MerchantsReturned() {
            DateTime todaysDateTime = DateTime.Now;

            Dictionary<string, int> transactionCounts = new() { { "Test Merchant 1", 25 }, { "Test Merchant 2", 15 }, { "Test Merchant 3", 45 }, { "Test Merchant 4", 8 } };

            Dictionary<string, List<Transaction>> transactionsDictionary = new();
            foreach (KeyValuePair<string, int> transactionCount in transactionCounts) {
                var merchant = merchantsList.Where(m => m.Name == transactionCount.Key).Single();
                var contract = this.contractList.Single(c => c.operatorName == "Safaricom");
                var product = contractProducts.Single(cp => cp.Key == contract.contractId).Value.First();
                for (int i = 0; i < transactionCount.Value; i++) {
                    Transaction transaction = await helper.BuildTransactionX(todaysDateTime.AddHours(-1), merchant.MerchantId, contract.operatorId, contract.contractId, product.Item1, "0000", product.productValue);
                    if (transactionsDictionary.ContainsKey(transactionCount.Key) == false) {
                        transactionsDictionary.Add(transactionCount.Key, new List<Transaction>());
                    }

                    transactionsDictionary[transactionCount.Key].Add(transaction);
                }
            }

            await this.helper.AddTransactionsX(transactionsDictionary.Values.SelectMany(t => t).ToList());

            await helper.RunTodaysTransactionsSummaryProcessing(todaysDateTime.Date);

            var result = await ApiClient.GetTopBottomMerchantData(string.Empty, Guid.NewGuid(), DataTransferObjects.TopBottom.Bottom, 3, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            List<TopBottomMerchantData>? topBottomMerchantData = result.Data;
            topBottomMerchantData.ShouldNotBeNull();
            topBottomMerchantData[0].MerchantName.ShouldBe("Test Merchant 4");
            topBottomMerchantData[0].SalesValue.ShouldBe(transactionsDictionary["Test Merchant 4"].Sum(p => p.TransactionAmount));
            topBottomMerchantData[1].MerchantName.ShouldBe("Test Merchant 2");
            topBottomMerchantData[1].SalesValue.ShouldBe(transactionsDictionary["Test Merchant 2"].Sum(p => p.TransactionAmount));
            topBottomMerchantData[2].MerchantName.ShouldBe("Test Merchant 1");
            topBottomMerchantData[2].SalesValue.ShouldBe(transactionsDictionary["Test Merchant 1"].Sum(p => p.TransactionAmount));
        }

        [Fact]
        public async Task FactTransactionsController_GetTopBottomMerchantsByValue_TopMerchants_MerchantsReturned() {
            DateTime todaysDateTime = DateTime.Now;

            Dictionary<string, int> transactionCounts = new() { { "Test Merchant 1", 25 }, { "Test Merchant 2", 15 }, { "Test Merchant 3", 45 }, { "Test Merchant 4", 8 } };

            Dictionary<string, List<Transaction>> transactionsDictionary = new();
            foreach (KeyValuePair<string, int> transactionCount in transactionCounts) {
                var merchant = merchantsList.Where(m => m.Name == transactionCount.Key).Single();
                var contract = this.contractList.Single(c => c.operatorName == "Safaricom");
                (Guid productId, String productName, Decimal? productValue) product = contractProducts.Single(cp => cp.Key == contract.contractId).Value.First();
                for (int i = 0; i < transactionCount.Value; i++) {
                    Transaction transaction = await helper.BuildTransactionX(todaysDateTime.AddHours(-1), merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", product.productValue);
                    if (transactionsDictionary.ContainsKey(transactionCount.Key) == false) {
                        transactionsDictionary.Add(transactionCount.Key, new List<Transaction>());
                    }

                    transactionsDictionary[transactionCount.Key].Add(transaction);
                }
            }

            await this.helper.AddTransactionsX(transactionsDictionary.Values.SelectMany(t => t).ToList());

            await helper.RunTodaysTransactionsSummaryProcessing(todaysDateTime.Date);
            var result = await ApiClient.GetTopBottomMerchantData(string.Empty, Guid.NewGuid(), DataTransferObjects.TopBottom.Top, 3, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            List<TopBottomMerchantData>? topBottomMerchantData = result.Data;
            topBottomMerchantData.ShouldNotBeNull();
            topBottomMerchantData[0].MerchantName.ShouldBe("Test Merchant 3");
            topBottomMerchantData[0].SalesValue.ShouldBe(transactionsDictionary["Test Merchant 3"].Sum(p => p.TransactionAmount));
            topBottomMerchantData[1].MerchantName.ShouldBe("Test Merchant 1");
            topBottomMerchantData[1].SalesValue.ShouldBe(transactionsDictionary["Test Merchant 1"].Sum(p => p.TransactionAmount));
            topBottomMerchantData[2].MerchantName.ShouldBe("Test Merchant 2");
            topBottomMerchantData[2].SalesValue.ShouldBe(transactionsDictionary["Test Merchant 2"].Sum(p => p.TransactionAmount));
        }

        [Fact]
        public async Task FactTransactionsControllerController_MerchantPerformance_AllMerchants_SalesReturned() {

            var todaysTransactions = new List<Transaction>();
            var comparisonDateTransactions = new List<Transaction>();

            DateTime todaysDateTime = DateTime.Now;
            DateTime comparisonDate = DateTime.Now.AddDays(-1).AddHours(-1);

            Dictionary<string, int> transactionCounts = new() {
                { "Test Merchant 1", 15 }, { "Test Merchant 2", 18 }, { "Test Merchant 3", 9 }, { "Test Merchant 4", 3 },
            };

            // Todays sales
            foreach (var merchant in merchantsList) {
                foreach (var contract in contractList) {
                    var productList = contractProducts.Single(cp => cp.Key == contract.contractId).Value;
                    foreach ((Guid productId, String productName, Decimal? productValue) product in productList) {
                        var transactionCount = transactionCounts.Single(m => m.Key == merchant.Name).Value;
                        for (int i = 0; i < transactionCount; i++) {
                            Transaction transaction = await helper.BuildTransactionX(todaysDateTime.AddHours(-1), merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", product.productValue);
                            todaysTransactions.Add(transaction);
                        }
                    }
                }
            }

            await this.helper.AddTransactionsX(todaysTransactions);

            // Comparison Date sales
            foreach (var merchant in merchantsList) {
                foreach (var contract in contractList) {
                    var productList = contractProducts.Single(cp => cp.Key == contract.contractId).Value;
                    foreach ((Guid productId, String productName, Decimal? productValue) product in productList) {
                        var transactionCount = transactionCounts.Single(m => m.Key == merchant.Name).Value;
                        for (int i = 0; i < transactionCount; i++) {
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

            var result = await ApiClient.GetMerchantPerformance(string.Empty, Guid.NewGuid(), comparisonDate, new List<Int32>(), CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            DataTransferObjects.TodaysSales? todaysSales = result.Data;
            todaysSales.ShouldNotBeNull();
            todaysSales.ComparisonSalesCount.ShouldBe(comparisonDateTransactions.Count);
            todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Sum(c => c.TransactionAmount));

            todaysSales.TodaysSalesCount.ShouldBe(todaysTransactions.Count);
            todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Sum(c => c.TransactionAmount));
        }

        [Fact]
        public async Task FactTransactionsControllerController_MerchantPerformance_SingleMerchant_SalesReturned() {

            var todaysTransactions = new List<Transaction>();
            var comparisonDateTransactions = new List<Transaction>();

            DateTime todaysDateTime = DateTime.Now;
            DateTime comparisonDate = DateTime.Now.AddDays(-1).AddHours(-1);

            Dictionary<string, int> transactionCounts = new() {
                { "Test Merchant 1", 15 }, { "Test Merchant 2", 18 }, { "Test Merchant 3", 9 }, { "Test Merchant 4", 3 },
            };

            // Todays sales
            foreach (var merchant in merchantsList) {
                foreach (var contract in contractList) {
                    var productList = contractProducts.Single(cp => cp.Key == contract.contractId).Value;
                    foreach ((Guid productId, String productName, Decimal? productValue) product in productList) {
                        var transactionCount = transactionCounts.Single(m => m.Key == merchant.Name).Value;
                        for (int i = 0; i < transactionCount; i++) {
                            Transaction transaction = await helper.BuildTransactionX(todaysDateTime.AddHours(-1), merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", product.productValue);
                            todaysTransactions.Add(transaction);
                        }
                    }
                }
            }

            await this.helper.AddTransactionsX(todaysTransactions);

            // Comparison Date sales
            foreach (var merchant in merchantsList) {
                foreach (var contract in contractList) {
                    var productList = contractProducts.Single(cp => cp.Key == contract.contractId).Value;
                    foreach ((Guid productId, String productName, Decimal? productValue) product in productList) {
                        var transactionCount = transactionCounts.Single(m => m.Key == merchant.Name).Value;
                        for (int i = 0; i < transactionCount; i++) {
                            Transaction transaction = await helper.BuildTransactionX(comparisonDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", product.productValue);
                            comparisonDateTransactions.Add(transaction);
                        }
                    }
                }
            }

            await this.helper.AddTransactionsX(comparisonDateTransactions);

            List<int> merchantFilterList = new List<int> { 2 };

            var merchantIdsForVerify = await this.context.Merchants.Where(m => m.MerchantReportingId == 2).Select(m => m.MerchantId).ToListAsync(CancellationToken.None);

            string serializedArray = string.Join(",", merchantFilterList);

            await helper.RunTodaysTransactionsSummaryProcessing(comparisonDate.Date);
            await helper.RunHistoricTransactionsSummaryProcessing(comparisonDate.Date);
            await helper.RunTodaysTransactionsSummaryProcessing(todaysDateTime.Date);

            var result = await ApiClient.GetMerchantPerformance(string.Empty, Guid.NewGuid(), comparisonDate, merchantFilterList, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            DataTransferObjects.TodaysSales? todaysSales = result.Data;
            todaysSales.ShouldNotBeNull();
            todaysSales.ComparisonSalesCount.ShouldBe(comparisonDateTransactions.Count(c => merchantIdsForVerify.Contains(c.MerchantId)));
            todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(c => merchantIdsForVerify.Contains(c.MerchantId)).Sum(c => c.TransactionAmount));

            todaysSales.TodaysSalesCount.ShouldBe(todaysTransactions.Count(c => merchantIdsForVerify.Contains(c.MerchantId)));
            todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(c => merchantIdsForVerify.Contains(c.MerchantId)).Sum(c => c.TransactionAmount));
        }

    }

    public class FactTransactionsControllerTests_SalesTests : FactTransactionsControllerTestsBase {

        public FactTransactionsControllerTests_SalesTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) {

        }

        #region Todays Sales Tests

        [Fact]
        public async Task FactTransactionsControllerController_TodaysSales_SalesReturned() {
            List<Transaction>? todaysTransactions = new List<Transaction>();
            List<Transaction> comparisonDateTransactions = new List<Transaction>();

            DateTime todaysDateTime = DateTime.Now;
            DateTime comparisonDate = DateTime.Now.AddDays(-1).AddHours(-1);

            Dictionary<string, int> transactionCounts = new() { { "Test Merchant 1", 15 }, { "Test Merchant 2", 18 }, { "Test Merchant 3", 9 }, { "Test Merchant 4", 0 } };

            foreach (var merchant in merchantsList) {
                foreach (var contract in contractList) {
                    var productList = contractProducts.Single(cp => cp.Key == contract.contractId).Value;
                    foreach ((Guid productId, String productName, Decimal? productValue) product in productList) {
                        var transactionCount = transactionCounts.Single(m => m.Key == merchant.Name).Value;
                        for (int i = 0; i < transactionCount; i++) {
                            Transaction transaction = await helper.BuildTransactionX(todaysDateTime.AddHours(-1), merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", product.productValue);
                            todaysTransactions.Add(transaction);
                        }
                    }
                }
            }

            await this.helper.AddTransactionsX(todaysTransactions);

            // Comparison Date sales
            foreach (var merchant in merchantsList) {
                foreach (var contract in contractList) {
                    var productList = contractProducts.Single(cp => cp.Key == contract.contractId).Value;
                    foreach ((Guid productId, String productName, Decimal? productValue) product in productList) {
                        var transactionCount = transactionCounts.Single(m => m.Key == merchant.Name).Value;
                        for (int i = 0; i < transactionCount; i++) {
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

            var result = await ApiClient.GetTodaysSales(string.Empty, Guid.NewGuid(), 0, 0, comparisonDate, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            var todaysSales = result.Data;

            todaysSales.ShouldNotBeNull();
            todaysSales.ComparisonSalesCount.ShouldBe(comparisonDateTransactions.Count);
            todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Sum(c => c.TransactionAmount));

            todaysSales.TodaysSalesCount.ShouldBe(todaysTransactions.Count);
            todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Sum(c => c.TransactionAmount));
        }

        [Fact]
        public async Task FactTransactionsControllerController_TodaysSales_OperatorFilter_SalesReturned() {
            List<Transaction> todaysTransactions = new();
            List<Transaction> comparisonDateTransactions = new();

            DateTime todaysDateTime = DateTime.Now;
            DateTime comparisonDate = DateTime.Now.AddDays(-1).AddHours(-1);

            Dictionary<string, int> transactionCounts = new() { { "Test Merchant 1", 15 }, { "Test Merchant 2", 18 }, { "Test Merchant 3", 9 }, { "Test Merchant 4", 0 } };

            // Todays sales
            foreach (var merchant in merchantsList) {
                foreach (var contract in contractList) {
                    var productList = contractProducts.Single(cp => cp.Key == contract.contractId).Value;
                    foreach ((Guid productId, String productName, Decimal? productValue) product in productList) {
                        var transactionCount = transactionCounts.Single(m => m.Key == merchant.Name).Value;
                        for (int i = 0; i < transactionCount; i++) {
                            Transaction transaction = await helper.BuildTransactionX(todaysDateTime.AddHours(-1), merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", product.productValue);
                            todaysTransactions.Add(transaction);
                        }
                    }
                }
            }

            await this.helper.AddTransactionsX(todaysTransactions);

            // Comparison Date sales
            foreach (var merchant in merchantsList) {
                foreach (var contract in contractList) {
                    var productList = contractProducts.Single(cp => cp.Key == contract.contractId).Value;
                    foreach ((Guid productId, String productName, Decimal? productValue) product in productList) {
                        var transactionCount = transactionCounts.Single(m => m.Key == merchant.Name).Value;
                        for (int i = 0; i < transactionCount; i++) {
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

            var result = await ApiClient.GetTodaysSales(string.Empty, Guid.NewGuid(), 0, 1, comparisonDate, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            var todaysSales = result.Data;

            todaysSales.ShouldNotBeNull();

            var operatorId = await this.helper.GetOperatorId(1, CancellationToken.None);
            todaysSales.ComparisonSalesCount.ShouldBe(comparisonDateTransactions.Count(c => c.OperatorId == operatorId));
            todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(c => c.OperatorId == operatorId).Sum(c => c.TransactionAmount));

            todaysSales.TodaysSalesCount.ShouldBe(todaysTransactions.Count(c => c.OperatorId == operatorId));
            todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(c => c.OperatorId == operatorId).Sum(c => c.TransactionAmount));
        }

        [Fact]
        public async Task FactTransactionsControllerController_TodaysSales_MerchantFilter_SalesReturned() {
            List<Transaction> todaysTransactions = new();
            List<Transaction> comparisonDateTransactions = new();

            DateTime todaysDateTime = DateTime.Now;
            DateTime comparisonDate = DateTime.Now.AddDays(-1).AddHours(-1);

            Dictionary<string, int> transactionCounts = new() { { "Test Merchant 1", 15 }, { "Test Merchant 2", 18 }, { "Test Merchant 3", 9 }, { "Test Merchant 4", 0 } };


            foreach (var merchant in merchantsList) {
                foreach (var contract in contractList) {
                    var productList = contractProducts.Single(cp => cp.Key == contract.contractId).Value;
                    foreach ((Guid productId, String productName, Decimal? productValue) product in productList) {
                        var transactionCount = transactionCounts.Single(m => m.Key == merchant.Name).Value;
                        for (int i = 0; i < transactionCount; i++) {
                            Transaction transaction = await helper.BuildTransactionX(todaysDateTime.AddHours(-1), merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", product.productValue);
                            todaysTransactions.Add(transaction);
                        }
                    }
                }
            }

            await this.helper.AddTransactionsX(todaysTransactions);


            foreach (var merchant in merchantsList) {
                foreach (var contract in contractList) {
                    var productList = contractProducts.Single(cp => cp.Key == contract.contractId).Value;
                    foreach ((Guid productId, String productName, Decimal? productValue) product in productList) {
                        var transactionCount = transactionCounts.Single(m => m.Key == merchant.Name).Value;
                        for (int i = 0; i < transactionCount; i++) {
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

            var result = await ApiClient.GetTodaysSales(string.Empty, Guid.NewGuid(), 1, 0, comparisonDate, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            var todaysSales = result.Data;

            todaysSales.ShouldNotBeNull();

            var merchantId = await this.helper.GetMerchantId(1, CancellationToken.None);
            todaysSales.ComparisonSalesCount.ShouldBe(comparisonDateTransactions.Count(c => c.MerchantId == merchantId));
            todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(c => c.MerchantId == merchantId).Sum(c => c.TransactionAmount));

            todaysSales.TodaysSalesCount.ShouldBe(todaysTransactions.Count(c => c.MerchantId == merchantId));
            todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(c => c.MerchantId == merchantId).Sum(c => c.TransactionAmount));
        }

        [Fact]
        public async Task FactTransactionsControllerController_TodaysSalesCountByHour_SalesReturned() {
            Stopwatch sw = Stopwatch.StartNew();

            List<Transaction> todaysTransactions = new List<Transaction>();
            List<Transaction> comparisonDateTransactions = new List<Transaction>();

            Dictionary<string, int> transactionCounts = new() { { "Test Merchant 1", 3 }, { "Test Merchant 2", 6 }, { "Test Merchant 3", 2 }, { "Test Merchant 4", 0 } };

            // TODO: make counts dynamic
            DateTime todaysDateTime = DateTime.Now;

            for (int hour = 0; hour < 24; hour++) {
                List<Transaction> localList = new List<Transaction>();
                DateTime date = new DateTime(todaysDateTime.Year, todaysDateTime.Month, todaysDateTime.Day, hour, 0, 0);
                foreach (var merchant in merchantsList) {
                    foreach (var contract in contractList) {
                        var productList = contractProducts.Single(cp => cp.Key == contract.contractId).Value;
                        foreach ((Guid productId, String productName, Decimal? productValue) product in productList) {
                            var transactionCount = transactionCounts.Single(m => m.Key == merchant.Name).Value;
                            for (int i = 0; i < transactionCount; i++) {
                                Transaction transaction = await helper.BuildTransactionX(todaysDateTime.AddHours(-1), merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", product.productValue);
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
            for (int hour = 0; hour < 24; hour++) {
                List<Transaction> localList = new List<Transaction>();
                DateTime date = new DateTime(comparisonDate.Year, comparisonDate.Month, comparisonDate.Day, hour, 0, 0);
                foreach (var merchant in merchantsList) {
                    foreach (var contract in contractList) {
                        var productList = contractProducts.Single(cp => cp.Key == contract.contractId).Value;
                        foreach ((Guid productId, String productName, Decimal? productValue) product in productList) {
                            var transactionCount = transactionCounts.Single(m => m.Key == merchant.Name).Value;
                            for (int i = 0; i < transactionCount; i++) {
                                Transaction transaction = await helper.BuildTransactionX(comparisonDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", product.productValue);
                                comparisonDateTransactions.Add(transaction);
                            }
                        }
                    }
                }

                comparisonDateTransactions.AddRange(localList);

            }

            await this.helper.AddTransactionsX(comparisonDateTransactions);

            sw.Stop();
            this.TestOutputHelper.WriteLine($"Setup Comparison txns {sw.ElapsedMilliseconds}ms");
            sw.Restart();

            await helper.RunTodaysTransactionsSummaryProcessing(comparisonDate.Date);
            await helper.RunHistoricTransactionsSummaryProcessing(comparisonDate.Date);
            await helper.RunTodaysTransactionsSummaryProcessing(todaysDateTime.Date);

            sw.Stop();
            this.TestOutputHelper.WriteLine($"Setup Summaries {sw.ElapsedMilliseconds}ms");
            sw.Restart();

            var result = await ApiClient.GetTodaysSalesCountByHour(string.Empty, Guid.NewGuid(), 0, 0, comparisonDate, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            var todaysSalesCountByHour = result.Data;

            todaysSalesCountByHour.ShouldNotBeNull();
            foreach (DataTransferObjects.TodaysSalesCountByHour salesCountByHour in todaysSalesCountByHour) {
                IEnumerable<Transaction> todayHour = todaysTransactions.Where(t => t.TransactionDateTime.Hour == salesCountByHour.Hour);
                IEnumerable<Transaction> comparisonHour = comparisonDateTransactions.Where(t => t.TransactionDateTime.Hour == salesCountByHour.Hour);
                salesCountByHour.ComparisonSalesCount.ShouldBe(comparisonHour.Count());
                salesCountByHour.TodaysSalesCount.ShouldBe(todayHour.Count());
            }
        }

        [Fact]
        public async Task FactTransactionsControllerController_TodaysSalesValueByHour_SalesReturned() {
            var todaysTransactions = new List<Transaction>();
            var comparisonDateTransactions = new List<Transaction>();

            Dictionary<string, int> transactionCounts = new() { { "Test Merchant 1", 3 }, { "Test Merchant 2", 6 }, { "Test Merchant 3", 2 }, { "Test Merchant 4", 0 } };

            DateTime todaysDateTime = DateTime.Now;

            for (int hour = 0; hour < 24; hour++) {
                DateTime date = new DateTime(todaysDateTime.Year, todaysDateTime.Month, todaysDateTime.Day, hour, 0, 0);
                foreach (var merchant in merchantsList) {
                    foreach (var contract in contractList) {
                        var productList = contractProducts.Single(cp => cp.Key == contract.contractId).Value;
                        foreach ((Guid productId, String productName, Decimal? productValue) product in productList) {
                            var transactionCount = transactionCounts.Single(m => m.Key == merchant.Name).Value;
                            for (int i = 0; i < transactionCount; i++) {
                                Transaction transaction = await helper.BuildTransactionX(todaysDateTime.AddHours(-1), merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", product.productValue);
                                todaysTransactions.Add(transaction);
                            }
                        }
                    }
                }
            }

            await this.helper.AddTransactionsX(todaysTransactions);

            DateTime comparisonDate = todaysDateTime.AddDays(-1);
            for (int hour = 0; hour < 24; hour++) {
                DateTime date = new DateTime(comparisonDate.Year, comparisonDate.Month, comparisonDate.Day, hour, 0, 0);
                foreach (var merchant in merchantsList) {
                    foreach (var contract in contractList) {
                        var productList = contractProducts.Single(cp => cp.Key == contract.contractId).Value;
                        foreach ((Guid productId, String productName, Decimal? productValue) product in productList) {
                            var transactionCount = transactionCounts.Single(m => m.Key == merchant.Name).Value;
                            for (int i = 0; i < transactionCount; i++) {
                                Transaction transaction = await helper.BuildTransactionX(comparisonDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", product.productValue);
                                comparisonDateTransactions.Add(transaction);
                            }
                        }
                    }
                }
            }

            await this.helper.AddTransactionsX(comparisonDateTransactions);

            await helper.RunTodaysTransactionsSummaryProcessing(comparisonDate.Date);
            await helper.RunHistoricTransactionsSummaryProcessing(comparisonDate.Date);
            await helper.RunTodaysTransactionsSummaryProcessing(todaysDateTime.Date);

            var result = await ApiClient.GetTodaysSalesValueByHour(string.Empty, Guid.NewGuid(), 0, 0, comparisonDate, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            List<DataTransferObjects.TodaysSalesValueByHour>? todaysSalesValueByHour = result.Data;
            foreach (DataTransferObjects.TodaysSalesValueByHour salesValueByHour in todaysSalesValueByHour) {
                IEnumerable<Transaction> todayHour = todaysTransactions.Where(t => t.TransactionDateTime.Hour == salesValueByHour.Hour);
                IEnumerable<Transaction> comparisonHour = comparisonDateTransactions.Where(t => t.TransactionDateTime.Hour == salesValueByHour.Hour);
                salesValueByHour.ComparisonSalesValue.ShouldBe(comparisonHour.Sum(c => c.TransactionAmount));
                salesValueByHour.TodaysSalesValue.ShouldBe(todayHour.Sum(c => c.TransactionAmount));
            }
        }

        #endregion


        [Fact]
        public async Task FactTransactionsControllerController_TodaysFailedSales_SalesReturned() {
            EstateManagementGenericContext context = new EstateManagementSqlServerContext(GetLocalConnectionString($"EstateReportingReadModel{TestId.ToString()}"));
            var todaysTransactions = new List<Transaction>();
            var comparisonDateTransactions = new List<Transaction>();
            DatabaseHelper helper = new DatabaseHelper(context);
            // TODO: make counts dynamic
            DateTime todaysDateTime = DateTime.Now;

            Dictionary<string, int> transactionCounts = new() { { "Test Merchant 1", 3 }, { "Test Merchant 2", 6 }, { "Test Merchant 3", 2 }, { "Test Merchant 4", 0 } };

            DateTime comparisonDate = todaysDateTime.AddDays(-1);

            foreach (var merchant in merchantsList) {
                foreach (var contract in contractList) {
                    var productList = contractProducts.Single(cp => cp.Key == contract.contractId).Value;
                    foreach ((Guid productId, String productName, Decimal? productValue) product in productList) {
                        var transactionCount = transactionCounts.Single(m => m.Key == merchant.Name).Value;
                        for (int i = 0; i < transactionCount; i++) {
                            Transaction transaction = await helper.BuildTransactionX(todaysDateTime.AddHours(-1), merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "1009", product.productValue);
                            todaysTransactions.Add(transaction);
                        }
                    }
                }
            }

            await this.helper.AddTransactionsX(todaysTransactions);

            // Comparison Date sales
            foreach (var merchant in merchantsList) {
                foreach (var contract in contractList) {
                    var productList = contractProducts.Single(cp => cp.Key == contract.contractId).Value;
                    foreach ((Guid productId, String productName, Decimal? productValue) product in productList) {
                        var transactionCount = transactionCounts.Single(m => m.Key == merchant.Name).Value;
                        for (int i = 0; i < transactionCount; i++) {
                            Transaction transaction = await helper.BuildTransactionX(comparisonDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "1009", product.productValue);
                            comparisonDateTransactions.Add(transaction);
                        }
                    }
                }
            }

            await this.helper.AddTransactionsX(comparisonDateTransactions);


            await helper.RunTodaysTransactionsSummaryProcessing(comparisonDate.Date);
            await helper.RunHistoricTransactionsSummaryProcessing(comparisonDate.Date);
            await helper.RunTodaysTransactionsSummaryProcessing(todaysDateTime.Date);

            var result = await ApiClient.GetTodaysFailedSales(string.Empty, Guid.NewGuid(), 1, 1, "1009", comparisonDate, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            DataTransferObjects.TodaysSales? todaysSales = result.Data;

            todaysSales.ShouldNotBeNull();
            todaysSales.ComparisonSalesCount.ShouldBe(comparisonDateTransactions.Count);
            todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Sum(c => c.TransactionAmount));

            todaysSales.TodaysSalesCount.ShouldBe(todaysTransactions.Count);
            todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Sum(c => c.TransactionAmount));
        }


        [Fact]
        public async Task FactTransactionsController_TransactionSearch_NoAdditionalFiltering_TransactionReturned() {

            DateTime transactionDate = new DateTime(2024, 3, 19);
            var merchant = merchantsList.Single(m => m.Name == "Test Merchant 1");
            var contract = contractList.Single(c => c.operatorName == "Safaricom");
            var contractProducts = this.contractProducts.Where(cp => cp.Key == contract.contractId);
            (Guid productId, String productName, Decimal? productValue) product = contractProducts.First().Value.Where(x => x.productName == "200 KES Topup").Single();

            // Add some transactions
            List<Transaction> transactions = new List<Transaction>();
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 1, transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 2, transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 3, transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 4, transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 5, transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 6, transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 7, transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 8, transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 9, transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 10, transactionAmount: product.productValue));
            await this.helper.AddTransactionsX(transactions);

            DataTransferObjects.TransactionSearchRequest searchRequest = new() { QueryDate = transactionDate };

            // No Paging or Sorting
            var result = await ApiClient.TransactionSearch(string.Empty, Guid.NewGuid(), searchRequest, null, null, null, null, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            var searchResult = result.Data;
            searchResult.Count.ShouldBe(10);
            searchResult.Any(s => s.TransactionReportingId == 1).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 2).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 3).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 4).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 5).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 6).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 7).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 8).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 9).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 10).ShouldBeTrue();


            result = await ApiClient.TransactionSearch(string.Empty, Guid.NewGuid(), searchRequest, 1, 5, null, null, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            searchResult = result.Data;
            searchResult.Count.ShouldBe(5);
            searchResult.Any(s => s.TransactionReportingId == 1).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 2).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 3).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 4).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 5).ShouldBeTrue();

            result = await ApiClient.TransactionSearch(string.Empty, Guid.NewGuid(), searchRequest, 2, 5, null, null, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            searchResult = result.Data;
            searchResult.Count.ShouldBe(5);
            searchResult.Any(s => s.TransactionReportingId == 6).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 7).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 8).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 9).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 10).ShouldBeTrue();
        }

        [Fact]
        public async Task FactTransactionsController_TransactionSearch_ValueRangeFiltering_TransactionReturned() {

            DateTime transactionDate = new DateTime(2024, 3, 19);

            var merchant = merchantsList.Single(m => m.Name == "Test Merchant 1");
            var contract = contractList.Single(c => c.operatorName == "Safaricom");
            var contractProducts = this.contractProducts.Where(cp => cp.Key == contract.contractId);
            var product = contractProducts.First().Value.Where(x => x.productName == "Custom").Single();

            // Add some transactions
            List<Transaction> transactions = new List<Transaction>();
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 1, transactionAmount: 50));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 2, transactionAmount: 123));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 3, transactionAmount: 100));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 4, transactionAmount: 101));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 5, transactionAmount: 199));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 6, transactionAmount: 150));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 7, transactionAmount: 200));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 8, transactionAmount: 201));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 9, transactionAmount: 99));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 10, transactionAmount: 111));
            await this.helper.AddTransactionsX(transactions);

            DataTransferObjects.TransactionSearchRequest searchRequest = new() { QueryDate = transactionDate, ValueRange = new() { StartValue = 100, EndValue = 200 } };

            // No Paging
            var result = await ApiClient.TransactionSearch(string.Empty, Guid.NewGuid(), searchRequest, null, null, null, null, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            List<DataTransferObjects.TransactionResult> searchResult = result.Data;
            searchResult.Count.ShouldBe(7);
            searchResult.Any(s => s.TransactionReportingId == 2).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 3).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 4).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 5).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 6).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 7).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 10).ShouldBeTrue();

            result = await ApiClient.TransactionSearch(string.Empty, Guid.NewGuid(), searchRequest, 1, 3, null, null, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            searchResult = result.Data;
            searchResult.Count.ShouldBe(3);
            searchResult.Any(s => s.TransactionReportingId == 10).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 3).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 4).ShouldBeTrue();

            result = await ApiClient.TransactionSearch(string.Empty, Guid.NewGuid(), searchRequest, 2, 3, null, null, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            searchResult = result.Data;
            searchResult.Count.ShouldBe(3);
            searchResult.Any(s => s.TransactionReportingId == 2).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 6).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 5).ShouldBeTrue();

        }

        [Fact]
        public async Task FactTransactionsController_TransactionSearch_AuthCodeFiltering_TransactionReturned() {

            DateTime transactionDate = new DateTime(2024, 3, 19);
            var merchant = merchantsList.Single(m => m.Name == "Test Merchant 1");
            var contract = contractList.Single(c => c.operatorName == "Safaricom");
            var contractProducts = this.contractProducts.Where(cp => cp.Key == contract.contractId);
            var product = contractProducts.First().Value.Where(x => x.productName == "200 KES Topup").Single();

            List<Transaction> transactions = new List<Transaction>();
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 1, authCode: "AUTH231", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 2, authCode: "AUTH1232", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 3, authCode: "AUTH1233", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 4, authCode: "AUTH1234", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 5, authCode: "AUTH1235", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 6, authCode: "AUTH1236", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 7, authCode: "AUTH1237", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 8, authCode: "AUTH1228", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 9, authCode: "AUTH1229", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 10, authCode: "AUTH1123", transactionAmount: product.productValue));
            await this.helper.AddTransactionsX(transactions);

            DataTransferObjects.TransactionSearchRequest searchRequest = new() { QueryDate = transactionDate, AuthCode = "AUTH1235" };

            // No Paging
            var result = await ApiClient.TransactionSearch(string.Empty, Guid.NewGuid(), searchRequest, null, null, null, null, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            List<DataTransferObjects.TransactionResult> searchResult = result.Data;
            searchResult.Count.ShouldBe(1);
            searchResult.Any(s => s.TransactionReportingId == 5).ShouldBeTrue();
        }


        [Fact]
        public async Task FactTransactionsController_TransactionSearch_TransactionNumberFiltering_TransactionReturned() {

            DateTime transactionDate = new DateTime(2024, 3, 19);

            var merchant = merchantsList.Single(m => m.Name == "Test Merchant 1");
            var contract = contractList.Single(c => c.operatorName == "Safaricom");
            var contractProducts = this.contractProducts.Where(cp => cp.Key == contract.contractId);
            var product = contractProducts.First().Value.Where(x => x.productName == "200 KES Topup").Single();

            List<Transaction> transactions = new List<Transaction>();
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 1, authCode: "AUTH231", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 2, authCode: "AUTH1232", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 3, authCode: "AUTH1233", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 4, authCode: "AUTH1234", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 5, authCode: "AUTH1235", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 6, authCode: "AUTH1236", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 7, authCode: "AUTH1237", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 8, authCode: "AUTH1228", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 9, authCode: "AUTH1229", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 10, authCode: "AUTH1123", transactionAmount: product.productValue));
            await this.helper.AddTransactionsX(transactions);

            DataTransferObjects.TransactionSearchRequest searchRequest = new() { QueryDate = transactionDate, TransactionNumber = "0004" };

            // No Paging
            var result = await ApiClient.TransactionSearch(string.Empty, Guid.NewGuid(), searchRequest, null, null, null, null, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            List<DataTransferObjects.TransactionResult> searchResult = result.Data;

            searchResult.Count.ShouldBe(1);
            searchResult.Any(s => s.TransactionReportingId == 4).ShouldBeTrue();
        }

        [Fact]
        public async Task FactTransactionsController_TransactionSearch_ResponseCodeFiltering_TransactionReturned() {

            DateTime transactionDate = new DateTime(2024, 3, 19);
            var merchant = merchantsList.Single(m => m.Name == "Test Merchant 1");
            var contract = contractList.Single(c => c.operatorName == "Safaricom");
            var contractProducts = this.contractProducts.Where(cp => cp.Key == contract.contractId);
            var product = contractProducts.First().Value.Where(x => x.productName == "200 KES Topup").Single();

            List<Transaction> transactions = new List<Transaction>();
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 1, authCode: "AUTH231", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0001", transactionReportingId: 2, authCode: "AUTH1232", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 3, authCode: "AUTH1233", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0001", transactionReportingId: 4, authCode: "AUTH1234", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 5, authCode: "AUTH1235", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0001", transactionReportingId: 6, authCode: "AUTH1236", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 7, authCode: "AUTH1237", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0001", transactionReportingId: 8, authCode: "AUTH1228", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 9, authCode: "AUTH1229", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0001", transactionReportingId: 10, authCode: "AUTH1123", transactionAmount: product.productValue));
            await this.helper.AddTransactionsX(transactions);

            DataTransferObjects.TransactionSearchRequest searchRequest = new() { QueryDate = transactionDate, ResponseCode = "0001" };

            // No Paging
            var result = await ApiClient.TransactionSearch(string.Empty, Guid.NewGuid(), searchRequest, null, null, null, null, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            List<DataTransferObjects.TransactionResult> searchResult = result.Data;
            searchResult.Count.ShouldBe(5);
            searchResult.Any(s => s.TransactionReportingId == 2).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 4).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 6).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 8).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 10).ShouldBeTrue();

            result = await ApiClient.TransactionSearch(string.Empty, Guid.NewGuid(), searchRequest, 1, 3, null, null, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            searchResult = result.Data;
            searchResult.Count.ShouldBe(3);
            searchResult.Any(s => s.TransactionReportingId == 2).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 4).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 6).ShouldBeTrue();

            result = await ApiClient.TransactionSearch(string.Empty, Guid.NewGuid(), searchRequest, 2, 3, null, null, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            searchResult = result.Data;
            searchResult.Count.ShouldBe(2);
            searchResult.Any(s => s.TransactionReportingId == 8).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 10).ShouldBeTrue();
        }

        [Fact]
        public async Task FactTransactionsController_TransactionSearch_MerchantFiltering_TransactionReturned() {

            DateTime transactionDate = new DateTime(2024, 3, 19);

            var merchant = merchantsList.Single(m => m.Name == "Test Merchant 1");
            var contract = contractList.Single(c => c.operatorName == "Safaricom");
            var contractProducts = this.contractProducts.Where(cp => cp.Key == contract.contractId);
            var product = contractProducts.First().Value.Where(x => x.productName == "200 KES Topup").Single();

            List<Transaction> transactions = new List<Transaction>();

            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 1, authCode: "AUTH231", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 2, authCode: "AUTH231", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 3, authCode: "AUTH231", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 4, authCode: "AUTH231", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 5, authCode: "AUTH231", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 6, authCode: "AUTH231", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 7, authCode: "AUTH231", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 8, authCode: "AUTH231", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 9, authCode: "AUTH231", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 10, authCode: "AUTH231", transactionAmount: product.productValue));

            merchant = merchantsList.Single(m => m.Name == "Test Merchant 2");
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 11, authCode: "AUTH231", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 12, authCode: "AUTH231", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 13, authCode: "AUTH231", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 14, authCode: "AUTH231", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 15, authCode: "AUTH231", transactionAmount: product.productValue));

            merchant = merchantsList.Single(m => m.Name == "Test Merchant 3");
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 16, authCode: "AUTH231", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 17, authCode: "AUTH231", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 18, authCode: "AUTH231", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 19, authCode: "AUTH231", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 20, authCode: "AUTH231", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 21, authCode: "AUTH231", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 22, authCode: "AUTH231", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 23, authCode: "AUTH231", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 24, authCode: "AUTH231", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 25, authCode: "AUTH231", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 26, authCode: "AUTH231", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 27, authCode: "AUTH231", transactionAmount: product.productValue));

            await this.helper.AddTransactionsX(transactions);

            DataTransferObjects.TransactionSearchRequest searchRequest = new() { QueryDate = transactionDate, Merchants = new List<int>() { 2, 3 } };

            // No Paging
            var result = await ApiClient.TransactionSearch(string.Empty, Guid.NewGuid(), searchRequest, null, null, null, null, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            List<DataTransferObjects.TransactionResult> searchResult = result.Data;

            searchResult.Count.ShouldBe(10);
            searchResult.Any(s => s.TransactionReportingId == 11).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 12).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 13).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 14).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 15).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 16).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 17).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 18).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 19).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 20).ShouldBeTrue();
            searchResult.Count(s => s.MerchantName == "Test Merchant 2").ShouldBe(5);
            searchResult.Count(s => s.MerchantName == "Test Merchant 3").ShouldBe(5);

            result = await ApiClient.TransactionSearch(string.Empty, Guid.NewGuid(), searchRequest, 1, 5, null, null, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            searchResult = result.Data;

            searchResult.Count.ShouldBe(5);
            searchResult.Any(s => s.TransactionReportingId == 11).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 12).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 13).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 14).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 15).ShouldBeTrue();
            searchResult.Count(s => s.MerchantName == "Test Merchant 2").ShouldBe(5);
            searchResult.Count(s => s.MerchantName == "Test Merchant 3").ShouldBe(0);

            result = await ApiClient.TransactionSearch(string.Empty, Guid.NewGuid(), searchRequest, 2, 5, null, null, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            searchResult = result.Data;

            searchResult.Count.ShouldBe(5);
            searchResult.Any(s => s.TransactionReportingId == 16).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 17).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 18).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 19).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 20).ShouldBeTrue();
            searchResult.Count(s => s.MerchantName == "Test Merchant 2").ShouldBe(0);
            searchResult.Count(s => s.MerchantName == "Test Merchant 3").ShouldBe(5);
        }

        [Fact]
        public async Task FactTransactionsController_TransactionSearch_OperatorFiltering_TransactionReturned() {

            DateTime transactionDate = new DateTime(2024, 3, 19);

            var merchant = merchantsList.Single(m => m.Name == "Test Merchant 1");
            var contract = contractList.Single(c => c.operatorName == "Safaricom");
            var contractProducts = this.contractProducts.Where(cp => cp.Key == contract.contractId);
            var product = contractProducts.First().Value.Where(x => x.productName == "200 KES Topup").Single();

            List<Transaction> transactions = new List<Transaction>();

            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 1, authCode: "AUTH231", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 4, authCode: "AUTH231", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 5, authCode: "AUTH231", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 7, authCode: "AUTH231", transactionAmount: product.productValue));

            contract = contractList.Single(c => c.operatorName == "Voucher");
            contractProducts = this.contractProducts.Where(cp => cp.Key == contract.contractId);
            product = contractProducts.First().Value.Where(x => x.productName == "10 KES Voucher").Single();

            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 2, authCode: "AUTH231", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 6, authCode: "AUTH231", transactionAmount: product.productValue));

            contract = contractList.Single(c => c.operatorName == "PataPawa PostPay");
            contractProducts = this.contractProducts.Where(cp => cp.Key == contract.contractId);
            product = contractProducts.First().Value.Where(x => x.productName == "Post Pay Bill Pay").Single();

            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 3, authCode: "AUTH231", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 8, authCode: "AUTH231", transactionAmount: product.productValue));

            contract = contractList.Single(c => c.operatorName == "PataPawa PrePay");
            contractProducts = this.contractProducts.Where(cp => cp.Key == contract.contractId);
            product = contractProducts.First().Value.Where(x => x.productName == "Pre Pay Bill Pay").Single();

            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 9, authCode: "AUTH231", transactionAmount: product.productValue));
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 10, authCode: "AUTH231", transactionAmount: product.productValue));

            await this.helper.AddTransactionsX(transactions);

            DataTransferObjects.TransactionSearchRequest searchRequest = new() { QueryDate = transactionDate, Operators = new List<int>() { 2, 4 } };

            // No Paging
            var result = await ApiClient.TransactionSearch(string.Empty, Guid.NewGuid(), searchRequest, null, null, null, null, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            List<DataTransferObjects.TransactionResult> searchResult = result.Data;
            searchResult.Count.ShouldBe(4);
            searchResult.Any(s => s.TransactionReportingId == 2).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 6).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 9).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 10).ShouldBeTrue();
            searchResult.Count(s => s.OperatorName == "Voucher").ShouldBe(2);
            searchResult.Count(s => s.OperatorName == "PataPawa PrePay").ShouldBe(2);

            result = await ApiClient.TransactionSearch(string.Empty, Guid.NewGuid(), searchRequest, 1, 2, null, null, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            searchResult = result.Data;
            searchResult.Count.ShouldBe(2);
            searchResult.Any(s => s.TransactionReportingId == 2).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 6).ShouldBeTrue();
            searchResult.Count(s => s.OperatorName == "Voucher").ShouldBe(2);
            searchResult.Count(s => s.OperatorName == "PataPawa PrePay").ShouldBe(0);

            result = await ApiClient.TransactionSearch(string.Empty, Guid.NewGuid(), searchRequest, 2, 2, null, null, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            searchResult = result.Data;
            searchResult.Count.ShouldBe(2);
            searchResult.Any(s => s.TransactionReportingId == 9).ShouldBeTrue();
            searchResult.Any(s => s.TransactionReportingId == 10).ShouldBeTrue();
            searchResult.Count(s => s.OperatorName == "Voucher").ShouldBe(0);
            searchResult.Count(s => s.OperatorName == "PataPawa PrePay").ShouldBe(2);
        }

        [Fact]
        public async Task FactTransactionsController_TransactionSearch_SortingTest_TransactionReturned() {

            DateTime transactionDate = new DateTime(2024, 3, 19);

            var merchant = merchantsList.Single(m => m.Name == "Test Merchant 1");
            var contract = contractList.Single(c => c.operatorName == "Safaricom");
            var contractProducts = this.contractProducts.Where(cp => cp.Key == contract.contractId);
            var product = contractProducts.First().Value.Where(x => x.productName == "Custom").Single();

            List<Transaction> transactions = new List<Transaction>();
            // Add some transactions
            //await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "Custom", "0000", transactionAmount: 100);
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 1, authCode: "AUTH231", transactionAmount: 100));

            merchant = merchantsList.Single(m => m.Name == "Test Merchant 2");
            contract = contractList.Single(c => c.operatorName == "Voucher");
            contractProducts = this.contractProducts.Where(cp => cp.Key == contract.contractId);
            product = contractProducts.First().Value.Where(x => x.productName == "Custom").Single();

            //await helper.AddTransaction(transactionDate, "Test Merchant 2", "Healthcare Centre 1 Contract", "Custom", "0000", transactionAmount: 200);
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 2, authCode: "AUTH231", transactionAmount: 200));

            merchant = merchantsList.Single(m => m.Name == "Test Merchant 3");
            contract = contractList.Single(c => c.operatorName == "PataPawa PostPay");
            contractProducts = this.contractProducts.Where(cp => cp.Key == contract.contractId);
            product = contractProducts.First().Value.Where(x => x.productName == "Post Pay Bill Pay").Single();

            //await helper.AddTransaction(transactionDate, "Test Merchant 3", "PataPawa PostPay Contract", "Post Pay Bill Pay", "0000", transactionAmount: 300);
            transactions.Add(await helper.BuildTransactionX(transactionDate, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, "0000", transactionReportingId: 3, authCode: "AUTH231", transactionAmount: 300));

            await this.helper.AddTransactionsX(transactions);

            DataTransferObjects.TransactionSearchRequest searchRequest = new() { QueryDate = transactionDate };
            // Default Sort
            var result = await ApiClient.TransactionSearch(string.Empty, Guid.NewGuid(), searchRequest, null, null, null, null, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            var searchResult = result.Data;
            searchResult.Count.ShouldBe(3);
            searchResult[0].TransactionAmount.ShouldBe(100);
            searchResult[1].TransactionAmount.ShouldBe(200);
            searchResult[2].TransactionAmount.ShouldBe(300);

            // Sort By merchant Name ascending
            result = await ApiClient.TransactionSearch(string.Empty, Guid.NewGuid(), searchRequest, null, null, DataTransferObjects.SortField.MerchantName, SortDirection.Ascending, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            searchResult = result.Data;
            searchResult.Count.ShouldBe(3);
            searchResult[0].MerchantName.ShouldBe("Test Merchant 1");
            searchResult[1].MerchantName.ShouldBe("Test Merchant 2");
            searchResult[2].MerchantName.ShouldBe("Test Merchant 3");

            // Sort By merchant Name descending
            result = await ApiClient.TransactionSearch(string.Empty, Guid.NewGuid(), searchRequest, null, null, DataTransferObjects.SortField.MerchantName, SortDirection.Descending, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            searchResult = result.Data;
            searchResult.Count.ShouldBe(3);
            searchResult[0].MerchantName.ShouldBe("Test Merchant 3");
            searchResult[1].MerchantName.ShouldBe("Test Merchant 2");
            searchResult[2].MerchantName.ShouldBe("Test Merchant 1");

            // Sort By operator Name ascending
            result = await ApiClient.TransactionSearch(string.Empty, Guid.NewGuid(), searchRequest, null, null, DataTransferObjects.SortField.OperatorName, SortDirection.Ascending, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            searchResult = result.Data;

            searchResult.Count.ShouldBe(3);
            searchResult[0].OperatorName.ShouldBe("PataPawa PostPay");
            searchResult[1].OperatorName.ShouldBe("Safaricom");
            searchResult[2].OperatorName.ShouldBe("Voucher");

            // Sort By operator Name descending
            result = await ApiClient.TransactionSearch(string.Empty, Guid.NewGuid(), searchRequest, null, null, DataTransferObjects.SortField.OperatorName, SortDirection.Descending, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            searchResult = result.Data;
            searchResult.Count.ShouldBe(3);
            searchResult[0].OperatorName.ShouldBe("Voucher");
            searchResult[1].OperatorName.ShouldBe("Safaricom");
            searchResult[2].OperatorName.ShouldBe("PataPawa PostPay");

            // Sort By transaction amount ascending
            result = await ApiClient.TransactionSearch(string.Empty, Guid.NewGuid(), searchRequest, null, null, DataTransferObjects.SortField.TransactionAmount, SortDirection.Ascending, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            searchResult = result.Data;
            searchResult.Count.ShouldBe(3);
            searchResult[0].TransactionAmount.ShouldBe(100);
            searchResult[1].TransactionAmount.ShouldBe(200);
            searchResult[2].TransactionAmount.ShouldBe(300);

            // Sort By transaction amount descending
            result = await ApiClient.TransactionSearch(string.Empty, Guid.NewGuid(), searchRequest, null, null, DataTransferObjects.SortField.TransactionAmount, SortDirection.Descending, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            searchResult = result.Data;
            searchResult.Count.ShouldBe(3);
            searchResult[0].TransactionAmount.ShouldBe(300);
            searchResult[1].TransactionAmount.ShouldBe(200);
            searchResult[2].TransactionAmount.ShouldBe(100);
        }
    }