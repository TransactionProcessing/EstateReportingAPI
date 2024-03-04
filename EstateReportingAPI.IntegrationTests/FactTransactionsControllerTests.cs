namespace EstateReportingAPI.IntegrationTests;

using System.Collections.Generic;
using System.Security.Policy;
using DataTransferObjects;
using EstateManagement.Database.Contexts;
using EstateManagement.Database.Entities;
using EstateReportingAPI.DataTrasferObjects;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Newtonsoft.Json;
using Shared.IntegrationTesting;
using Shouldly;
using Xunit;

public class FactTransactionsControllerTests : ControllerTestsBase{
    
    protected override async Task ClearStandingData(){
        await this.helper.DeleteAllContracts();
        await this.helper.DeleteAllEstateOperator();
        await this.helper.DeleteAllMerchants();
    }

    protected override async Task SetupStandingData(){
        // Estates
        await helper.AddEstate("Test Estate", "Ref1");

        // Estate Operators
        await helper.AddEstateOperator("Test Estate", "Safaricom");
        await helper.AddEstateOperator("Test Estate", "Voucher");
        await helper.AddEstateOperator("Test Estate", "PataPawa PostPay");
        await helper.AddEstateOperator("Test Estate", "PataPawa PrePay");

        // Merchants
        await helper.AddMerchant("Test Estate", "Test Merchant 1", DateTime.MinValue);
        await helper.AddMerchant("Test Estate", "Test Merchant 2", DateTime.MinValue);
        await helper.AddMerchant("Test Estate", "Test Merchant 3", DateTime.MinValue);
        await helper.AddMerchant("Test Estate", "Test Merchant 4", DateTime.MinValue);

        // Contracts & Products
        List<(String productName, Int32 productType, Decimal? value)> safaricomProductList = new(){
                                                                                                      ("200 KES Topup", 0, 200.00m),
                                                                                                      ("100 KES Topup", 0, 100.00m),
                                                                                                      ("50 KES Topup", 0, 50.00m),
                                                                                                      ("Custom", 0, null)
                                                                                                  };
        await helper.AddContractWithProducts("Test Estate", "Safaricom Contract", "Safaricom", safaricomProductList);

        List<(String productName, Int32 productType, Decimal? value)> voucherProductList = new(){
                                                                                                    ("10 KES Voucher", 0, 10.00m),
                                                                                                    ("Custom", 0, null)
                                                                                                };
        await helper.AddContractWithProducts("Test Estate", "Healthcare Centre 1 Contract", "Voucher", voucherProductList);

        List<(String productName, Int32 productType, Decimal? value)> postPayProductList = new(){
                                                                                                    ("Post Pay Bill Pay", 0, null)
                                                                                                };
        await helper.AddContractWithProducts("Test Estate", "PataPawa PostPay Contract", "PataPawa PostPay", postPayProductList);

        List<(String productName, Int32 productType, Decimal? value)> prePayProductList = new(){
                                                                                                   ("Pre Pay Bill Pay", 0, null)
                                                                                               };
        await helper.AddContractWithProducts("Test Estate", "PataPawa PrePay Contract", "PataPawa PrePay", prePayProductList);

        // Response Codes
        await helper.AddResponseCode(0, "Success");
        await helper.AddResponseCode(1000, "Unknown Device");
        await helper.AddResponseCode(1001, "Unknown Estate");
        await helper.AddResponseCode(1002, "Unknown Merchant");
        await helper.AddResponseCode(1003, "No Devices Configured");

        merchantsList = this.context.Merchants.Select(m => m.Name).ToList();
        this.contractList = this.context.Contracts
                                 .Join(
                                       this.context.EstateOperators,
                                       c => c.OperatorId,
                                       o => o.OperatorId,
                                       (c, o) => new { c.Description, OperatorName = o.Name }
                                      )
                                 .ToList().Select(x => (x.Description, x.OperatorName))
                                 .ToList();


        var query1 = this.context.Contracts
                         .GroupJoin(
                                    this.context.ContractProducts,
                                    c => c.ContractReportingId,
                                    cp => cp.ContractReportingId,
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
    public async Task FactTransactionsControllerController_TodaysSales_SalesReturned(){
        List<Transaction>? todaysTransactions = new List<Transaction>();
        List<Transaction> comparisonDateTransactions = new List<Transaction>();

        DateTime todaysDateTime = DateTime.Now;
        DateTime comparisonDate = DateTime.Now.AddDays(-1).AddHours(-1);

        Dictionary<String, Int32> transactionCounts = new(){
                                                               { "Test Merchant 1", 15 },
                                                               { "Test Merchant 2", 18 },
                                                               { "Test Merchant 3", 9 },
                                                               { "Test Merchant 4", 0 }
                                                           };

        // Todays sales
        foreach (String merchantName in merchantsList){
            foreach ((String contract, String operatorname) contract in contractList){
                var productList = contractProducts.Single(cp => cp.Key == contract.contract).Value;
                foreach (String product in productList){
                    var transactionCount = transactionCounts.Single(m => m.Key == merchantName).Value;
                    for (int i = 0; i < transactionCount; i++){
                        Transaction transaction = await helper.AddTransaction(todaysDateTime.AddHours(-1), merchantName, contract.contract, product, "0000");
                        todaysTransactions.Add(transaction);
                    }
                }
            }
        }

        // Comparison Date sales
        foreach (String merchantName in merchantsList){
            foreach ((String contract, String operatorname) contract in contractList){
                var productList = contractProducts.Single(cp => cp.Key == contract.contract).Value;
                foreach (String product in productList){
                    var transactionCount = transactionCounts.Single(m => m.Key == merchantName).Value;
                    for (int i = 0; i < transactionCount; i++){
                        Transaction transaction = await helper.AddTransaction(comparisonDate, merchantName, contract.contract, product, "0000");
                        comparisonDateTransactions.Add(transaction);
                    }
                }
            }
        }

        TodaysSales? todaysSales = await this.CreateAndSendHttpRequestMessage<TodaysSales>($"api/facts/transactions/todayssales?comparisonDate={comparisonDate.ToString("yyyy-MM-dd")}", CancellationToken.None);
        todaysSales.ShouldNotBeNull();
        todaysSales.ComparisonSalesCount.ShouldBe(comparisonDateTransactions.Count);
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Sum(c => c.TransactionAmount));

        todaysSales.TodaysSalesCount.ShouldBe(todaysTransactions.Count);
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Sum(c => c.TransactionAmount));
    }

