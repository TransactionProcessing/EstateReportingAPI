using Ductus.FluentDocker.Common;
using Microsoft.EntityFrameworkCore;

namespace EstateReportingAPI.IntegrationTests;

using System.Collections.Generic;
using DataTrasferObjects;
using EstateManagement.Database.Contexts;
using EstateManagement.Database.Entities;
using EstateReportingAPI.DataTransferObjects;
using Microsoft.OpenApi.Services;
using Newtonsoft.Json;
using Shouldly;
using Xunit;
using Merchant = DataTrasferObjects.Merchant;
using SortDirection = DataTransferObjects.SortDirection;

public class FactTransactionsControllerTests : ControllerTestsBase
{
    protected Dictionary<String, List<String>> contractProducts;
    protected override async Task ClearStandingData()
    {
        await helper.DeleteAllContracts();
        await helper.DeleteAllMerchants();
    }

    protected override async Task SetupStandingData()
    {
        // Estates
        await helper.AddEstate("Test Estate", "Ref1");

        // Operators
        Int32 safaricomReportingId = await this.helper.AddOperator("Test Estate", "Safaricom");
        Int32 voucherReportingId = await this.helper.AddOperator("Test Estate", "Voucher");
        Int32 pataPawaPostPayReportingId = await this.helper.AddOperator("Test Estate", "PataPawa PostPay");
        Int32 pataPawaPrePay = await this.helper.AddOperator("Test Estate", "PataPawa PrePay");
        
        // Merchants
        await helper.AddMerchant("Test Estate", "Test Merchant 1", DateTime.MinValue);
        await helper.AddMerchant("Test Estate", "Test Merchant 2", DateTime.MinValue);
        await helper.AddMerchant("Test Estate", "Test Merchant 3", DateTime.MinValue);
        await helper.AddMerchant("Test Estate", "Test Merchant 4", DateTime.MinValue);

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

        // Response Codes
        await helper.AddResponseCode(0, "Success");
        await helper.AddResponseCode(1000, "Unknown Device");
        await helper.AddResponseCode(1001, "Unknown Estate");
        await helper.AddResponseCode(1002, "Unknown Merchant");
        await helper.AddResponseCode(1003, "No Devices Configured");

        merchantsList = context.Merchants.Select(m => m.Name).ToList();
        contractList = context.Contracts
                                 .Join(
                                       context.Operators,
                                       c => c.OperatorId,
                                       o => o.OperatorId,
                                       (c, o) => new { c.Description, OperatorName = o.Name }
                                      )
                                 .ToList().Select(x => (x.Description, x.OperatorName))
                                 .ToList();


        var query1 = context.Contracts
                         .GroupJoin(
                                    context.ContractProducts,
                                    c => c.ContractId,
                                    cp => cp.ContractId,
                                    (c, productGroup) => new
                                    {
                                        c.Description,
                                        Products = productGroup.Select(p => new { p.ContractProductReportingId, p.ProductName })
                                                                                    .OrderBy(p => p.ContractProductReportingId)
                                                                                    .Select(p => p.ProductName)
                                                                                    .ToList()
                                    })
                         .ToList();

        contractProducts = query1.ToDictionary(
                                                    item => item.Description,
                                                    item => item.Products
                                                   );
    }

    #region Todays Sales Tests