    [Fact]
    public async Task FactTransactionsControllerController_TodaysSalesCountByHour_SalesReturned(){
        List<Transaction> todaysTransactions = new List<Transaction>();
        List<Transaction> comparisonDateTransactions = new List<Transaction>();

        Dictionary<String, Int32> transactionCounts = new(){
                                                               { "Test Merchant 1", 3 },
                                                               { "Test Merchant 2", 6 },
                                                               { "Test Merchant 3", 2 },
                                                               { "Test Merchant 4", 0 }
                                                           };

        // TODO: make counts dynamic
        DateTime todaysDateTime = DateTime.Now;

        for (int hour = 0; hour < 24; hour++){
            List<Transaction> localList = new List<Transaction>();
            DateTime date = new DateTime(todaysDateTime.Year, todaysDateTime.Month, todaysDateTime.Day, hour, 0, 0);
            foreach (String merchantName in merchantsList){
                foreach ((String contract, String operatorname) contract in contractList){
                    var productList = contractProducts.Single(cp => cp.Key == contract.contract).Value;
                    foreach (String product in productList){
                        var transactionCount = transactionCounts.Single(m => m.Key == merchantName).Value;
                        for (int i = 0; i < transactionCount; i++){
                            Transaction transaction = await helper.AddTransaction(todaysDateTime.AddHours(-1), merchantName, contract.contract, product, "0000");
                            todaysTransactions.Add(transaction);
                        }
                    }
                }
            }

            todaysTransactions.AddRange(localList);
        }

        DateTime comparisonDate = todaysDateTime.AddDays(-1);
        for (int hour = 0; hour < 24; hour++){
            List<Transaction> localList = new List<Transaction>();
            DateTime date = new DateTime(comparisonDate.Year, comparisonDate.Month, comparisonDate.Day, hour, 0, 0);
            foreach (String merchantName in merchantsList){
                foreach (var contract in contractList){
                    var productList = contractProducts.Single(cp => cp.Key == contract.contract).Value;
                    foreach (String product in productList){
                        var transactionCount = transactionCounts.Single(m => m.Key == merchantName).Value;
                        for (int i = 0; i < transactionCount; i++){
                            Transaction transaction = await helper.AddTransaction(comparisonDate, merchantName, contract.contract, product, "0000");
                            comparisonDateTransactions.Add(transaction);
                        }
                    }
                }
            }

            comparisonDateTransactions.AddRange(localList);
        }

        List<TodaysSalesCountByHour>? todaysSalesCountByHour = await this.CreateAndSendHttpRequestMessage<List<TodaysSalesCountByHour>>($"api/facts/transactions/todayssales/countbyhour?comparisonDate={comparisonDate.ToString("yyyy-MM-dd")}", CancellationToken.None);
        todaysSalesCountByHour.ShouldNotBeNull();
        foreach (TodaysSalesCountByHour salesCountByHour in todaysSalesCountByHour){
            IEnumerable<Transaction> todayHour = todaysTransactions.Where(t => t.TransactionDateTime.Hour == salesCountByHour.Hour);
            IEnumerable<Transaction> comparisonHour = comparisonDateTransactions.Where(t => t.TransactionDateTime.Hour == salesCountByHour.Hour);
            salesCountByHour.ComparisonSalesCount.ShouldBe(comparisonHour.Count());
            salesCountByHour.TodaysSalesCount.ShouldBe(todayHour.Count());
        }
    }

    [Fact]
    public async Task FactTransactionsControllerController_TodaysSalesValueByHour_SalesReturned(){
        var todaysTransactions = new List<Transaction>();
        var comparisonDateTransactions = new List<Transaction>();

        Dictionary<String, Int32> transactionCounts = new(){
                                                               { "Test Merchant 1", 3 },
                                                               { "Test Merchant 2", 6 },
                                                               { "Test Merchant 3", 2 },
                                                               { "Test Merchant 4", 0 }
                                                           };

        DateTime todaysDateTime = DateTime.Now;

        for (int hour = 0; hour < 24; hour++){
            List<Transaction> localList = new List<Transaction>();
            DateTime date = new DateTime(todaysDateTime.Year, todaysDateTime.Month, todaysDateTime.Day, hour, 0, 0);
            foreach (String merchantName in merchantsList){
                foreach (var contract in contractList){
                    var productList = contractProducts.Single(cp => cp.Key == contract.contract).Value;
                    foreach (String product in productList){
                        var transactionCount = transactionCounts.Single(m => m.Key == merchantName).Value;
                        for (int i = 0; i < transactionCount; i++){
                            Transaction transaction = await helper.AddTransaction(todaysDateTime.AddHours(-1), merchantName, contract.contract, product, "0000");
                            todaysTransactions.Add(transaction);
                        }
                    }
                }
            }

            todaysTransactions.AddRange(localList);
        }

        DateTime comparisonDate = todaysDateTime.AddDays(-1);
        for (int hour = 0; hour < 24; hour++){
            List<Transaction> localList = new List<Transaction>();
            DateTime date = new DateTime(comparisonDate.Year, comparisonDate.Month, comparisonDate.Day, hour, 0, 0);
            foreach (String merchantName in merchantsList){
                foreach (var contract in contractList){
                    var productList = contractProducts.Single(cp => cp.Key == contract.contract).Value;
                    foreach (String product in productList){
                        var transactionCount = transactionCounts.Single(m => m.Key == merchantName).Value;
                        for (int i = 0; i < transactionCount; i++){
                            Transaction transaction = await helper.AddTransaction(comparisonDate, merchantName, contract.contract, product, "0000");
                            comparisonDateTransactions.Add(transaction);
                        }
                    }
                }
            }

            comparisonDateTransactions.AddRange(localList);
        }

        List<TodaysSalesValueByHour>? todaysSalesValueByHour = await this.CreateAndSendHttpRequestMessage<List<TodaysSalesValueByHour>>($"api/facts/transactions/todayssales/valuebyhour?comparisonDate={comparisonDate.ToString("yyyy-MM-dd")}", CancellationToken.None);

        foreach (TodaysSalesValueByHour salesValueByHour in todaysSalesValueByHour){
            IEnumerable<Transaction> todayHour = todaysTransactions.Where(t => t.TransactionDateTime.Hour == salesValueByHour.Hour);
            IEnumerable<Transaction> comparisonHour = comparisonDateTransactions.Where(t => t.TransactionDateTime.Hour == salesValueByHour.Hour);
            salesValueByHour.ComparisonSalesValue.ShouldBe(comparisonHour.Sum(c => c.TransactionAmount));
            salesValueByHour.TodaysSalesValue.ShouldBe(todayHour.Sum(c => c.TransactionAmount));
        }
    }

    #endregion

    #region Todays Failed Sales Tests

    [Fact]
    public async Task FactTransactionsControllerController_TodaysFailedSales_SalesReturned(){
        EstateManagementGenericContext context = new EstateManagementSqlServerContext(ControllerTestsBase.GetLocalConnectionString($"EstateReportingReadModel{this.TestId.ToString()}"));
        var todaysTransactions = new List<Transaction>();
        var comparisonDateTransactions = new List<Transaction>();
        DatabaseHelper helper = new DatabaseHelper(context);
        // TODO: make counts dynamic
        DateTime todaysDateTime = DateTime.Now;

        Dictionary<String, Int32> transactionCounts = new(){
                                                               { "Test Merchant 1", 3 },
                                                               { "Test Merchant 2", 6 },
                                                               { "Test Merchant 3", 2 },
                                                               { "Test Merchant 4", 0 }
                                                           };

        foreach (String merchantName in merchantsList){
            foreach (var contract in contractList){
                var productList = contractProducts.Single(cp => cp.Key == contract.contract).Value;
                foreach (String product in productList){
                    var transactionCount = transactionCounts.Single(m => m.Key == merchantName).Value;
                    for (int i = 0; i < transactionCount; i++){
                        Transaction transaction = await helper.AddTransaction(todaysDateTime.AddHours(-1), merchantName, contract.contract, product, "1009");
                        todaysTransactions.Add(transaction);
                    }
                }
            }
        }

        DateTime comparisonDate = todaysDateTime.AddDays(-1);
        // Comparison Date sales
        foreach (String merchantName in merchantsList){
            foreach (var contract in contractList){
                var productList = contractProducts.Single(cp => cp.Key == contract.contract).Value;
                foreach (String product in productList){
                    var transactionCount = transactionCounts.Single(m => m.Key == merchantName).Value;
                    for (int i = 0; i < transactionCount; i++){
                        Transaction transaction = await helper.AddTransaction(comparisonDate, merchantName, contract.contract, product, "1009");
                        comparisonDateTransactions.Add(transaction);
                    }
                }
            }
        }

        TodaysSales? todaysSales = await this.CreateAndSendHttpRequestMessage<TodaysSales>($"api/facts/transactions/todaysfailedsales?responseCode=1009&comparisonDate={comparisonDate.ToString("yyyy-MM-dd")}", CancellationToken.None);

        todaysSales.ShouldNotBeNull();
        todaysSales.ComparisonSalesCount.ShouldBe(comparisonDateTransactions.Count);
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Sum(c => c.TransactionAmount));