    [Fact]
    public async Task FactTransactionsControllerController_TodaysSales_SalesReturned()
    {
        List<Transaction>? todaysTransactions = new List<Transaction>();
        List<Transaction> comparisonDateTransactions = new List<Transaction>();

        DateTime todaysDateTime = DateTime.Now;
        DateTime comparisonDate = DateTime.Now.AddDays(-1).AddHours(-1);

        Dictionary<string, int> transactionCounts = new(){
                                                               { "Test Merchant 1", 15 },
                                                               { "Test Merchant 2", 18 },
                                                               { "Test Merchant 3", 9 },
                                                               { "Test Merchant 4", 0 }
                                                           };

        // Todays sales
        foreach (string merchantName in merchantsList)
        {
            foreach ((string contract, string operatorname) contract in contractList)
            {
                var productList = contractProducts.Single(cp => cp.Key == contract.contract).Value;
                foreach (string product in productList)
                {
                    var transactionCount = transactionCounts.Single(m => m.Key == merchantName).Value;
                    for (int i = 0; i < transactionCount; i++)
                    {
                        Transaction transaction = await helper.AddTransaction(todaysDateTime.AddHours(-1), merchantName, contract.contract, product, "0000");
                        todaysTransactions.Add(transaction);
                    }
                }
            }
        }

        // Comparison Date sales
        foreach (string merchantName in merchantsList)
        {
            foreach ((string contract, string operatorname) contract in contractList)
            {
                var productList = contractProducts.Single(cp => cp.Key == contract.contract).Value;
                foreach (string product in productList)
                {
                    var transactionCount = transactionCounts.Single(m => m.Key == merchantName).Value;
                    for (int i = 0; i < transactionCount; i++)
                    {
                        Transaction transaction = await helper.AddTransaction(comparisonDate, merchantName, contract.contract, product, "0000");
                        comparisonDateTransactions.Add(transaction);
                    }
                }
            }
        }

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
    public async Task FactTransactionsControllerController_TodaysSalesCountByHour_SalesReturned()
    {
        List<Transaction> todaysTransactions = new List<Transaction>();
        List<Transaction> comparisonDateTransactions = new List<Transaction>();

        Dictionary<string, int> transactionCounts = new(){
                                                               { "Test Merchant 1", 3 },
                                                               { "Test Merchant 2", 6 },
                                                               { "Test Merchant 3", 2 },
                                                               { "Test Merchant 4", 0 }
                                                           };

        // TODO: make counts dynamic
        DateTime todaysDateTime = DateTime.Now;

        for (int hour = 0; hour < 24; hour++)
        {
            List<Transaction> localList = new List<Transaction>();
            DateTime date = new DateTime(todaysDateTime.Year, todaysDateTime.Month, todaysDateTime.Day, hour, 0, 0);
            foreach (string merchantName in merchantsList)
            {
                foreach ((string contract, string operatorname) contract in contractList)
                {
                    var productList = contractProducts.Single(cp => cp.Key == contract.contract).Value;
                    foreach (string product in productList)
                    {
                        var transactionCount = transactionCounts.Single(m => m.Key == merchantName).Value;
                        for (int i = 0; i < transactionCount; i++)
                        {
                            Transaction transaction = await helper.AddTransaction(todaysDateTime.AddHours(-1), merchantName, contract.contract, product, "0000");
                            todaysTransactions.Add(transaction);
                        }
                    }
                }
            }

            todaysTransactions.AddRange(localList);
        }

        DateTime comparisonDate = todaysDateTime.AddDays(-1);
        for (int hour = 0; hour < 24; hour++)
        {
            List<Transaction> localList = new List<Transaction>();
            DateTime date = new DateTime(comparisonDate.Year, comparisonDate.Month, comparisonDate.Day, hour, 0, 0);
            foreach (string merchantName in merchantsList)
            {
                foreach (var contract in contractList)
                {
                    var productList = contractProducts.Single(cp => cp.Key == contract.contract).Value;
                    foreach (string product in productList)
                    {
                        var transactionCount = transactionCounts.Single(m => m.Key == merchantName).Value;
                        for (int i = 0; i < transactionCount; i++)
                        {
                            Transaction transaction = await helper.AddTransaction(comparisonDate, merchantName, contract.contract, product, "0000");
                            comparisonDateTransactions.Add(transaction);
                        }
                    }
                }
            }

            comparisonDateTransactions.AddRange(localList);
        }

        await helper.RunTodaysTransactionsSummaryProcessing(comparisonDate.Date);
        await helper.RunHistoricTransactionsSummaryProcessing(comparisonDate.Date);
        await helper.RunTodaysTransactionsSummaryProcessing(todaysDateTime.Date);

        var result = await ApiClient.GetTodaysSalesCountByHour(string.Empty, Guid.NewGuid(), 0, 0, comparisonDate, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        var todaysSalesCountByHour= result.Data;

        todaysSalesCountByHour.ShouldNotBeNull();
        foreach (TodaysSalesCountByHour salesCountByHour in todaysSalesCountByHour)
        {
            IEnumerable<Transaction> todayHour = todaysTransactions.Where(t => t.TransactionDateTime.Hour == salesCountByHour.Hour);
            IEnumerable<Transaction> comparisonHour = comparisonDateTransactions.Where(t => t.TransactionDateTime.Hour == salesCountByHour.Hour);
            salesCountByHour.ComparisonSalesCount.ShouldBe(comparisonHour.Count());
            salesCountByHour.TodaysSalesCount.ShouldBe(todayHour.Count());
        }
    }

    [Fact]
    public async Task FactTransactionsControllerController_TodaysSalesValueByHour_SalesReturned()
    {
        var todaysTransactions = new List<Transaction>();
        var comparisonDateTransactions = new List<Transaction>();

        Dictionary<string, int> transactionCounts = new(){
                                                               { "Test Merchant 1", 3 },
                                                               { "Test Merchant 2", 6 },
                                                               { "Test Merchant 3", 2 },
                                                               { "Test Merchant 4", 0 }
                                                           };

        DateTime todaysDateTime = DateTime.Now;

        for (int hour = 0; hour < 24; hour++)
        {
            List<Transaction> localList = new List<Transaction>();
            DateTime date = new DateTime(todaysDateTime.Year, todaysDateTime.Month, todaysDateTime.Day, hour, 0, 0);
            foreach (string merchantName in merchantsList)
            {
                foreach (var contract in contractList)
                {
                    var productList = contractProducts.Single(cp => cp.Key == contract.contract).Value;
                    foreach (string product in productList)
                    {
                        var transactionCount = transactionCounts.Single(m => m.Key == merchantName).Value;
                        for (int i = 0; i < transactionCount; i++)
                        {
                            Transaction transaction = await helper.AddTransaction(todaysDateTime.AddHours(-1), merchantName, contract.contract, product, "0000");
                            todaysTransactions.Add(transaction);
                        }
                    }
                }
            }

            todaysTransactions.AddRange(localList);
        }

        DateTime comparisonDate = todaysDateTime.AddDays(-1);
        for (int hour = 0; hour < 24; hour++)
        {
            List<Transaction> localList = new List<Transaction>();
            DateTime date = new DateTime(comparisonDate.Year, comparisonDate.Month, comparisonDate.Day, hour, 0, 0);
            foreach (string merchantName in merchantsList)
            {
                foreach (var contract in contractList)
                {
                    var productList = contractProducts.Single(cp => cp.Key == contract.contract).Value;
                    foreach (string product in productList)
                    {
                        var transactionCount = transactionCounts.Single(m => m.Key == merchantName).Value;
                        for (int i = 0; i < transactionCount; i++)
                        {
                            Transaction transaction = await helper.AddTransaction(comparisonDate, merchantName, contract.contract, product, "0000");
                            comparisonDateTransactions.Add(transaction);
                        }
                    }
                }
            }

            comparisonDateTransactions.AddRange(localList);
        }

        await helper.RunTodaysTransactionsSummaryProcessing(comparisonDate.Date);
        await helper.RunHistoricTransactionsSummaryProcessing(comparisonDate.Date);
        await helper.RunTodaysTransactionsSummaryProcessing(todaysDateTime.Date);

        var result = await ApiClient.GetTodaysSalesValueByHour(string.Empty, Guid.NewGuid(), 0, 0, comparisonDate, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        List<TodaysSalesValueByHour>? todaysSalesValueByHour = result.Data;
        foreach (TodaysSalesValueByHour salesValueByHour in todaysSalesValueByHour)
        {
            IEnumerable<Transaction> todayHour = todaysTransactions.Where(t => t.TransactionDateTime.Hour == salesValueByHour.Hour);
            IEnumerable<Transaction> comparisonHour = comparisonDateTransactions.Where(t => t.TransactionDateTime.Hour == salesValueByHour.Hour);
            salesValueByHour.ComparisonSalesValue.ShouldBe(comparisonHour.Sum(c => c.TransactionAmount));
            salesValueByHour.TodaysSalesValue.ShouldBe(todayHour.Sum(c => c.TransactionAmount));
        }
    }

    #endregion

    #region Todays Failed Sales Tests

    [Fact]
    public async Task FactTransactionsControllerController_TodaysFailedSales_SalesReturned()
    {
        EstateManagementGenericContext context = new EstateManagementSqlServerContext(GetLocalConnectionString($"EstateReportingReadModel{TestId.ToString()}"));
        var todaysTransactions = new List<Transaction>();
        var comparisonDateTransactions = new List<Transaction>();
        DatabaseHelper helper = new DatabaseHelper(context);
        // TODO: make counts dynamic
        DateTime todaysDateTime = DateTime.Now;

        Dictionary<string, int> transactionCounts = new(){
            { "Test Merchant 1", 3 },
            { "Test Merchant 2", 6 },
            { "Test Merchant 3", 2 },
            { "Test Merchant 4", 0 }
        };

        foreach (string merchantName in merchantsList)
        {
            foreach (var contract in contractList)
            {
                var productList = contractProducts.Single(cp => cp.Key == contract.contract).Value;
                foreach (string product in productList)
                {
                    var transactionCount = transactionCounts.Single(m => m.Key == merchantName).Value;
                    for (int i = 0; i < transactionCount; i++)
                    {
                        Transaction transaction = await helper.AddTransaction(todaysDateTime.AddHours(-1), merchantName, contract.contract, product, "1009");
                        todaysTransactions.Add(transaction);
                    }
                }
            }
        }

        DateTime comparisonDate = todaysDateTime.AddDays(-1);
        // Comparison Date sales
        foreach (string merchantName in merchantsList)
        {
            foreach (var contract in contractList)
            {
                var productList = contractProducts.Single(cp => cp.Key == contract.contract).Value;
                foreach (string product in productList)
                {
                    var transactionCount = transactionCounts.Single(m => m.Key == merchantName).Value;
                    for (int i = 0; i < transactionCount; i++)
                    {
                        Transaction transaction = await helper.AddTransaction(comparisonDate.AddHours(-1), merchantName, contract.contract, product, "1009");
                        comparisonDateTransactions.Add(transaction);
                    }
                }
            }
        }

        await helper.RunTodaysTransactionsSummaryProcessing(comparisonDate.Date);
        await helper.RunHistoricTransactionsSummaryProcessing(comparisonDate.Date);
        await helper.RunTodaysTransactionsSummaryProcessing(todaysDateTime.Date);

        var result = await ApiClient.GetTodaysFailedSales(string.Empty, Guid.NewGuid(), 1, 1, "1009", comparisonDate, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        TodaysSales? todaysSales = result.Data;

        todaysSales.ShouldNotBeNull();
        todaysSales.ComparisonSalesCount.ShouldBe(comparisonDateTransactions.Count);
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Sum(c => c.TransactionAmount));

        todaysSales.TodaysSalesCount.ShouldBe(todaysTransactions.Count);
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Sum(c => c.TransactionAmount));
    }

    #endregion

    [Fact]
    public async Task FactTransactionsController_GetTopBottomProductsByValue_BottomProducts_ProductsReturned()
    {
        DateTime todaysDateTime = DateTime.Now;

        string merchantName = merchantsList.First();
        (string contract, string operatorname) contract = contractList.Single(c => c.operatorname == "Safaricom");
        List<string> productList = contractProducts.Single(cp => cp.Key == contract.contract).Value;

        Dictionary<string, int> transactionCounts = new(){
                                                               { "200 KES Topup", 25 }, //5000
                                                               { "100 KES Topup", 15 }, // 1500 
                                                               { "50 KES Topup", 45 }, // 2250
                                                               { "Custom", 8 } // 600
                                                           };
        Dictionary<string, List<Transaction>> transactionsDictionary = new();
        foreach (string product in productList)
        {
            int transactionCount = transactionCounts.Single(m => m.Key == product).Value;
            for (int i = 0; i < transactionCount; i++)
            {
                Transaction transaction = await helper.AddTransaction(todaysDateTime.AddHours(-1), merchantName, contract.contract, product, "0000");
                if (transactionsDictionary.ContainsKey(product) == false)
                {
                    transactionsDictionary.Add(product, new List<Transaction>());
                }

                transactionsDictionary[product].Add(transaction);
            }
        }

        await helper.RunTodaysTransactionsSummaryProcessing(todaysDateTime.Date);

        var result = await ApiClient.GetTopBottomProductData(string.Empty, Guid.NewGuid(), TopBottom.Bottom, 3, CancellationToken.None);
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
    public async Task FactTransactionsController_GetTopBottomProductsByValue_TopProducts_ProductsReturned()
    {
        DateTime todaysDateTime = DateTime.Now;

        string merchantName = merchantsList.First();
        (string contract, string operatorname) contract = contractList.Single(c => c.operatorname == "Safaricom");
        List<string> productList = contractProducts.Single(cp => cp.Key == contract.contract).Value;

        Dictionary<string, int> transactionCounts = new(){
                                                               { "200 KES Topup", 25 }, //5000
                                                               { "100 KES Topup", 15 }, // 1500 
                                                               { "50 KES Topup", 45 }, // 2250
                                                               { "Custom", 8 } // 600
                                                           };
        Dictionary<string, List<Transaction>> transactionsDictionary = new();
        foreach (string product in productList)
        {
            int transactionCount = transactionCounts.Single(m => m.Key == product).Value;
            for (int i = 0; i < transactionCount; i++)
            {
                Transaction transaction = await helper.AddTransaction(todaysDateTime.AddHours(-1), merchantName, contract.contract, product, "0000");
                if (transactionsDictionary.ContainsKey(product) == false)
                {
                    transactionsDictionary.Add(product, new List<Transaction>());
                }

                transactionsDictionary[product].Add(transaction);
            }
        }

        await helper.RunTodaysTransactionsSummaryProcessing(todaysDateTime.Date);

        var result= await ApiClient.GetTopBottomProductData(string.Empty, Guid.NewGuid(), TopBottom.Top, 3, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        List<TopBottomProductData>? topBottomProductData = result.Data;
        topBottomProductData[0].ProductName.ShouldBe("200 KES Topup");
        topBottomProductData[0].SalesValue.ShouldBe(transactionsDictionary["200 KES Topup"].Sum(p => p.TransactionAmount));
        topBottomProductData[1].ProductName.ShouldBe("50 KES Topup");
        topBottomProductData[1].SalesValue.ShouldBe(transactionsDictionary["50 KES Topup"].Sum(p => p.TransactionAmount));
        topBottomProductData[2].ProductName.ShouldBe("100 KES Topup");
        topBottomProductData[2].SalesValue.ShouldBe(transactionsDictionary["100 KES Topup"].Sum(p => p.TransactionAmount));
    }

    [Fact]
    public async Task FactTransactionsController_GetTopBottomOperatorsByValue_BottomOperators_OperatorsReturned()
    {
        DateTime todaysDateTime = DateTime.Now;

        Dictionary<string, int> transactionCounts = new(){
                                                               { "Safaricom", 25 }, // 5000
                                                               { "Voucher", 15 }, // 150 
                                                               { "PataPawa PostPay", 45 }, // 3375
                                                               { "PataPawa PrePay", 8 } // 600
                                                           };

        Dictionary<string, List<Transaction>> transactionsDictionary = new();
        string merchantName = merchantsList.First();
        //List<String> productList = contractProducts.Single(cp => cp.Key == contractName).Value;
        foreach (KeyValuePair<string, int> transactionCount in transactionCounts)
        {
            var contract = contractList.Single(s => s.operatorname == transactionCount.Key);
            var products = contractProducts.Single(p => p.Key == contract.contract);
            var productname = products.Value.First();
            for (int i = 0; i < transactionCount.Value; i++)
            {
                Transaction transaction = await helper.AddTransaction(todaysDateTime.AddHours(-1), merchantName, contract.contract, productname, "0000");
                if (transactionsDictionary.ContainsKey(transactionCount.Key) == false)
                {
                    transactionsDictionary.Add(transactionCount.Key, new List<Transaction>());
                }

                transactionsDictionary[transactionCount.Key].Add(transaction);
            }
        }

        await helper.RunTodaysTransactionsSummaryProcessing(todaysDateTime.Date);

        var result= await ApiClient.GetTopBottomOperatorData(string.Empty, Guid.NewGuid(), TopBottom.Bottom, 3, CancellationToken.None);
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
    public async Task FactTransactionsController_GetTopBottomOperatorsByValue_TopOperators_OperatorsReturned()
    {
        DateTime todaysDateTime = DateTime.Now;

        Dictionary<string, int> transactionCounts = new(){
                                                               { "Safaricom", 25 }, // 5000
                                                               { "Voucher", 15 }, // 150 
                                                               { "PataPawa PostPay", 45 }, // 3375
                                                               { "PataPawa PrePay", 8 } // 600
                                                           };

        Dictionary<string, List<Transaction>> transactionsDictionary = new();
        string merchantName = merchantsList.First();
        foreach (KeyValuePair<string, int> transactionCount in transactionCounts)
        {
            var contract = contractList.Single(s => s.operatorname == transactionCount.Key);
            var products = contractProducts.Single(p => p.Key == contract.contract);
            var productname = products.Value.First();
            for (int i = 0; i < transactionCount.Value; i++)
            {
                Transaction transaction = await helper.AddTransaction(todaysDateTime.AddHours(-1), merchantName, contract.contract, productname, "0000");
                if (transactionsDictionary.ContainsKey(transactionCount.Key) == false)
                {
                    transactionsDictionary.Add(transactionCount.Key, new List<Transaction>());
                }

                transactionsDictionary[transactionCount.Key].Add(transaction);
            }
        }

        await helper.RunTodaysTransactionsSummaryProcessing(todaysDateTime.Date);

        var result = await ApiClient.GetTopBottomOperatorData(string.Empty, Guid.NewGuid(), TopBottom.Top, 3, CancellationToken.None);
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
    public async Task FactTransactionsController_GetTopBottomMerchantsByValue_BottomMerchants_MerchantsReturned()
    {
        DateTime todaysDateTime = DateTime.Now;

        Dictionary<string, int> transactionCounts = new(){
                                                               { "Test Merchant 1", 25 },
                                                               { "Test Merchant 2", 15 },
                                                               { "Test Merchant 3", 45 },
                                                               { "Test Merchant 4", 8 }
                                                           };

        Dictionary<string, List<Transaction>> transactionsDictionary = new();
        foreach (KeyValuePair<string, int> transactionCount in transactionCounts)
        {
            var contract = contractList.First();
            var products = contractProducts.Single(p => p.Key == contract.contract);
            var productname = products.Value.First();
            for (int i = 0; i < transactionCount.Value; i++)
            {
                Transaction transaction = await helper.AddTransaction(todaysDateTime.AddHours(-1), transactionCount.Key, contract.contract, productname, "0000");
                if (transactionsDictionary.ContainsKey(transactionCount.Key) == false)
                {
                    transactionsDictionary.Add(transactionCount.Key, new List<Transaction>());
                }

                transactionsDictionary[transactionCount.Key].Add(transaction);
            }
        }
        
        await helper.RunTodaysTransactionsSummaryProcessing(todaysDateTime.Date);

        var result = await ApiClient.GetTopBottomMerchantData(string.Empty, Guid.NewGuid(), TopBottom.Bottom, 3, CancellationToken.None);
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
    public async Task FactTransactionsController_GetTopBottomMerchantsByValue_TopMerchants_MerchantsReturned()
    {
        DateTime todaysDateTime = DateTime.Now;

        Dictionary<string, int> transactionCounts = new(){
                                                               { "Test Merchant 1", 25 },
                                                               { "Test Merchant 2", 15 },
                                                               { "Test Merchant 3", 45 },
                                                               { "Test Merchant 4", 8 }
                                                           };

        Dictionary<string, List<Transaction>> transactionsDictionary = new();
        foreach (KeyValuePair<string, int> transactionCount in transactionCounts)
        {
            var contract = contractList.First();
            var products = contractProducts.Single(p => p.Key == contract.contract);
            var productname = products.Value.First();
            for (int i = 0; i < transactionCount.Value; i++)
            {
                Transaction transaction = await helper.AddTransaction(todaysDateTime.AddHours(-1), transactionCount.Key, contract.contract, productname, "0000");
                if (transactionsDictionary.ContainsKey(transactionCount.Key) == false)
                {
                    transactionsDictionary.Add(transactionCount.Key, new List<Transaction>());
                }

                transactionsDictionary[transactionCount.Key].Add(transaction);
            }
        }

        await helper.RunTodaysTransactionsSummaryProcessing(todaysDateTime.Date);
        var result = await ApiClient.GetTopBottomMerchantData(string.Empty, Guid.NewGuid(), TopBottom.Top, 3, CancellationToken.None);
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
    public async Task FactTransactionsControllerController_MerchantPerformance_AllMerchants_SalesReturned()
    {

        var todaysTransactions = new List<Transaction>();
        var comparisonDateTransactions = new List<Transaction>();

        DateTime todaysDateTime = DateTime.Now;
        DateTime comparisonDate = DateTime.Now.AddDays(-1).AddHours(-1);

        Dictionary<string, int> transactionCounts = new(){
                                                               { "Test Merchant 1", 15 },
                                                               { "Test Merchant 2", 18 },
                                                               { "Test Merchant 3", 9 },
                                                               { "Test Merchant 4", 3 },
                                                           };

        // Todays sales
        foreach (string merchantName in merchantsList)
        {
            foreach ((string contract, string operatorname) contract in contractList)
            {
                var productList = contractProducts.Single(cp => cp.Key == contract.contract).Value;
                foreach (string product in productList)
                {
                    var transactionCount = transactionCounts.Single(m => m.Key == merchantName).Value;
                    for (int i = 0; i < transactionCount; i++)
                    {
                        Transaction transaction = await helper.AddTransaction(todaysDateTime.AddHours(-1), merchantName, contract.contract, product, "0000");
                        todaysTransactions.Add(transaction);
                    }
                }
            }
        }

        // Comparison Date sales
        foreach (string merchantName in merchantsList)
        {
            foreach ((string contract, string operatorname) contract in contractList)
            {
                var productList = contractProducts.Single(cp => cp.Key == contract.contract).Value;
                foreach (string product in productList)
                {
                    var transactionCount = transactionCounts.Single(m => m.Key == merchantName).Value;
                    for (int i = 0; i < transactionCount; i++)
                    {
                        Transaction transaction = await helper.AddTransaction(comparisonDate, merchantName, contract.contract, product, "0000");
                        comparisonDateTransactions.Add(transaction);
                    }
                }
            }
        }

        await helper.RunTodaysTransactionsSummaryProcessing(comparisonDate.Date);
        await helper.RunHistoricTransactionsSummaryProcessing(comparisonDate.Date);
        await helper.RunTodaysTransactionsSummaryProcessing(todaysDateTime.Date);

        var result= await ApiClient.GetMerchantPerformance(string.Empty, Guid.NewGuid(), comparisonDate, new List<Int32>(), CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        TodaysSales? todaysSales = result.Data;
        todaysSales.ShouldNotBeNull();
        todaysSales.ComparisonSalesCount.ShouldBe(comparisonDateTransactions.Count);
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Sum(c => c.TransactionAmount));

        todaysSales.TodaysSalesCount.ShouldBe(todaysTransactions.Count);
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Sum(c => c.TransactionAmount));
    }

    [Fact]
    public async Task FactTransactionsControllerController_MerchantPerformance_SingleMerchant_SalesReturned()
    {

        var todaysTransactions = new List<Transaction>();
        var comparisonDateTransactions = new List<Transaction>();

        DateTime todaysDateTime = DateTime.Now;
        DateTime comparisonDate = DateTime.Now.AddDays(-1).AddHours(-1);

        Dictionary<string, int> transactionCounts = new(){
                                                               { "Test Merchant 1", 15 },
                                                               { "Test Merchant 2", 18 },
                                                               { "Test Merchant 3", 9 },
                                                               { "Test Merchant 4", 3 },
                                                           };

        // Todays sales
        foreach (string merchantName in merchantsList)
        {
            foreach ((string contract, string operatorname) contract in contractList)
            {
                var productList = contractProducts.Single(cp => cp.Key == contract.contract).Value;
                foreach (string product in productList)
                {
                    var transactionCount = transactionCounts.Single(m => m.Key == merchantName).Value;
                    for (int i = 0; i < transactionCount; i++)
                    {
                        Transaction transaction = await helper.AddTransaction(todaysDateTime.AddHours(-1), merchantName, contract.contract, product, "0000");
                        todaysTransactions.Add(transaction);
                    }
                }
            }
        }

        // Comparison Date sales
        foreach (string merchantName in merchantsList)
        {
            foreach ((string contract, string operatorname) contract in contractList)
            {
                var productList = contractProducts.Single(cp => cp.Key == contract.contract).Value;
                foreach (string product in productList)
                {
                    var transactionCount = transactionCounts.Single(m => m.Key == merchantName).Value;
                    for (int i = 0; i < transactionCount; i++)
                    {
                        Transaction transaction = await helper.AddTransaction(comparisonDate, merchantName, contract.contract, product, "0000");
                        comparisonDateTransactions.Add(transaction);
                    }
                }
            }
        }

        List<int> merchantFilterList = new List<int>{
                                                            2
                                                        };

        var merchantIdsForVerify = await this.context.Merchants.Where(m => m.MerchantReportingId == 2).Select(m => m.MerchantId)
            .ToListAsync(CancellationToken.None);

        string serializedArray = string.Join(",", merchantFilterList);

        await helper.RunTodaysTransactionsSummaryProcessing(comparisonDate.Date);
        await helper.RunHistoricTransactionsSummaryProcessing(comparisonDate.Date);
        await helper.RunTodaysTransactionsSummaryProcessing(todaysDateTime.Date);

        var result = await ApiClient.GetMerchantPerformance(string.Empty, Guid.NewGuid(), comparisonDate, merchantFilterList, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        TodaysSales? todaysSales = result.Data;
        todaysSales.ShouldNotBeNull();
        todaysSales.ComparisonSalesCount.ShouldBe(comparisonDateTransactions.Count(c => merchantIdsForVerify.Contains(c.MerchantId)));
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(c => merchantIdsForVerify.Contains(c.MerchantId)).Sum(c => c.TransactionAmount));

        todaysSales.TodaysSalesCount.ShouldBe(todaysTransactions.Count(c => merchantIdsForVerify.Contains(c.MerchantId)));
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(c => merchantIdsForVerify.Contains(c.MerchantId)).Sum(c => c.TransactionAmount));
    }

    [Fact]
    public async Task FactTransactionsControllerController_ProductPerformance_AllProducts_SalesReturned()
    {
        var todaysTransactions = new List<Transaction>();
        var comparisonDateTransactions = new List<Transaction>();

        DateTime todaysDateTime = DateTime.Now;
        DateTime comparisonDate = DateTime.Now.AddDays(-1).AddHours(-1);

        string merchantName = merchantsList.First();
        (string contract, string operatorname) contract = contractList.Single(c => c.operatorname == "Safaricom");
        List<string> productList = contractProducts.Single(cp => cp.Key == contract.contract).Value;

        Dictionary<string, int> transactionCounts = new(){
                                                               { "200 KES Topup", 25 }, //5000
                                                               { "100 KES Topup", 15 }, // 1500 
                                                               { "50 KES Topup", 45 }, // 2250
                                                               { "Custom", 8 } // 600
                                                           };
        foreach (string product in productList)
        {
            int transactionCount = transactionCounts.Single(m => m.Key == product).Value;
            for (int i = 0; i < transactionCount; i++)
            {
                Transaction transaction = await helper.AddTransaction(todaysDateTime.AddHours(-1), merchantName, contract.contract, product, "0000");
                todaysTransactions.Add(transaction);
            }
        }

        foreach (string product in productList)
        {
            int transactionCount = transactionCounts.Single(m => m.Key == product).Value;
            for (int i = 0; i < transactionCount; i++)
            {
                Transaction transaction = await helper.AddTransaction(comparisonDate, merchantName, contract.contract, product, "0000");
                comparisonDateTransactions.Add(transaction);
            }
        }

        await helper.RunTodaysTransactionsSummaryProcessing(comparisonDate.Date);
        await helper.RunHistoricTransactionsSummaryProcessing(comparisonDate.Date);
        await helper.RunTodaysTransactionsSummaryProcessing(todaysDateTime.Date);

        var result = await ApiClient.GetProductPerformance(string.Empty, Guid.NewGuid(), comparisonDate, new List<Int32>(), CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        TodaysSales? todaysSales = result.Data;
        todaysSales.ShouldNotBeNull();
        todaysSales.ComparisonSalesCount.ShouldBe(comparisonDateTransactions.Count);
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Sum(c => c.TransactionAmount));

        todaysSales.TodaysSalesCount.ShouldBe(todaysTransactions.Count);
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Sum(c => c.TransactionAmount));
    }

    [Fact]
    public async Task FactTransactionsControllerController_ProductPerformance_SingleProduct_SalesReturned()
    {
        var todaysTransactions = new List<Transaction>();
        var comparisonDateTransactions = new List<Transaction>();

        DateTime todaysDateTime = DateTime.Now;
        DateTime comparisonDate = DateTime.Now.AddDays(-1).AddHours(-1);

        string merchantName = merchantsList.First();
        (string contract, string operatorname) contract = contractList.Single(c => c.operatorname == "Safaricom");
        List<string> productList = contractProducts.Single(cp => cp.Key == contract.contract).Value;

        Dictionary<string, int> transactionCounts = new(){
                                                               { "200 KES Topup", 25 }, //5000
                                                               { "100 KES Topup", 15 }, // 1500 
                                                               { "50 KES Topup", 45 }, // 2250
                                                               { "Custom", 8 } // 600
                                                           };
        foreach (string product in productList)
        {
            int transactionCount = transactionCounts.Single(m => m.Key == product).Value;
            for (int i = 0; i < transactionCount; i++)
            {
                Transaction transaction = await helper.AddTransaction(todaysDateTime.AddHours(-1), merchantName, contract.contract, product, "0000");
                todaysTransactions.Add(transaction);
            }
        }

        foreach (string product in productList)
        {
            int transactionCount = transactionCounts.Single(m => m.Key == product).Value;
            for (int i = 0; i < transactionCount; i++)
            {
                Transaction transaction = await helper.AddTransaction(comparisonDate, merchantName, contract.contract, product, "0000");
                comparisonDateTransactions.Add(transaction);
            }
        }

        List<int> productFilterList = new List<int>
        {
            2
        };

        var productIdsForVerify = await context.ContractProducts.Where(cp => cp.ContractProductReportingId == 2)
            .Select(cp => cp.ContractProductId).ToListAsync(CancellationToken.None);

        await helper.RunTodaysTransactionsSummaryProcessing(comparisonDate.Date);
        await helper.RunHistoricTransactionsSummaryProcessing(comparisonDate.Date);
        await helper.RunTodaysTransactionsSummaryProcessing(todaysDateTime.Date);

        string serializedArray = string.Join(",", productFilterList);
        
        var result = await ApiClient.GetProductPerformance(string.Empty, Guid.NewGuid(), comparisonDate, productFilterList, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        TodaysSales? todaysSales = result.Data;
        todaysSales.ShouldNotBeNull();
        todaysSales.ComparisonSalesCount.ShouldBe(comparisonDateTransactions.Count(c => productIdsForVerify.Contains(c.ContractProductId)));
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(c => productIdsForVerify.Contains(c.ContractProductId)).Sum(c => c.TransactionAmount));

        todaysSales.TodaysSalesCount.ShouldBe(todaysTransactions.Count(c => productIdsForVerify.Contains(c.ContractProductId)));
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(c => productIdsForVerify.Contains(c.ContractProductId)).Sum(c => c.TransactionAmount));
    }

    [Fact]
    public async Task FactTransactionsControllerController_ProductPerformance_MultipleProducts_SalesReturned()
    {
        var todaysTransactions = new List<Transaction>();
        var comparisonDateTransactions = new List<Transaction>();

        DateTime todaysDateTime = DateTime.Now;
        DateTime comparisonDate = DateTime.Now.AddDays(-1).AddHours(-1);

        string merchantName = merchantsList.First();
        (string contract, string operatorname) contract = contractList.Single(c => c.operatorname == "Safaricom");
        List<string> productList = contractProducts.Single(cp => cp.Key == contract.contract).Value;

        Dictionary<string, int> transactionCounts = new(){
                                                               { "200 KES Topup", 25 }, //5000
                                                               { "100 KES Topup", 15 }, // 1500 
                                                               { "50 KES Topup", 45 }, // 2250
                                                               { "Custom", 8 } // 600
                                                           };
        foreach (string product in productList)
        {
            int transactionCount = transactionCounts.Single(m => m.Key == product).Value;
            for (int i = 0; i < transactionCount; i++)
            {
                Transaction transaction = await helper.AddTransaction(todaysDateTime.AddHours(-1), merchantName, contract.contract, product, "0000");
                todaysTransactions.Add(transaction);
            }
        }

        foreach (string product in productList)
        {
            int transactionCount = transactionCounts.Single(m => m.Key == product).Value;
            for (int i = 0; i < transactionCount; i++)
            {
                Transaction transaction = await helper.AddTransaction(comparisonDate, merchantName, contract.contract, product, "0000");
                comparisonDateTransactions.Add(transaction);
            }
        }

        List<int> productFilterList = new List<int>{
                                                           2,
                                                           3
                                                       };
        var productIdsForVerify = await context.ContractProducts.Where(cp => cp.ContractProductReportingId >= 2 && cp.ContractProductReportingId <= 3)
            .Select(cp => cp.ContractProductId).ToListAsync(CancellationToken.None);

        string serializedArray = string.Join(",", productFilterList);

        await helper.RunTodaysTransactionsSummaryProcessing(comparisonDate.Date);
        await helper.RunHistoricTransactionsSummaryProcessing(comparisonDate.Date);
        await helper.RunTodaysTransactionsSummaryProcessing(todaysDateTime.Date);
        
        var result = await ApiClient.GetProductPerformance(string.Empty, Guid.NewGuid(), comparisonDate, productFilterList, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        TodaysSales? todaysSales = result.Data;
        todaysSales.ShouldNotBeNull();
        todaysSales.ComparisonSalesCount.ShouldBe(comparisonDateTransactions.Count(c => productIdsForVerify.Contains(c.ContractProductId)));
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(c => productIdsForVerify.Contains(c.ContractProductId)).Sum(c => c.TransactionAmount));

        todaysSales.TodaysSalesCount.ShouldBe(todaysTransactions.Count(c => productIdsForVerify.Contains(c.ContractProductId)));
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(c => productIdsForVerify.Contains(c.ContractProductId)).Sum(c => c.TransactionAmount));
    }

    [Fact]
    public async Task FactTransactionsControllerController_OperatorPerformance_SingleOperator_SalesReturned()
    {
        var todaysTransactions = new List<Transaction>();
        var comparisonDateTransactions = new List<Transaction>();

        DateTime todaysDateTime = DateTime.Now;
        DateTime comparisonDate = DateTime.Now.AddDays(-1).AddHours(-1);

        Dictionary<string, int> transactionCounts = new(){
                                                               { "Safaricom", 25 }, // 5000
                                                               { "Voucher", 15 }, // 150 
                                                               { "PataPawa PostPay", 45 }, // 3375
                                                               { "PataPawa PrePay", 8 } // 600
                                                           };

        string merchantName = merchantsList.First();
        foreach (KeyValuePair<string, int> transactionCount in transactionCounts)
        {
            var contract = contractList.Single(s => s.operatorname == transactionCount.Key);
            var products = contractProducts.Single(p => p.Key == contract.contract);
            var productname = products.Value.First();
            for (int i = 0; i < transactionCount.Value; i++)
            {
                Transaction transaction = await helper.AddTransaction(todaysDateTime.AddHours(-1), merchantName, contract.contract, productname, "0000");
                todaysTransactions.Add(transaction);
            }
        }

        foreach (KeyValuePair<string, int> transactionCount in transactionCounts)
        {
            var contract = contractList.Single(s => s.operatorname == transactionCount.Key);
            var products = contractProducts.Single(p => p.Key == contract.contract);
            var productname = products.Value.First();
            for (int i = 0; i < transactionCount.Value; i++)
            {
                Transaction transaction = await helper.AddTransaction(comparisonDate.AddHours(-1), merchantName, contract.contract, productname, "0000");
                comparisonDateTransactions.Add(transaction);
            }
        }

        List<int> operatorFilterList = new List<int>{
                                                            2
                                                        };
        var operatorIdsForVerify = await context.Operators.Where(cp => cp.OperatorReportingId == 2)
            .Select(cp => cp.OperatorId).ToListAsync(CancellationToken.None);


        string serializedArray = string.Join(",", operatorFilterList);

        await helper.RunTodaysTransactionsSummaryProcessing(comparisonDate.Date);
        await helper.RunHistoricTransactionsSummaryProcessing(comparisonDate.Date);
        await helper.RunTodaysTransactionsSummaryProcessing(todaysDateTime.Date);

        var result = await ApiClient.GetOperatorPerformance(string.Empty, Guid.NewGuid(), comparisonDate, operatorFilterList, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        TodaysSales? todaysSales = result.Data;
        todaysSales.ShouldNotBeNull();
        todaysSales.ComparisonSalesCount.ShouldBe(comparisonDateTransactions.Count(c => operatorIdsForVerify.Contains(c.OperatorId)));
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(c => operatorIdsForVerify.Contains(c.OperatorId)).Sum(c => c.TransactionAmount));

        todaysSales.TodaysSalesCount.ShouldBe(todaysTransactions.Count(c => operatorIdsForVerify.Contains(c.OperatorId)));
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(c => operatorIdsForVerify.Contains(c.OperatorId)).Sum(c => c.TransactionAmount));
    }

    [Fact]
    public async Task FactTransactionsControllerController_OperatorPerformance_MultipleOperators_SalesReturned()
    {
        var todaysTransactions = new List<Transaction>();
        var comparisonDateTransactions = new List<Transaction>();

        DateTime todaysDateTime = DateTime.Now;
        DateTime comparisonDate = DateTime.Now.AddDays(-1).AddHours(-1);

        Dictionary<string, int> transactionCounts = new(){
                                                               { "Safaricom", 25 }, // 5000
                                                               { "Voucher", 15 }, // 150 
                                                               { "PataPawa PostPay", 45 }, // 3375
                                                               { "PataPawa PrePay", 8 } // 600
                                                           };

        string merchantName = merchantsList.First();
        foreach (KeyValuePair<string, int> transactionCount in transactionCounts)
        {
            var contract = contractList.Single(s => s.operatorname == transactionCount.Key);
            var products = contractProducts.Single(p => p.Key == contract.contract);
            var productname = products.Value.First();
            for (int i = 0; i < transactionCount.Value; i++)
            {
                Transaction transaction = await helper.AddTransaction(todaysDateTime.AddHours(-1), merchantName, contract.contract, productname, "0000");
                todaysTransactions.Add(transaction);
            }
        }

        foreach (KeyValuePair<string, int> transactionCount in transactionCounts)
        {
            var contract = contractList.Single(s => s.operatorname == transactionCount.Key);
            var products = contractProducts.Single(p => p.Key == contract.contract);
            var productname = products.Value.First();
            for (int i = 0; i < transactionCount.Value; i++)
            {
                Transaction transaction = await helper.AddTransaction(comparisonDate.AddHours(-1), merchantName, contract.contract, productname, "0000");
                comparisonDateTransactions.Add(transaction);
            }
        }

        List<int> operatorFilterList = new List<int>{
                                                            2,3
                                                        };

        var operatorIdsForVerify = await context.Operators.Where(cp => cp.OperatorReportingId >= 2 && cp.OperatorReportingId <= 3)
            .Select(cp => cp.OperatorId).ToListAsync(CancellationToken.None);

        string serializedArray = string.Join(",", operatorFilterList);

        await helper.RunTodaysTransactionsSummaryProcessing(comparisonDate.Date);
        await helper.RunHistoricTransactionsSummaryProcessing(comparisonDate.Date);
        await helper.RunTodaysTransactionsSummaryProcessing(todaysDateTime.Date);

        var result = await ApiClient.GetOperatorPerformance(string.Empty, Guid.NewGuid(), comparisonDate, operatorFilterList, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        TodaysSales? todaysSales = result.Data;
        todaysSales.ShouldNotBeNull();
        todaysSales.ComparisonSalesCount.ShouldBe(comparisonDateTransactions.Count(c => operatorIdsForVerify.Contains(c.OperatorId)));
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(c => operatorIdsForVerify.Contains(c.OperatorId)).Sum(c => c.TransactionAmount));

        todaysSales.TodaysSalesCount.ShouldBe(todaysTransactions.Count(c => operatorIdsForVerify.Contains(c.OperatorId)));
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(c => operatorIdsForVerify.Contains(c.OperatorId)).Sum(c => c.TransactionAmount));
    }

    [Fact]
    public async Task FactTransactionsControllerController_GetMerchantsTransactionKpis_SalesReturned()
    {
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
        MerchantKpi? merchantKpi = result.Data;
        merchantKpi.ShouldNotBeNull();
        merchantKpi.MerchantsWithSaleInLastHour.ShouldBe(4);
        merchantKpi.MerchantsWithNoSaleToday.ShouldBe(6);
        merchantKpi.MerchantsWithNoSaleInLast7Days.ShouldBe(8);
    }

    [Fact]
    public async Task FactTransactionsController_TransactionSearch_NoAdditionalFiltering_TransactionReturned()
    {

        DateTime transactionDate = new DateTime(2024, 3, 19);

        // Add some transactions
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 1);
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "100 KES Topup", "0000", transactionReportingId: 2);
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 3);
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "100 KES Topup", "0000", transactionReportingId: 4);
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 5);
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "100 KES Topup", "0000", transactionReportingId: 6);
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 7);
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "100 KES Topup", "0000", transactionReportingId: 8);
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 9);
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "100 KES Topup", "0000", transactionReportingId: 10);

        TransactionSearchRequest searchRequest = new TransactionSearchRequest
        {
            QueryDate = transactionDate
        };

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
        searchResult.Any(s => s.TransactionReportingId == 2).ShouldBeTrue();
        searchResult.Any(s => s.TransactionReportingId == 4).ShouldBeTrue();
        searchResult.Any(s => s.TransactionReportingId == 6).ShouldBeTrue();
        searchResult.Any(s => s.TransactionReportingId == 8).ShouldBeTrue();
        searchResult.Any(s => s.TransactionReportingId == 10).ShouldBeTrue();

        result = await ApiClient.TransactionSearch(string.Empty, Guid.NewGuid(), searchRequest, 2, 5, null, null, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        searchResult = result.Data;
        searchResult.Count.ShouldBe(5);
        searchResult.Any(s => s.TransactionReportingId == 1).ShouldBeTrue();
        searchResult.Any(s => s.TransactionReportingId == 3).ShouldBeTrue();
        searchResult.Any(s => s.TransactionReportingId == 5).ShouldBeTrue();
        searchResult.Any(s => s.TransactionReportingId == 7).ShouldBeTrue();
        searchResult.Any(s => s.TransactionReportingId == 9).ShouldBeTrue();
    }

    [Fact]
    public async Task FactTransactionsController_TransactionSearch_ValueRangeFiltering_TransactionReturned()
    {

        DateTime transactionDate = new DateTime(2024, 3, 19);

        // Add some transactions
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "Custom", "0000", 50, 1);
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "Custom", "0000", 123, 2);
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "Custom", "0000", 100, 3);
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "Custom", "0000", 101, 4);
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "Custom", "0000", 199, 5);
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "Custom", "0000", 150, 6);
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "Custom", "0000", 200, 7);
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "Custom", "0000", 201, 8);
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "Custom", "0000", 99, 9);
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "Custom", "0000", 111, 10);

        TransactionSearchRequest searchRequest = new TransactionSearchRequest
        {
            QueryDate = transactionDate,
            ValueRange = new ValueRange
            {
                StartValue = 100,
                EndValue = 200
            }
        };

        // No Paging
        var result = await ApiClient.TransactionSearch(string.Empty, Guid.NewGuid(), searchRequest, null, null, null, null, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        List<TransactionResult> searchResult = result.Data;
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
    public async Task FactTransactionsController_TransactionSearch_AuthCodeFiltering_TransactionReturned()
    {

        DateTime transactionDate = new DateTime(2024, 3, 19);

        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 1, authCode: "AUTH231");
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 2, authCode: "AUTH1232");
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 3, authCode: "AUTH1233");
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 4, authCode: "AUTH1234");
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 5, authCode: "AUTH1235");
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 6, authCode: "AUTH1236");
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 7, authCode: "AUTH1237");
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 8, authCode: "AUTH1228");
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 9, authCode: "AUTH1229");
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 10, authCode: "AUTH1123");

        TransactionSearchRequest searchRequest = new TransactionSearchRequest
        {
            QueryDate = transactionDate,
            AuthCode = "AUTH1235"
        };

        // No Paging
        var result = await ApiClient.TransactionSearch(string.Empty, Guid.NewGuid(), searchRequest, null, null, null, null, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        List<TransactionResult> searchResult = result.Data;
        searchResult.Count.ShouldBe(1);
        searchResult.Any(s => s.TransactionReportingId == 5).ShouldBeTrue();
    }

    [Fact]
    public async Task FactTransactionsController_TransactionSearch_TransactionNumberFiltering_TransactionReturned()
    {

        DateTime transactionDate = new DateTime(2024, 3, 19);

        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 1, authCode: "AUTH231");
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 2, authCode: "AUTH1232");
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 3, authCode: "AUTH1233");
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 4, authCode: "AUTH1234");
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 5, authCode: "AUTH1235");
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 6, authCode: "AUTH1236");
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 7, authCode: "AUTH1237");
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 8, authCode: "AUTH1228");
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 9, authCode: "AUTH1229");
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 10, authCode: "AUTH1123");

        TransactionSearchRequest searchRequest = new TransactionSearchRequest
        {
            QueryDate = transactionDate,
            TransactionNumber = "0004"
        };

        // No Paging
        var result = await ApiClient.TransactionSearch(string.Empty, Guid.NewGuid(), searchRequest, null, null, null, null, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        List<TransactionResult> searchResult = result.Data;

        searchResult.Count.ShouldBe(1);
        searchResult.Any(s => s.TransactionReportingId == 4).ShouldBeTrue();
    }

    [Fact]
    public async Task FactTransactionsController_TransactionSearch_ResponseCodeFiltering_TransactionReturned()
    {

        DateTime transactionDate = new DateTime(2024, 3, 19);

        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 1, authCode: "AUTH231");
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0001", transactionReportingId: 2, authCode: "AUTH1232");
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 3, authCode: "AUTH1233");
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0001", transactionReportingId: 4, authCode: "AUTH1234");
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 5, authCode: "AUTH1235");
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0001", transactionReportingId: 6, authCode: "AUTH1236");
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 7, authCode: "AUTH1237");
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0001", transactionReportingId: 8, authCode: "AUTH1228");
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 9, authCode: "AUTH1229");
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0001", transactionReportingId: 10, authCode: "AUTH1123");

        TransactionSearchRequest searchRequest = new TransactionSearchRequest
        {
            QueryDate = transactionDate,
            ResponseCode = "0001"
        };

        // No Paging
        var result = await ApiClient.TransactionSearch(string.Empty, Guid.NewGuid(), searchRequest, null, null, null, null, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        List<TransactionResult> searchResult = result.Data;
        searchResult.Count.ShouldBe(5);
        searchResult.Any(s => s.TransactionReportingId == 2).ShouldBeTrue();
        searchResult.Any(s => s.TransactionReportingId == 4).ShouldBeTrue();
        searchResult.Any(s => s.TransactionReportingId == 6).ShouldBeTrue();
        searchResult.Any(s => s.TransactionReportingId == 8).ShouldBeTrue();
        searchResult.Any(s => s.TransactionReportingId == 10).ShouldBeTrue();
        
        result= await ApiClient.TransactionSearch(string.Empty, Guid.NewGuid(), searchRequest, 1, 3, null, null, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        searchResult = result.Data;
        searchResult.Count.ShouldBe(3);
        searchResult.Any(s => s.TransactionReportingId == 2).ShouldBeTrue();
        searchResult.Any(s => s.TransactionReportingId == 4).ShouldBeTrue();
        searchResult.Any(s => s.TransactionReportingId == 6).ShouldBeTrue();

        result= await ApiClient.TransactionSearch(string.Empty, Guid.NewGuid(), searchRequest, 2, 3, null, null, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        searchResult = result.Data;
        searchResult.Count.ShouldBe(2);
        searchResult.Any(s => s.TransactionReportingId == 8).ShouldBeTrue();
        searchResult.Any(s => s.TransactionReportingId == 10).ShouldBeTrue();
    }

    [Fact]
    public async Task FactTransactionsController_TransactionSearch_MerchantFiltering_TransactionReturned()
    {

        DateTime transactionDate = new DateTime(2024, 3, 19);

        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 1, authCode: "AUTH231");
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 2, authCode: "AUTH1232");
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 3, authCode: "AUTH1233");
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 4, authCode: "AUTH1234");
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 5, authCode: "AUTH1235");
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 6, authCode: "AUTH1236");
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 7, authCode: "AUTH1237");
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 8, authCode: "AUTH1228");
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 9, authCode: "AUTH1229");
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 10, authCode: "AUTH1123");

        await helper.AddTransaction(transactionDate, "Test Merchant 2", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 11, authCode: "AUTH231");
        await helper.AddTransaction(transactionDate, "Test Merchant 2", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 12, authCode: "AUTH1232");
        await helper.AddTransaction(transactionDate, "Test Merchant 2", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 13, authCode: "AUTH1233");
        await helper.AddTransaction(transactionDate, "Test Merchant 2", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 14, authCode: "AUTH1234");
        await helper.AddTransaction(transactionDate, "Test Merchant 2", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 15, authCode: "AUTH1235");
        await helper.AddTransaction(transactionDate, "Test Merchant 2", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 16, authCode: "AUTH1236");

        await helper.AddTransaction(transactionDate, "Test Merchant 3", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 17, authCode: "AUTH231");
        await helper.AddTransaction(transactionDate, "Test Merchant 3", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 18, authCode: "AUTH1232");
        await helper.AddTransaction(transactionDate, "Test Merchant 3", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 19, authCode: "AUTH1233");
        await helper.AddTransaction(transactionDate, "Test Merchant 3", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 20, authCode: "AUTH1234");
        await helper.AddTransaction(transactionDate, "Test Merchant 3", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 21, authCode: "AUTH1235");
        await helper.AddTransaction(transactionDate, "Test Merchant 3", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 22, authCode: "AUTH1236");
        await helper.AddTransaction(transactionDate, "Test Merchant 3", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 23, authCode: "AUTH1237");
        await helper.AddTransaction(transactionDate, "Test Merchant 3", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 24, authCode: "AUTH1228");
        await helper.AddTransaction(transactionDate, "Test Merchant 3", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 25, authCode: "AUTH1229");
        await helper.AddTransaction(transactionDate, "Test Merchant 3", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 26, authCode: "AUTH1123");
        await helper.AddTransaction(transactionDate, "Test Merchant 3", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 27, authCode: "AUTH1236");
        await helper.AddTransaction(transactionDate, "Test Merchant 3", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 28, authCode: "AUTH1237");

        TransactionSearchRequest searchRequest = new TransactionSearchRequest
        {
            QueryDate = transactionDate,
            Merchants = new List<int>(){
                                             2,3
                                         }
        };

        // No Paging
        var result = await ApiClient.TransactionSearch(string.Empty, Guid.NewGuid(), searchRequest, null, null, null, null, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        List<TransactionResult> searchResult = result.Data;

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
        searchResult.Count(s => s.MerchantName == "Test Merchant 2").ShouldBe(6);
        searchResult.Count(s => s.MerchantName == "Test Merchant 3").ShouldBe(4);

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
        searchResult.Count(s => s.MerchantName == "Test Merchant 2").ShouldBe(1);
        searchResult.Count(s => s.MerchantName == "Test Merchant 3").ShouldBe(4);
    }

    [Fact]
    public async Task FactTransactionsController_TransactionSearch_OperatorFiltering_TransactionReturned()
    {

        DateTime transactionDate = new DateTime(2024, 3, 19);

        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 1, authCode: "AUTH231");
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Healthcare Centre 1 Contract", "10 KES Voucher", "0000", transactionReportingId: 2, authCode: "AUTH1232");
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "PataPawa PostPay Contract", "Post Pay Bill Pay", "0000", transactionReportingId: 3, authCode: "AUTH1233");
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 4, authCode: "AUTH1234");
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 5, authCode: "AUTH1235");
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Healthcare Centre 1 Contract", "10 KES Voucher", "0000", transactionReportingId: 6, authCode: "AUTH1236");
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "200 KES Topup", "0000", transactionReportingId: 7, authCode: "AUTH1237");
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "PataPawa PostPay Contract", "Post Pay Bill Pay", "0000", transactionReportingId: 8, authCode: "AUTH1228");
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "PataPawa PrePay Contract", "Pre Pay Bill Pay", "0000", transactionReportingId: 9, authCode: "AUTH1229");
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "PataPawa PrePay Contract", "Pre Pay Bill Pay", "0000", transactionReportingId: 10, authCode: "AUTH1123");

        TransactionSearchRequest searchRequest = new TransactionSearchRequest
        {
            QueryDate = transactionDate,
            Operators = new List<int>(){
                                             2,4
                                         }
        };

        // No Paging
        var result = await ApiClient.TransactionSearch(string.Empty, Guid.NewGuid(), searchRequest, null, null, null, null, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        List<TransactionResult> searchResult = result.Data;
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

        result= await ApiClient.TransactionSearch(string.Empty, Guid.NewGuid(), searchRequest, 2, 2, null, null, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        searchResult = result.Data;
        searchResult.Count.ShouldBe(2);
        searchResult.Any(s => s.TransactionReportingId == 9).ShouldBeTrue();
        searchResult.Any(s => s.TransactionReportingId == 10).ShouldBeTrue();
        searchResult.Count(s => s.OperatorName == "Voucher").ShouldBe(0);
        searchResult.Count(s => s.OperatorName == "PataPawa PrePay").ShouldBe(2);
    }

    [Fact]
    public async Task FactTransactionsController_TransactionSearch_SortingTest_TransactionReturned()
    {

        DateTime transactionDate = new DateTime(2024, 3, 19);

        // Add some transactions
        await helper.AddTransaction(transactionDate, "Test Merchant 1", "Safaricom Contract", "Custom", "0000", transactionAmount: 100);
        await helper.AddTransaction(transactionDate, "Test Merchant 2", "Healthcare Centre 1 Contract", "Custom", "0000", transactionAmount: 200);
        await helper.AddTransaction(transactionDate, "Test Merchant 3", "PataPawa PostPay Contract", "Post Pay Bill Pay", "0000", transactionAmount: 300);

        TransactionSearchRequest searchRequest = new TransactionSearchRequest
        {
            QueryDate = transactionDate
        };
        // Default Sort
        var result =  await ApiClient.TransactionSearch(string.Empty, Guid.NewGuid(), searchRequest, null, null, null, null, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        var searchResult = result.Data;
        searchResult.Count.ShouldBe(3);
        searchResult[0].TransactionAmount.ShouldBe(100);
        searchResult[1].TransactionAmount.ShouldBe(200);
        searchResult[2].TransactionAmount.ShouldBe(300);

        // Sort By merchant Name ascending
        result = await ApiClient.TransactionSearch(string.Empty, Guid.NewGuid(), searchRequest, null, null, SortField.MerchantName, SortDirection.Ascending, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        searchResult = result.Data;
        searchResult.Count.ShouldBe(3);
        searchResult[0].MerchantName.ShouldBe("Test Merchant 1");
        searchResult[1].MerchantName.ShouldBe("Test Merchant 2");
        searchResult[2].MerchantName.ShouldBe("Test Merchant 3");

        // Sort By merchant Name descending
        result = await ApiClient.TransactionSearch(string.Empty, Guid.NewGuid(), searchRequest, null, null, SortField.MerchantName, SortDirection.Descending, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        searchResult = result.Data;
        searchResult.Count.ShouldBe(3);
        searchResult[0].MerchantName.ShouldBe("Test Merchant 3");
        searchResult[1].MerchantName.ShouldBe("Test Merchant 2");
        searchResult[2].MerchantName.ShouldBe("Test Merchant 1");

        // Sort By operator Name ascending
        result = await ApiClient.TransactionSearch(string.Empty, Guid.NewGuid(), searchRequest, null, null, SortField.OperatorName, SortDirection.Ascending, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        searchResult = result.Data;

        searchResult.Count.ShouldBe(3);
        searchResult[0].OperatorName.ShouldBe("PataPawa PostPay");
        searchResult[1].OperatorName.ShouldBe("Safaricom");
        searchResult[2].OperatorName.ShouldBe("Voucher");

        // Sort By operator Name descending
        result = await ApiClient.TransactionSearch(string.Empty, Guid.NewGuid(), searchRequest, null, null, SortField.OperatorName, SortDirection.Descending, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        searchResult = result.Data;
        searchResult.Count.ShouldBe(3);
        searchResult[0].OperatorName.ShouldBe("Voucher");
        searchResult[1].OperatorName.ShouldBe("Safaricom");
        searchResult[2].OperatorName.ShouldBe("PataPawa PostPay");

        // Sort By transaction amount ascending
        result= await ApiClient.TransactionSearch(string.Empty, Guid.NewGuid(), searchRequest, null, null, SortField.TransactionAmount, SortDirection.Ascending, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        searchResult = result.Data;
        searchResult.Count.ShouldBe(3);
        searchResult[0].TransactionAmount.ShouldBe(100);
        searchResult[1].TransactionAmount.ShouldBe(200);
        searchResult[2].TransactionAmount.ShouldBe(300);

        // Sort By transaction amount descending
        result= await ApiClient.TransactionSearch(string.Empty, Guid.NewGuid(), searchRequest, null, null, SortField.TransactionAmount, SortDirection.Descending, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        searchResult = result.Data;
        searchResult.Count.ShouldBe(3);
        searchResult[0].TransactionAmount.ShouldBe(300);
        searchResult[1].TransactionAmount.ShouldBe(200);
        searchResult[2].TransactionAmount.ShouldBe(100);
    }

    [Fact]
    public async Task FactTransactionsControllerController_GetMerchantsByLastDaleDate_MerchantsReturned()
    {
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
}