        todaysSales.TodaysSalesCount.ShouldBe(todaysTransactions.Count);
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Sum(c => c.TransactionAmount));
    }

    #endregion

    [Fact]
    public async Task FactTransactionsController_GetTopBottomProductsByValue_BottomProducts_ProductsReturned(){
        DateTime todaysDateTime = DateTime.Now;

        String merchantName = this.merchantsList.First();
        (String contract, String operatorname) contract = this.contractList.Single(c => c.operatorname == "Safaricom");
        List<String> productList = contractProducts.Single(cp => cp.Key == contract.contract).Value;

        Dictionary<String, Int32> transactionCounts = new(){
                                                               { "200 KES Topup", 25 }, //5000
                                                               { "100 KES Topup", 15 }, // 1500 
                                                               { "50 KES Topup", 45 }, // 2250
                                                               { "Custom", 8 } // 600
                                                           };
        Dictionary<String, List<Transaction>> transactionsDictionary = new();
        foreach (String product in productList){
            Int32 transactionCount = transactionCounts.Single(m => m.Key == product).Value;
            for (int i = 0; i < transactionCount; i++){
                Transaction transaction = await helper.AddTransaction(todaysDateTime.AddHours(-1), merchantName, contract.contract, product, "0000");
                if (transactionsDictionary.ContainsKey(product) == false){
                    transactionsDictionary.Add(product, new List<Transaction>());
                }

                transactionsDictionary[product].Add(transaction);
            }
        }

        List<TopBottomProductData>? topBottomProductData = await this.CreateAndSendHttpRequestMessage<List<TopBottomProductData>>($"api/facts/transactions/products/topbottombyvalue?count=3&topOrBottom=bottom", CancellationToken.None);

        topBottomProductData[0].ProductName.ShouldBe("Custom");
        topBottomProductData[0].SalesValue.ShouldBe(transactionsDictionary["Custom"].Sum(p => p.TransactionAmount));
        topBottomProductData[1].ProductName.ShouldBe("100 KES Topup");
        topBottomProductData[1].SalesValue.ShouldBe(transactionsDictionary["100 KES Topup"].Sum(p => p.TransactionAmount));
        topBottomProductData[2].ProductName.ShouldBe("50 KES Topup");
        topBottomProductData[2].SalesValue.ShouldBe(transactionsDictionary["50 KES Topup"].Sum(p => p.TransactionAmount));
    }

    [Fact]
    public async Task FactTransactionsController_GetTopBottomProductsByValue_TopProducts_ProductsReturned(){
        DateTime todaysDateTime = DateTime.Now;

        String merchantName = this.merchantsList.First();
        (String contract, String operatorname) contract = this.contractList.Single(c => c.operatorname == "Safaricom");
        List<String> productList = contractProducts.Single(cp => cp.Key == contract.contract).Value;

        Dictionary<String, Int32> transactionCounts = new(){
                                                               { "200 KES Topup", 25 }, //5000
                                                               { "100 KES Topup", 15 }, // 1500 
                                                               { "50 KES Topup", 45 }, // 2250
                                                               { "Custom", 8 } // 600
                                                           };
        Dictionary<String, List<Transaction>> transactionsDictionary = new();
        foreach (String product in productList){
            Int32 transactionCount = transactionCounts.Single(m => m.Key == product).Value;
            for (int i = 0; i < transactionCount; i++){
                Transaction transaction = await helper.AddTransaction(todaysDateTime.AddHours(-1), merchantName, contract.contract, product, "0000");
                if (transactionsDictionary.ContainsKey(product) == false){
                    transactionsDictionary.Add(product, new List<Transaction>());
                }

                transactionsDictionary[product].Add(transaction);
            }
        }

        List<TopBottomProductData>? topBottomProductData = await this.CreateAndSendHttpRequestMessage<List<TopBottomProductData>>($"api/facts/transactions/products/topbottombyvalue?count=3&topOrBottom=top", CancellationToken.None);

        topBottomProductData[0].ProductName.ShouldBe("200 KES Topup");
        topBottomProductData[0].SalesValue.ShouldBe(transactionsDictionary["200 KES Topup"].Sum(p => p.TransactionAmount));
        topBottomProductData[1].ProductName.ShouldBe("50 KES Topup");
        topBottomProductData[1].SalesValue.ShouldBe(transactionsDictionary["50 KES Topup"].Sum(p => p.TransactionAmount));
        topBottomProductData[2].ProductName.ShouldBe("100 KES Topup");
        topBottomProductData[2].SalesValue.ShouldBe(transactionsDictionary["100 KES Topup"].Sum(p => p.TransactionAmount));
    }

    [Fact]
    public async Task FactTransactionsController_GetTopBottomOperatorsByValue_BottomOperators_OperatorsReturned(){
        DateTime todaysDateTime = DateTime.Now;

        Dictionary<String, Int32> transactionCounts = new(){
                                                               { "Safaricom", 25 }, // 5000
                                                               { "Voucher", 15 }, // 150 
                                                               { "PataPawa PostPay", 45 }, // 3375
                                                               { "PataPawa PrePay", 8 } // 600
                                                           };

        Dictionary<String, List<Transaction>> transactionsDictionary = new();
        String merchantName = this.merchantsList.First();
        //List<String> productList = contractProducts.Single(cp => cp.Key == contractName).Value;
        foreach (KeyValuePair<String, Int32> transactionCount in transactionCounts){
            var contract = this.contractList.Single(s => s.operatorname == transactionCount.Key);
            var products = this.contractProducts.Single(p => p.Key == contract.contract);
            var productname = products.Value.First();
            for (int i = 0; i < transactionCount.Value; i++){
                Transaction transaction = await helper.AddTransaction(todaysDateTime.AddHours(-1), merchantName, contract.contract, productname, "0000");
                if (transactionsDictionary.ContainsKey(transactionCount.Key) == false){
                    transactionsDictionary.Add(transactionCount.Key, new List<Transaction>());
                }

                transactionsDictionary[transactionCount.Key].Add(transaction);
            }
        }

        List<TopBottomOperatorData>? topBottomOperatorData = await this.CreateAndSendHttpRequestMessage<List<TopBottomOperatorData>>($"api/facts/transactions/operators/topbottombyvalue?count=3&topOrBottom=bottom", CancellationToken.None);

        topBottomOperatorData.ShouldNotBeNull();
        topBottomOperatorData[0].OperatorName.ShouldBe("Voucher");
        topBottomOperatorData[0].SalesValue.ShouldBe(transactionsDictionary["Voucher"].Sum(p => p.TransactionAmount));
        topBottomOperatorData[1].OperatorName.ShouldBe("PataPawa PrePay");
        topBottomOperatorData[1].SalesValue.ShouldBe(transactionsDictionary["PataPawa PrePay"].Sum(p => p.TransactionAmount));
        topBottomOperatorData[2].OperatorName.ShouldBe("PataPawa PostPay");
        topBottomOperatorData[2].SalesValue.ShouldBe(transactionsDictionary["PataPawa PostPay"].Sum(p => p.TransactionAmount));
    }

    [Fact]
    public async Task FactTransactionsController_GetTopBottomOperatorsByValue_TopOperators_OperatorsReturned(){
        DateTime todaysDateTime = DateTime.Now;

        Dictionary<String, Int32> transactionCounts = new(){
                                                               { "Safaricom", 25 }, // 5000
                                                               { "Voucher", 15 }, // 150 
                                                               { "PataPawa PostPay", 45 }, // 3375
                                                               { "PataPawa PrePay", 8 } // 600
                                                           };

        Dictionary<String, List<Transaction>> transactionsDictionary = new();
        String merchantName = this.merchantsList.First();
        //List<String> productList = contractProducts.Single(cp => cp.Key == contractName).Value;
        foreach (KeyValuePair<String, Int32> transactionCount in transactionCounts){
            var contract = this.contractList.Single(s => s.operatorname == transactionCount.Key);
            var products = this.contractProducts.Single(p => p.Key == contract.contract);
            var productname = products.Value.First();
            for (int i = 0; i < transactionCount.Value; i++){
                Transaction transaction = await helper.AddTransaction(todaysDateTime.AddHours(-1), merchantName, contract.contract, productname, "0000");
                if (transactionsDictionary.ContainsKey(transactionCount.Key) == false){
                    transactionsDictionary.Add(transactionCount.Key, new List<Transaction>());
                }

                transactionsDictionary[transactionCount.Key].Add(transaction);
            }
        }

        List<TopBottomOperatorData>? topBottomOperatorData = await this.CreateAndSendHttpRequestMessage<List<TopBottomOperatorData>>($"api/facts/transactions/operators/topbottombyvalue?count=3&topOrBottom=top", CancellationToken.None);

        topBottomOperatorData[0].OperatorName.ShouldBe("Safaricom");
        topBottomOperatorData[0].SalesValue.ShouldBe(transactionsDictionary["Safaricom"].Sum(p => p.TransactionAmount));
        topBottomOperatorData[1].OperatorName.ShouldBe("PataPawa PostPay");
        topBottomOperatorData[1].SalesValue.ShouldBe(transactionsDictionary["PataPawa PostPay"].Sum(p => p.TransactionAmount));
        topBottomOperatorData[2].OperatorName.ShouldBe("PataPawa PrePay");
        topBottomOperatorData[2].SalesValue.ShouldBe(transactionsDictionary["PataPawa PrePay"].Sum(p => p.TransactionAmount));
    }

    [Fact]
    public async Task FactTransactionsController_GetTopBottomMerchantsByValue_BottomMerchants_MerchantsReturned(){
        DateTime todaysDateTime = DateTime.Now;

        Dictionary<String, Int32> transactionCounts = new(){
                                                               { "Test Merchant 1", 25 },
                                                               { "Test Merchant 2", 15 },
                                                               { "Test Merchant 3", 45 },
                                                               { "Test Merchant 4", 8 }
                                                           };

        Dictionary<String, List<Transaction>> transactionsDictionary = new();
        foreach (KeyValuePair<String, Int32> transactionCount in transactionCounts){
            var contract = this.contractList.First();
            var products = this.contractProducts.Single(p => p.Key == contract.contract);
            var productname = products.Value.First();
            for (int i = 0; i < transactionCount.Value; i++){
                Transaction transaction = await helper.AddTransaction(todaysDateTime.AddHours(-1), transactionCount.Key, contract.contract, productname, "0000");
                if (transactionsDictionary.ContainsKey(transactionCount.Key) == false){
                    transactionsDictionary.Add(transactionCount.Key, new List<Transaction>());
                }

                transactionsDictionary[transactionCount.Key].Add(transaction);
            }
        }

        List<TopBottomMerchantData> topBottomMerchantData = await this.CreateAndSendHttpRequestMessage<List<TopBottomMerchantData>>($"api/facts/transactions/merchants/topbottombyvalue?count=3&topOrBottom=bottom", CancellationToken.None);

        topBottomMerchantData.ShouldNotBeNull();
        topBottomMerchantData[0].MerchantName.ShouldBe("Test Merchant 4");
        topBottomMerchantData[0].SalesValue.ShouldBe(transactionsDictionary["Test Merchant 4"].Sum(p => p.TransactionAmount));
        topBottomMerchantData[1].MerchantName.ShouldBe("Test Merchant 2");
        topBottomMerchantData[1].SalesValue.ShouldBe(transactionsDictionary["Test Merchant 2"].Sum(p => p.TransactionAmount));
        topBottomMerchantData[2].MerchantName.ShouldBe("Test Merchant 1");
        topBottomMerchantData[2].SalesValue.ShouldBe(transactionsDictionary["Test Merchant 1"].Sum(p => p.TransactionAmount));
    }

    [Fact]
    public async Task FactTransactionsController_GetTopBottomMerchantsByValue_TopMerchants_MerchantsReturned(){
        DateTime todaysDateTime = DateTime.Now;

        Dictionary<String, Int32> transactionCounts = new(){
                                                               { "Test Merchant 1", 25 },
                                                               { "Test Merchant 2", 15 },
                                                               { "Test Merchant 3", 45 },
                                                               { "Test Merchant 4", 8 }
                                                           };

        Dictionary<String, List<Transaction>> transactionsDictionary = new();
        foreach (KeyValuePair<String, Int32> transactionCount in transactionCounts){
            var contract = this.contractList.First();
            var products = this.contractProducts.Single(p => p.Key == contract.contract);
            var productname = products.Value.First();
            for (int i = 0; i < transactionCount.Value; i++){
                Transaction transaction = await helper.AddTransaction(todaysDateTime.AddHours(-1), transactionCount.Key, contract.contract, productname, "0000");
                if (transactionsDictionary.ContainsKey(transactionCount.Key) == false){
                    transactionsDictionary.Add(transactionCount.Key, new List<Transaction>());
                }

                transactionsDictionary[transactionCount.Key].Add(transaction);
            }
        }

        List<TopBottomMerchantData> topBottomMerchantData = await this.CreateAndSendHttpRequestMessage<List<TopBottomMerchantData>>($"api/facts/transactions/merchants/topbottombyvalue?count=3&topOrBottom=top", CancellationToken.None);

        topBottomMerchantData.ShouldNotBeNull();
        topBottomMerchantData[0].MerchantName.ShouldBe("Test Merchant 3");
        topBottomMerchantData[0].SalesValue.ShouldBe(transactionsDictionary["Test Merchant 3"].Sum(p => p.TransactionAmount));
        topBottomMerchantData[1].MerchantName.ShouldBe("Test Merchant 1");
        topBottomMerchantData[1].SalesValue.ShouldBe(transactionsDictionary["Test Merchant 1"].Sum(p => p.TransactionAmount));
        topBottomMerchantData[2].MerchantName.ShouldBe("Test Merchant 2");
        topBottomMerchantData[2].SalesValue.ShouldBe(transactionsDictionary["Test Merchant 2"].Sum(p => p.TransactionAmount));
    }

    [Fact]
    public async Task FactTransactionsControllerController_MerchantPerformance_AllMerchants_SalesReturned(){

        var todaysTransactions = new List<Transaction>();
        var comparisonDateTransactions = new List<Transaction>();

        DateTime todaysDateTime = DateTime.Now;
        DateTime comparisonDate = DateTime.Now.AddDays(-1).AddHours(-1);

        Dictionary<String, Int32> transactionCounts = new(){
                                                               { "Test Merchant 1", 15 },
                                                               { "Test Merchant 2", 18 },
                                                               { "Test Merchant 3", 9 },
                                                               { "Test Merchant 4", 3 },
                                                           };

        // Todays sales
        foreach (String merchantName in merchantsList){
            foreach ((String contract, String operatorname) contract in contractList){
                var productList = contractProducts.Single(cp => cp.Key == contract.contract).Value;
                foreach (String product in productList){
                    var transactionCount = transactionCounts.Single(m => m.Key == merchantName).Value;
                    for (int i = 0; i < transactionCount; i++){
                        Transaction transaction = await helper.AddTransaction(todaysDateTime.AddHours(-1), merchantName, contract.contract, product, "0000");
                        todaysTransactions.Add(transaction);
                    }
                }
            }
        }

        // Comparison Date sales
        foreach (String merchantName in merchantsList){
            foreach ((String contract, String operatorname) contract in contractList){
                var productList = contractProducts.Single(cp => cp.Key == contract.contract).Value;
                foreach (String product in productList){
                    var transactionCount = transactionCounts.Single(m => m.Key == merchantName).Value;
                    for (int i = 0; i < transactionCount; i++){
                        Transaction transaction = await helper.AddTransaction(comparisonDate, merchantName, contract.contract, product, "0000");
                        comparisonDateTransactions.Add(transaction);
                    }
                }
            }
        }

        TodaysSales todaysSales = await this.CreateAndSendHttpRequestMessage<TodaysSales>($"api/facts/transactions/merchants/performance?comparisonDate={comparisonDate.ToString("yyyy-MM-dd")}", CancellationToken.None);

        todaysSales.ShouldNotBeNull();
        todaysSales.ComparisonSalesCount.ShouldBe(comparisonDateTransactions.Count);
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Sum(c => c.TransactionAmount));

        todaysSales.TodaysSalesCount.ShouldBe(todaysTransactions.Count);
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Sum(c => c.TransactionAmount));
    }

    [Fact]
    public async Task FactTransactionsControllerController_MerchantPerformance_SingleMerchant_SalesReturned(){

        var todaysTransactions = new List<Transaction>();
        var comparisonDateTransactions = new List<Transaction>();

        DateTime todaysDateTime = DateTime.Now;
        DateTime comparisonDate = DateTime.Now.AddDays(-1).AddHours(-1);

        Dictionary<String, Int32> transactionCounts = new(){
                                                               { "Test Merchant 1", 15 },
                                                               { "Test Merchant 2", 18 },
                                                               { "Test Merchant 3", 9 },
                                                               { "Test Merchant 4", 3 },
                                                           };

        // Todays sales
        foreach (String merchantName in merchantsList){
            foreach ((String contract, String operatorname) contract in contractList){
                var productList = contractProducts.Single(cp => cp.Key == contract.contract).Value;
                foreach (String product in productList){
                    var transactionCount = transactionCounts.Single(m => m.Key == merchantName).Value;
                    for (int i = 0; i < transactionCount; i++){
                        Transaction transaction = await helper.AddTransaction(todaysDateTime.AddHours(-1), merchantName, contract.contract, product, "0000");
                        todaysTransactions.Add(transaction);
                    }
                }
            }
        }

        // Comparison Date sales
        foreach (String merchantName in merchantsList){
            foreach ((String contract, String operatorname) contract in contractList){
                var productList = contractProducts.Single(cp => cp.Key == contract.contract).Value;
                foreach (String product in productList){
                    var transactionCount = transactionCounts.Single(m => m.Key == merchantName).Value;
                    for (int i = 0; i < transactionCount; i++){
                        Transaction transaction = await helper.AddTransaction(comparisonDate, merchantName, contract.contract, product, "0000");
                        comparisonDateTransactions.Add(transaction);
                    }
                }
            }
        }

        List<Int32> merchantFilterList = new List<Int32>{
                                                            2
                                                        };
        string serializedArray = string.Join(",", merchantFilterList);

        TodaysSales todaysSales = await this.CreateAndSendHttpRequestMessage<TodaysSales>($"api/facts/transactions/merchants/performance?comparisonDate={comparisonDate.ToString("yyyy-MM-dd")}&merchantIds={serializedArray}", CancellationToken.None);

        todaysSales.ShouldNotBeNull();
        todaysSales.ComparisonSalesCount.ShouldBe(comparisonDateTransactions.Count(c => merchantFilterList.Contains(c.MerchantReportingId)));
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(c => merchantFilterList.Contains(c.MerchantReportingId)).Sum(c => c.TransactionAmount));

        todaysSales.TodaysSalesCount.ShouldBe(todaysTransactions.Count(c => merchantFilterList.Contains(c.MerchantReportingId)));
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(c => merchantFilterList.Contains(c.MerchantReportingId)).Sum(c => c.TransactionAmount));
    }

    [Fact]
    public async Task FactTransactionsControllerController_ProductPerformance_AllProducts_SalesReturned(){
        var todaysTransactions = new List<Transaction>();
        var comparisonDateTransactions = new List<Transaction>();

        DateTime todaysDateTime = DateTime.Now;
        DateTime comparisonDate = DateTime.Now.AddDays(-1).AddHours(-1);

        String merchantName = this.merchantsList.First();
        (String contract, String operatorname) contract = this.contractList.Single(c => c.operatorname == "Safaricom");
        List<String> productList = contractProducts.Single(cp => cp.Key == contract.contract).Value;

        Dictionary<String, Int32> transactionCounts = new(){
                                                               { "200 KES Topup", 25 }, //5000
                                                               { "100 KES Topup", 15 }, // 1500 
                                                               { "50 KES Topup", 45 }, // 2250
                                                               { "Custom", 8 } // 600
                                                           };
        foreach (String product in productList){
            Int32 transactionCount = transactionCounts.Single(m => m.Key == product).Value;
            for (int i = 0; i < transactionCount; i++){
                Transaction transaction = await helper.AddTransaction(todaysDateTime.AddHours(-1), merchantName, contract.contract, product, "0000");
                todaysTransactions.Add(transaction);
            }
        }

        foreach (String product in productList){
            Int32 transactionCount = transactionCounts.Single(m => m.Key == product).Value;
            for (int i = 0; i < transactionCount; i++){
                Transaction transaction = await helper.AddTransaction(comparisonDate, merchantName, contract.contract, product, "0000");
                comparisonDateTransactions.Add(transaction);
            }
        }

        TodaysSales todaysSales = await this.CreateAndSendHttpRequestMessage<TodaysSales>($"api/facts/transactions/products/performance?comparisonDate={comparisonDate.ToString("yyyy-MM-dd")}", CancellationToken.None);

        todaysSales.ShouldNotBeNull();
        todaysSales.ComparisonSalesCount.ShouldBe(comparisonDateTransactions.Count);
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Sum(c => c.TransactionAmount));

        todaysSales.TodaysSalesCount.ShouldBe(todaysTransactions.Count);
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Sum(c => c.TransactionAmount));
    }

    [Fact]
    public async Task FactTransactionsControllerController_ProductPerformance_SingleProduct_SalesReturned(){
        var todaysTransactions = new List<Transaction>();
        var comparisonDateTransactions = new List<Transaction>();

        DateTime todaysDateTime = DateTime.Now;
        DateTime comparisonDate = DateTime.Now.AddDays(-1).AddHours(-1);

        String merchantName = this.merchantsList.First();
        (String contract, String operatorname) contract = this.contractList.Single(c => c.operatorname == "Safaricom");
        List<String> productList = contractProducts.Single(cp => cp.Key == contract.contract).Value;

        Dictionary<String, Int32> transactionCounts = new(){
                                                               { "200 KES Topup", 25 }, //5000
                                                               { "100 KES Topup", 15 }, // 1500 
                                                               { "50 KES Topup", 45 }, // 2250
                                                               { "Custom", 8 } // 600
                                                           };
        foreach (String product in productList){
            Int32 transactionCount = transactionCounts.Single(m => m.Key == product).Value;
            for (int i = 0; i < transactionCount; i++){
                Transaction transaction = await helper.AddTransaction(todaysDateTime.AddHours(-1), merchantName, contract.contract, product, "0000");
                todaysTransactions.Add(transaction);
            }
        }

        foreach (String product in productList){
            Int32 transactionCount = transactionCounts.Single(m => m.Key == product).Value;
            for (int i = 0; i < transactionCount; i++){
                Transaction transaction = await helper.AddTransaction(comparisonDate, merchantName, contract.contract, product, "0000");
                comparisonDateTransactions.Add(transaction);
            }
        }

        List<Int32> productFilterList = new List<Int32>{
                                                           2
                                                       };
        string serializedArray = string.Join(",", productFilterList);

        TodaysSales todaysSales = await this.CreateAndSendHttpRequestMessage<TodaysSales>($"api/facts/transactions/products/performance?comparisonDate={comparisonDate.ToString("yyyy-MM-dd")}&productIds={serializedArray}", CancellationToken.None);

        todaysSales.ShouldNotBeNull();
        todaysSales.ComparisonSalesCount.ShouldBe(comparisonDateTransactions.Count(c => productFilterList.Contains(c.ContractProductReportingId)));
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(c => productFilterList.Contains(c.ContractProductReportingId)).Sum(c => c.TransactionAmount));

        todaysSales.TodaysSalesCount.ShouldBe(todaysTransactions.Count(c => productFilterList.Contains(c.ContractProductReportingId)));
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(c => productFilterList.Contains(c.ContractProductReportingId)).Sum(c => c.TransactionAmount));
    }

    [Fact]
    public async Task FactTransactionsControllerController_ProductPerformance_MultipleProducts_SalesReturned(){
        var todaysTransactions = new List<Transaction>();
        var comparisonDateTransactions = new List<Transaction>();

        DateTime todaysDateTime = DateTime.Now;
        DateTime comparisonDate = DateTime.Now.AddDays(-1).AddHours(-1);

        String merchantName = this.merchantsList.First();
        (String contract, String operatorname) contract = this.contractList.Single(c => c.operatorname == "Safaricom");
        List<String> productList = contractProducts.Single(cp => cp.Key == contract.contract).Value;

        Dictionary<String, Int32> transactionCounts = new(){
                                                               { "200 KES Topup", 25 }, //5000
                                                               { "100 KES Topup", 15 }, // 1500 
                                                               { "50 KES Topup", 45 }, // 2250
                                                               { "Custom", 8 } // 600
                                                           };
        foreach (String product in productList){
            Int32 transactionCount = transactionCounts.Single(m => m.Key == product).Value;
            for (int i = 0; i < transactionCount; i++){
                Transaction transaction = await helper.AddTransaction(todaysDateTime.AddHours(-1), merchantName, contract.contract, product, "0000");
                todaysTransactions.Add(transaction);
            }
        }

        foreach (String product in productList){
            Int32 transactionCount = transactionCounts.Single(m => m.Key == product).Value;
            for (int i = 0; i < transactionCount; i++){
                Transaction transaction = await helper.AddTransaction(comparisonDate, merchantName, contract.contract, product, "0000");
                comparisonDateTransactions.Add(transaction);
            }
        }

        List<Int32> productFilterList = new List<Int32>{
                                                           2,
                                                           3
                                                       };
        string serializedArray = string.Join(",", productFilterList);

        TodaysSales todaysSales = await this.CreateAndSendHttpRequestMessage<TodaysSales>($"api/facts/transactions/products/performance?comparisonDate={comparisonDate.ToString("yyyy-MM-dd")}&productIds={serializedArray}", CancellationToken.None);

        todaysSales.ShouldNotBeNull();
        todaysSales.ComparisonSalesCount.ShouldBe(comparisonDateTransactions.Count(c => productFilterList.Contains(c.ContractProductReportingId)));
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(c => productFilterList.Contains(c.ContractProductReportingId)).Sum(c => c.TransactionAmount));

        todaysSales.TodaysSalesCount.ShouldBe(todaysTransactions.Count(c => productFilterList.Contains(c.ContractProductReportingId)));
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(c => productFilterList.Contains(c.ContractProductReportingId)).Sum(c => c.TransactionAmount));
    }

    [Fact]
    public async Task FactTransactionsControllerController_OperatorPerformance_SingleOperator_SalesReturned(){
        var todaysTransactions = new List<Transaction>();
        var comparisonDateTransactions = new List<Transaction>();

        DateTime todaysDateTime = DateTime.Now;
        DateTime comparisonDate = DateTime.Now.AddDays(-1).AddHours(-1);

        Dictionary<String, Int32> transactionCounts = new(){
                                                               { "Safaricom", 25 }, // 5000
                                                               { "Voucher", 15 }, // 150 
                                                               { "PataPawa PostPay", 45 }, // 3375
                                                               { "PataPawa PrePay", 8 } // 600
                                                           };

        String merchantName = this.merchantsList.First();
        foreach (KeyValuePair<String, Int32> transactionCount in transactionCounts)
        {
            var contract = this.contractList.Single(s => s.operatorname == transactionCount.Key);
            var products = this.contractProducts.Single(p => p.Key == contract.contract);
            var productname = products.Value.First();
            for (int i = 0; i < transactionCount.Value; i++)
            {
                Transaction transaction = await helper.AddTransaction(todaysDateTime.AddHours(-1), merchantName, contract.contract, productname, "0000");
                todaysTransactions.Add(transaction);
            }
        }

        foreach (KeyValuePair<String, Int32> transactionCount in transactionCounts)
        {
            var contract = this.contractList.Single(s => s.operatorname == transactionCount.Key);
            var products = this.contractProducts.Single(p => p.Key == contract.contract);
            var productname = products.Value.First();
            for (int i = 0; i < transactionCount.Value; i++)
            {
                Transaction transaction = await helper.AddTransaction(comparisonDate.AddHours(-1), merchantName, contract.contract, productname, "0000");
                comparisonDateTransactions.Add(transaction);
            }
        }

        List<Int32> operatorFilterList = new List<Int32>{
                                                            2
                                                        };
        string serializedArray = string.Join(",", operatorFilterList);

        TodaysSales todaysSales = await this.CreateAndSendHttpRequestMessage<TodaysSales>($"api/facts/transactions/operators/performance?comparisonDate={comparisonDate.ToString("yyyy-MM-dd")}&operatorIds={serializedArray}",CancellationToken.None);
        todaysSales.ShouldNotBeNull();
        todaysSales.ComparisonSalesCount.ShouldBe(comparisonDateTransactions.Count(c => operatorFilterList.Contains(c.EstateOperatorReportingId)));
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(c => operatorFilterList.Contains(c.EstateOperatorReportingId)).Sum(c => c.TransactionAmount));

        todaysSales.TodaysSalesCount.ShouldBe(todaysTransactions.Count(c => operatorFilterList.Contains(c.EstateOperatorReportingId)));
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(c => operatorFilterList.Contains(c.EstateOperatorReportingId)).Sum(c => c.TransactionAmount));
    }

    [Fact]
    public async Task FactTransactionsControllerController_OperatorPerformance_MultipleOperators_SalesReturned()
    {
        var todaysTransactions = new List<Transaction>();
        var comparisonDateTransactions = new List<Transaction>();

        DateTime todaysDateTime = DateTime.Now;
        DateTime comparisonDate = DateTime.Now.AddDays(-1).AddHours(-1);

        Dictionary<String, Int32> transactionCounts = new(){
                                                               { "Safaricom", 25 }, // 5000
                                                               { "Voucher", 15 }, // 150 
                                                               { "PataPawa PostPay", 45 }, // 3375
                                                               { "PataPawa PrePay", 8 } // 600
                                                           };

        String merchantName = this.merchantsList.First();
        foreach (KeyValuePair<String, Int32> transactionCount in transactionCounts)
        {
            var contract = this.contractList.Single(s => s.operatorname == transactionCount.Key);
            var products = this.contractProducts.Single(p => p.Key == contract.contract);
            var productname = products.Value.First();
            for (int i = 0; i < transactionCount.Value; i++)
            {
                Transaction transaction = await helper.AddTransaction(todaysDateTime.AddHours(-1), merchantName, contract.contract, productname, "0000");
                todaysTransactions.Add(transaction);
            }
        }

        foreach (KeyValuePair<String, Int32> transactionCount in transactionCounts)
        {
            var contract = this.contractList.Single(s => s.operatorname == transactionCount.Key);
            var products = this.contractProducts.Single(p => p.Key == contract.contract);
            var productname = products.Value.First();
            for (int i = 0; i < transactionCount.Value; i++)
            {
                Transaction transaction = await helper.AddTransaction(comparisonDate.AddHours(-1), merchantName, contract.contract, productname, "0000");
                comparisonDateTransactions.Add(transaction);
            }
        }

        List<Int32> operatorFilterList = new List<Int32>{
                                                            2,3
                                                        };
        string serializedArray = string.Join(",", operatorFilterList);

        TodaysSales todaysSales = await this.CreateAndSendHttpRequestMessage<TodaysSales>($"api/facts/transactions/operators/performance?comparisonDate={comparisonDate.ToString("yyyy-MM-dd")}&operatorIds={serializedArray}", CancellationToken.None);
        todaysSales.ShouldNotBeNull();
        todaysSales.ComparisonSalesCount.ShouldBe(comparisonDateTransactions.Count(c => operatorFilterList.Contains(c.EstateOperatorReportingId)));
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(c => operatorFilterList.Contains(c.EstateOperatorReportingId)).Sum(c => c.TransactionAmount));

        todaysSales.TodaysSalesCount.ShouldBe(todaysTransactions.Count(c => operatorFilterList.Contains(c.EstateOperatorReportingId)));
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(c => operatorFilterList.Contains(c.EstateOperatorReportingId)).Sum(c => c.TransactionAmount));
    }

    [Fact]
    public async Task FactTransactionsControllerController_GetMerchantsTransactionKpis_SalesReturned(){
        DateTime todaysDateTime = DateTime.Now;

        await this.ClearStandingData();

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

        MerchantKpi? merchantKpi = await this.CreateAndSendHttpRequestMessage<MerchantKpi>($"api/facts/transactions/merchantkpis", CancellationToken.None);

        merchantKpi.ShouldNotBeNull();
        merchantKpi.MerchantsWithSaleInLastHour.ShouldBe(4);
        merchantKpi.MerchantsWithNoSaleToday.ShouldBe(6);
        merchantKpi.MerchantsWithNoSaleInLast7Days.ShouldBe(8);
    }
}

