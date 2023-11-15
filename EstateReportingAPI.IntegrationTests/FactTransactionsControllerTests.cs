namespace EstateReportingAPI.IntegrationTests;

using System.Security.Policy;
using DataTransferObjects;
using EstateManagement.Database.Contexts;
using EstateManagement.Database.Entities;
using Newtonsoft.Json;
using Shared.IntegrationTesting;
using Shouldly;
using Xunit;

public class FactTransactionsControllerTests : ControllerTestsBase, IDisposable{
    [Fact]
    public async Task FactTransactionsControllerController_TodaysSales_SalesReturned(){
        EstateManagementGenericContext context = new EstateManagementSqlServerContext(ControllerTestsBase.GetLocalConnectionString($"EstateReportingReadModel{this.TestId.ToString()}"));
        var todaysTransactions = new List<Transaction>();
        var comparisonDateTransactions = new List<Transaction>();
        DatabaseHelper helper = new DatabaseHelper(context);
        // TODO: make counts dynamic
        DateTime todaysDateTime = DateTime.Now;

        for (int i = 0; i < 25; i++){
            Decimal amount = 100 + i;
            Transaction transaction = await helper.AddTransaction(todaysDateTime.AddHours(-1), 1, "Safaricom", 1, "0000", amount );
            todaysTransactions.Add(transaction);
        }

        DateTime comparisonDate = DateTime.Now.AddDays(-1).AddHours(-1);
        for (int i = 0; i < 21; i++)
        {
            Decimal amount = 100 + i;
            Transaction transaction= await helper.AddTransaction(comparisonDate, 1, "Safaricom", 1, "0000", amount);
            comparisonDateTransactions.Add(transaction);
        }

        HttpResponseMessage response = await this.CreateAndSendHttpRequestMessage($"api/facts/transactions/todayssales?comparisonDate={comparisonDate.ToString("yyyy-MM-dd")}");

        response.IsSuccessStatusCode.ShouldBeTrue();
        String content = await response.Content.ReadAsStringAsync(CancellationToken.None);
        TodaysSales? todaysSales = JsonConvert.DeserializeObject<TodaysSales>(content);
        todaysSales.ComparisonSalesCount.ShouldBe(comparisonDateTransactions.Count);
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Sum(c => c.TransactionAmount));

        todaysSales.TodaysSalesCount.ShouldBe(todaysTransactions.Count);
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Sum(c => c.TransactionAmount));
    }

    [Fact]
    public async Task FactTransactionsControllerController_TodaysSalesCountByHour_SalesReturned()
    {
        EstateManagementGenericContext context = new EstateManagementSqlServerContext(ControllerTestsBase.GetLocalConnectionString($"EstateReportingReadModel{this.TestId.ToString()}"));
        var todaysTransactions = new List<Transaction>();
        var comparisonDateTransactions = new List<Transaction>();
        DatabaseHelper helper = new DatabaseHelper(context);
        // TODO: make counts dynamic
        DateTime todaysDateTime = DateTime.Now;

        for (int hour = 0; hour < 24; hour++){
            List<Transaction> localList = new List<Transaction>();
            DateTime date = new DateTime(todaysDateTime.Year, todaysDateTime.Month, todaysDateTime.Day, hour, 0, 0);
            for (int i = 0; i < 25; i++){
                Decimal amount = 100 + i;
                
                Transaction transaction = await helper.AddTransaction(date, 1, "Safaricom", 1, "0000", amount);
                localList.Add(transaction);
            }
            todaysTransactions.AddRange(localList);
        }

        DateTime comparisonDate = todaysDateTime.AddDays(-1);
        for (int hour = 0; hour < 24; hour++){
            List<Transaction> localList = new List<Transaction>();
            DateTime date = new DateTime(comparisonDate.Year, comparisonDate.Month, comparisonDate.Day, hour, 0, 0);
            for (int i = 0; i < 21; i++){
                Decimal amount = 100 + i;
                    
                Transaction transaction = await helper.AddTransaction(date, 1, "Safaricom", 1, "0000", amount);
                localList.Add(transaction);
            }

            comparisonDateTransactions.AddRange(localList);
        }

        HttpResponseMessage response = await this.CreateAndSendHttpRequestMessage($"api/facts/transactions/todayssales/countbyhour?comparisonDate={comparisonDate.ToString("yyyy-MM-dd")}");

        String content = await response.Content.ReadAsStringAsync(CancellationToken.None);

        List<TodaysSalesCountByHour>? todaysSalesCountByHour = JsonConvert.DeserializeObject<List<TodaysSalesCountByHour>>(content);
        
        foreach (TodaysSalesCountByHour salesCountByHour in todaysSalesCountByHour){
            var todayHour = todaysTransactions.Where(t => t.TransactionDateTime.Hour == salesCountByHour.Hour);
            var comparisonHour = comparisonDateTransactions.Where(t => t.TransactionDateTime.Hour == salesCountByHour.Hour);
            salesCountByHour.ComparisonSalesCount.ShouldBe(comparisonHour.Count());
            salesCountByHour.TodaysSalesCount.ShouldBe(todayHour.Count());
        }
    }

    [Fact]
    public async Task FactTransactionsControllerController_TodaysSalesValueByHour_SalesReturned()
    {
        EstateManagementGenericContext context = new EstateManagementSqlServerContext(ControllerTestsBase.GetLocalConnectionString($"EstateReportingReadModel{this.TestId.ToString()}"));
        var todaysTransactions = new List<Transaction>();
        var comparisonDateTransactions = new List<Transaction>();
        DatabaseHelper helper = new DatabaseHelper(context);
        // TODO: make counts dynamic
        DateTime todaysDateTime = DateTime.Now;

        for (int hour = 0; hour < 24; hour++)
        {
            DateTime date = new DateTime(todaysDateTime.Year, todaysDateTime.Month, todaysDateTime.Day, hour, 0, 0);
            for (int i = 0; i < 25; i++)
            {
                Decimal amount = 100 + i;
                Transaction transaction = await helper.AddTransaction(date, 1, "Safaricom", 1, "0000", amount);
                todaysTransactions.Add(transaction);
            }
        }

        DateTime comparisonDate = todaysDateTime.AddDays(-1);
        for (int hour = 0; hour < 24; hour++)
        {
            DateTime date = new DateTime(comparisonDate.Year, comparisonDate.Month, comparisonDate.Day, hour, 0, 0);
            for (int i = 0; i < 21; i++)
            {
                Decimal amount = 100 + i;

                Transaction transaction = await helper.AddTransaction(date, 1, "Safaricom", 1, "0000", amount);
                comparisonDateTransactions.Add(transaction);
            }
        }

        HttpResponseMessage response = await this.CreateAndSendHttpRequestMessage($"api/facts/transactions/todayssales/valuebyhour?comparisonDate={comparisonDate.ToString("yyyy-MM-dd")}");

        String content = await response.Content.ReadAsStringAsync(CancellationToken.None);
        List<TodaysSalesValueByHour>? todaysSalesValueByHour = JsonConvert.DeserializeObject<List<TodaysSalesValueByHour>>(content);

        foreach (TodaysSalesValueByHour salesValueByHour in todaysSalesValueByHour)
        {
            var todayHour = todaysTransactions.Where(t => t.TransactionDateTime.Hour == salesValueByHour.Hour);
            var comparisonHour = comparisonDateTransactions.Where(t => t.TransactionDateTime.Hour == salesValueByHour.Hour);
            salesValueByHour.ComparisonSalesValue.ShouldBe(comparisonHour.Sum(c => c.TransactionAmount));
            salesValueByHour.TodaysSalesValue.ShouldBe(todayHour.Sum(c => c.TransactionAmount));
        }
    }

    [Fact]
    public async Task FactTransactionsControllerController_TodaysFailedSales_SalesReturned()
    {
        EstateManagementGenericContext context = new EstateManagementSqlServerContext(ControllerTestsBase.GetLocalConnectionString($"EstateReportingReadModel{this.TestId.ToString()}"));
        var todaysTransactions = new List<Transaction>();
        var comparisonDateTransactions = new List<Transaction>();
        DatabaseHelper helper = new DatabaseHelper(context);
        // TODO: make counts dynamic
        DateTime todaysDateTime = DateTime.Now;

        for (int i = 0; i < 25; i++)
        {
            Decimal amount = 100 + i;
            Transaction transaction = await helper.AddTransaction(todaysDateTime.AddHours(-1), 1, "Safaricom", 1, "1009", amount);
            todaysTransactions.Add(transaction);
        }

        DateTime comparisonDate = DateTime.Now.AddDays(-1).AddHours(-1);
        for (int i = 0; i < 21; i++)
        {
            Decimal amount = 100 + i;
            Transaction transaction = await helper.AddTransaction(comparisonDate, 1, "Safaricom", 1, "1009", amount);
            comparisonDateTransactions.Add(transaction);
        }

        HttpResponseMessage response = await this.CreateAndSendHttpRequestMessage($"api/facts/transactions/todaysfailedsales?responseCode=1009&comparisonDate={comparisonDate.ToString("yyyy-MM-dd")}");

        String content = await response.Content.ReadAsStringAsync(CancellationToken.None);
        TodaysSales? todaysSales = JsonConvert.DeserializeObject<TodaysSales>(content);
        todaysSales.ComparisonSalesCount.ShouldBe(comparisonDateTransactions.Count);
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Sum(c => c.TransactionAmount));

        todaysSales.TodaysSalesCount.ShouldBe(todaysTransactions.Count);
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Sum(c => c.TransactionAmount));
    }

    [Fact]
    public async Task FactTransactionsControllerController_GetMerchantsTransactionKpis_SalesReturned()
    {
        EstateManagementGenericContext context = new EstateManagementSqlServerContext(ControllerTestsBase.GetLocalConnectionString($"EstateReportingReadModel{this.TestId.ToString()}"));
            
        DatabaseHelper helper = new DatabaseHelper(context);
            
        DateTime todaysDateTime = DateTime.Now;

        // Last Hour
        await helper.AddMerchant(1, "Merchant 1", todaysDateTime.AddMinutes(-10));
        await helper.AddMerchant(1, "Merchant 2", todaysDateTime.AddMinutes(-10));
        await helper.AddMerchant(1, "Merchant 3", todaysDateTime.AddMinutes(-10));
        await helper.AddMerchant(1, "Merchant 4", todaysDateTime.AddMinutes(-10));

        // Yesterday
        await helper.AddMerchant(1, "Merchant 5", todaysDateTime.AddDays(-1));
        await helper.AddMerchant(1, "Merchant 6", todaysDateTime.AddDays(-1));
        await helper.AddMerchant(1, "Merchant 7", todaysDateTime.AddDays(-1));
        await helper.AddMerchant(1, "Merchant 8", todaysDateTime.AddDays(-1));
        await helper.AddMerchant(1, "Merchant 9", todaysDateTime.AddDays(-1));
        await helper.AddMerchant(1, "Merchant 10", todaysDateTime.AddDays(-1));

        // 10 Days Ago
        await helper.AddMerchant(1, "Merchant 11", todaysDateTime.AddDays(-10));
        await helper.AddMerchant(1, "Merchant 12", todaysDateTime.AddDays(-10));
        await helper.AddMerchant(1, "Merchant 13", todaysDateTime.AddDays(-10));
        await helper.AddMerchant(1, "Merchant 14", todaysDateTime.AddDays(-10));
        await helper.AddMerchant(1, "Merchant 15", todaysDateTime.AddDays(-10));
        await helper.AddMerchant(1, "Merchant 16", todaysDateTime.AddDays(-10));
        await helper.AddMerchant(1, "Merchant 17", todaysDateTime.AddDays(-10));
        await helper.AddMerchant(1, "Merchant 18", todaysDateTime.AddDays(-10));

        HttpResponseMessage response = await this.CreateAndSendHttpRequestMessage($"api/facts/transactions/merchantkpis");

        String content = await response.Content.ReadAsStringAsync(CancellationToken.None);
        MerchantKpi? merchantKpi = JsonConvert.DeserializeObject<MerchantKpi>(content);
        merchantKpi.MerchantsWithSaleInLastHour.ShouldBe(4);
        merchantKpi.MerchantsWithNoSaleToday.ShouldBe(6);
        merchantKpi.MerchantsWithNoSaleInLast7Days.ShouldBe(8);
    }

    [Fact]
    public async Task FactTransactionsController_GetTopBottomProductsByValue_BottomProducts_ProductsReturned(){
        EstateManagementGenericContext context = new EstateManagementSqlServerContext(ControllerTestsBase.GetLocalConnectionString($"EstateReportingReadModel{this.TestId.ToString()}"));

        DatabaseHelper helper = new DatabaseHelper(context);
        List<Transaction> product1Transactions = new List<Transaction>();
        List<Transaction> product2Transactions = new List<Transaction>();
        List<Transaction> product3Transactions = new List<Transaction>();
        List<Transaction> product4Transactions = new List<Transaction>();

        DateTime todaysDateTime = DateTime.Now.AddHours(-1);
        // Product 1
        for (int i = 0; i < 25; i++){
            Decimal amount = 100 + i;
            Transaction transaction = await helper.AddTransaction(todaysDateTime, 1, "Safaricom", 1, "0000", amount);
            product1Transactions.Add(transaction);
        }

        // Product 2
        for (int i = 0; i < 15; i++)
        {
            Decimal amount = 100 + i;
            Transaction transaction = await helper.AddTransaction(todaysDateTime, 1, "Safaricom", 2, "0000", amount);
            product2Transactions.Add(transaction);
        }

        // Product 3
        for (int i = 0; i < 45; i++)
        {
            Decimal amount = 100 + i;
            Transaction transaction = await helper.AddTransaction(todaysDateTime, 1, "Safaricom", 3, "0000", amount);
            product3Transactions.Add(transaction);
        }

        // Product 4
        for (int i = 0; i < 8; i++)
        {
            Decimal amount = 100 + i;
            Transaction transaction = await helper.AddTransaction(todaysDateTime, 1, "Safaricom", 4, "0000", amount);
            product4Transactions.Add(transaction);
        }

        await helper.AddContractProduct("Product 1");
        await helper.AddContractProduct("Product 2");
        await helper.AddContractProduct("Product 3");
        await helper.AddContractProduct("Product 4");

        HttpResponseMessage response = await this.CreateAndSendHttpRequestMessage($"api/facts/transactions/products/topbottombyvalue?count=3&topOrBottom=bottom");

        String content = await response.Content.ReadAsStringAsync(CancellationToken.None);
        List<TopBottomProductData>? topBottomProductData = JsonConvert.DeserializeObject<List<TopBottomProductData>>(content);

        topBottomProductData[0].ProductName.ShouldBe("Product 4");
        topBottomProductData[0].SalesValue.ShouldBe(product4Transactions.Sum(p => p.TransactionAmount));
        topBottomProductData[1].ProductName.ShouldBe("Product 2");
        topBottomProductData[1].SalesValue.ShouldBe(product2Transactions.Sum(p => p.TransactionAmount));
        topBottomProductData[2].ProductName.ShouldBe("Product 1");
        topBottomProductData[2].SalesValue.ShouldBe(product1Transactions.Sum(p => p.TransactionAmount));
    }

    [Fact]
    public async Task FactTransactionsController_GetTopBottomProductsByValue_Top_ProductsReturned()
    {
        EstateManagementGenericContext context = new EstateManagementSqlServerContext(ControllerTestsBase.GetLocalConnectionString($"EstateReportingReadModel{this.TestId.ToString()}"));

        DatabaseHelper helper = new DatabaseHelper(context);
        List<Transaction> product1Transactions = new List<Transaction>();
        List<Transaction> product2Transactions = new List<Transaction>();
        List<Transaction> product3Transactions = new List<Transaction>();
        List<Transaction> product4Transactions = new List<Transaction>();

        DateTime todaysDateTime = DateTime.Now.AddHours(-1);
        // Product 1
        for (int i = 0; i < 25; i++)
        {
            Decimal amount = 100 + i;
            Transaction transaction = await helper.AddTransaction(todaysDateTime, 1, "Safaricom", 1, "0000", amount);
            product1Transactions.Add(transaction);
        }

        // Product 2
        for (int i = 0; i < 15; i++)
        {
            Decimal amount = 100 + i;
            Transaction transaction = await helper.AddTransaction(todaysDateTime, 1, "Safaricom", 2, "0000", amount);
            product2Transactions.Add(transaction);
        }

        // Product 3
        for (int i = 0; i < 45; i++)
        {
            Decimal amount = 100 + i;
            Transaction transaction = await helper.AddTransaction(todaysDateTime, 1, "Safaricom", 3, "0000", amount);
            product3Transactions.Add(transaction);
        }

        for (int i = 0; i < 8; i++)
        {
            Decimal amount = 100 + i;
            Transaction transaction = await helper.AddTransaction(todaysDateTime, 1, "Safaricom", 4, "0000", amount);
            product4Transactions.Add(transaction);
        }

        await helper.AddContractProduct("Product 1");
        await helper.AddContractProduct("Product 2");
        await helper.AddContractProduct("Product 3");
        await helper.AddContractProduct("Product 4");

        HttpResponseMessage response = await this.CreateAndSendHttpRequestMessage($"api/facts/transactions/products/topbottombyvalue?count=3&topOrBottom=top");

        String content = await response.Content.ReadAsStringAsync(CancellationToken.None);
        List<TopBottomProductData>? topBottomProductData = JsonConvert.DeserializeObject<List<TopBottomProductData>>(content);

        topBottomProductData[0].ProductName.ShouldBe("Product 3");
        topBottomProductData[0].SalesValue.ShouldBe(product3Transactions.Sum(p => p.TransactionAmount));
        topBottomProductData[1].ProductName.ShouldBe("Product 1");
        topBottomProductData[1].SalesValue.ShouldBe(product1Transactions.Sum(p => p.TransactionAmount));
        topBottomProductData[2].ProductName.ShouldBe("Product 2");
        topBottomProductData[2].SalesValue.ShouldBe(product2Transactions.Sum(p => p.TransactionAmount));
    }

    [Fact]
    public async Task FactTransactionsController_GetTopBottomOperatorsByValue_BottomOperators_OperatorsReturned()
    {
        EstateManagementGenericContext context = new EstateManagementSqlServerContext(ControllerTestsBase.GetLocalConnectionString($"EstateReportingReadModel{this.TestId.ToString()}"));

        DatabaseHelper helper = new DatabaseHelper(context);
        List<Transaction> operator1Transactions = new List<Transaction>();
        List<Transaction> operator2Transactions = new List<Transaction>();
        List<Transaction> operator3Transactions = new List<Transaction>();
        List<Transaction> operator4Transactions = new List<Transaction>();

        DateTime todaysDateTime = DateTime.Now.AddHours(-1);
        // Operator 1
        for (int i = 0; i < 25; i++)
        {
            Decimal amount = 100 + i;
            Transaction transaction = await helper.AddTransaction(todaysDateTime, 1, "Operator 1", 1, "0000", amount);
            operator1Transactions.Add(transaction);
        }

        // Operator 2
        for (int i = 0; i < 15; i++)
        {
            Decimal amount = 100 + i;
            Transaction transaction = await helper.AddTransaction(todaysDateTime, 1, "Operator 2", 2, "0000", amount);
            operator2Transactions.Add(transaction);
        }

        // Operator 3
        for (int i = 0; i < 45; i++)
        {
            Decimal amount = 100 + i;
            Transaction transaction = await helper.AddTransaction(todaysDateTime, 1, "Operator 3", 3, "0000", amount);
            operator3Transactions.Add(transaction);
        }

        // Operator 4
        for (int i = 0; i < 8; i++)
        {
            Decimal amount = 100 + i;
            Transaction transaction = await helper.AddTransaction(todaysDateTime, 1, "Operator 4", 4, "0000", amount);
            operator4Transactions.Add(transaction);
        }

        await helper.AddEstateOperator("Operator 1");
        await helper.AddEstateOperator("Operator 2");
        await helper.AddEstateOperator("Operator 3");
        await helper.AddEstateOperator("Operator 4");

        HttpResponseMessage response = await this.CreateAndSendHttpRequestMessage($"api/facts/transactions/operators/topbottombyvalue?count=3&topOrBottom=bottom");

        String content = await response.Content.ReadAsStringAsync(CancellationToken.None);
        List<TopBottomOperatorData>? topBottomOperatorData = JsonConvert.DeserializeObject<List<TopBottomOperatorData>>(content);

        topBottomOperatorData[0].OperatorName.ShouldBe("Operator 4");
        topBottomOperatorData[0].SalesValue.ShouldBe(operator4Transactions.Sum(p => p.TransactionAmount));
        topBottomOperatorData[1].OperatorName.ShouldBe("Operator 2");
        topBottomOperatorData[1].SalesValue.ShouldBe(operator2Transactions.Sum(p => p.TransactionAmount));
        topBottomOperatorData[2].OperatorName.ShouldBe("Operator 1");
        topBottomOperatorData[2].SalesValue.ShouldBe(operator1Transactions.Sum(p => p.TransactionAmount));
    }

    [Fact]
    public async Task FactTransactionsController_GetTopBottomOperatorsByValue_TopOperators_OperatorsReturned()
    {
        EstateManagementGenericContext context = new EstateManagementSqlServerContext(ControllerTestsBase.GetLocalConnectionString($"EstateReportingReadModel{this.TestId.ToString()}"));

        DatabaseHelper helper = new DatabaseHelper(context);
        List<Transaction> operator1Transactions = new List<Transaction>();
        List<Transaction> operator2Transactions = new List<Transaction>();
        List<Transaction> operator3Transactions = new List<Transaction>();
        List<Transaction> operator4Transactions = new List<Transaction>();

        DateTime todaysDateTime = DateTime.Now.AddHours(-1);
        // Operator 1
        for (int i = 0; i < 25; i++)
        {
            Decimal amount = 100 + i;
            Transaction transaction = await helper.AddTransaction(todaysDateTime, 1, "Operator 1", 1, "0000", amount);
            operator1Transactions.Add(transaction);
        }

        // Operator 2
        for (int i = 0; i < 15; i++)
        {
            Decimal amount = 100 + i;
            Transaction transaction = await helper.AddTransaction(todaysDateTime, 1, "Operator 2", 2, "0000", amount);
            operator2Transactions.Add(transaction);
        }

        // Operator 3
        for (int i = 0; i < 45; i++)
        {
            Decimal amount = 100 + i;
            Transaction transaction = await helper.AddTransaction(todaysDateTime, 1, "Operator 3", 3, "0000", amount);
            operator3Transactions.Add(transaction);
        }

        // Operator 4
        for (int i = 0; i < 8; i++)
        {
            Decimal amount = 100 + i;
            Transaction transaction = await helper.AddTransaction(todaysDateTime, 1, "Operator 4", 4, "0000", amount);
            operator4Transactions.Add(transaction);
        }

        await helper.AddEstateOperator("Operator 1");
        await helper.AddEstateOperator("Operator 2");
        await helper.AddEstateOperator("Operator 3");
        await helper.AddEstateOperator("Operator 4");

        HttpResponseMessage response = await this.CreateAndSendHttpRequestMessage($"api/facts/transactions/operators/topbottombyvalue?count=3&topOrBottom=top");

        String content = await response.Content.ReadAsStringAsync(CancellationToken.None);
        List<TopBottomOperatorData>? topBottomOperatorData = JsonConvert.DeserializeObject<List<TopBottomOperatorData>>(content);

        topBottomOperatorData[0].OperatorName.ShouldBe("Operator 3");
        topBottomOperatorData[0].SalesValue.ShouldBe(operator3Transactions.Sum(p => p.TransactionAmount));
        topBottomOperatorData[1].OperatorName.ShouldBe("Operator 1");
        topBottomOperatorData[1].SalesValue.ShouldBe(operator1Transactions.Sum(p => p.TransactionAmount));
        topBottomOperatorData[2].OperatorName.ShouldBe("Operator 2");
        topBottomOperatorData[2].SalesValue.ShouldBe(operator2Transactions.Sum(p => p.TransactionAmount));
    }

    [Fact]
    public async Task FactTransactionsController_GetTopBottoMerchantsByValue_BottomMerchants_MerchantsReturned()
    {
        EstateManagementGenericContext context = new EstateManagementSqlServerContext(ControllerTestsBase.GetLocalConnectionString($"EstateReportingReadModel{this.TestId.ToString()}"));

        DatabaseHelper helper = new DatabaseHelper(context);
        List<Transaction> merchant1Transactions = new List<Transaction>();
        List<Transaction> merchant2Transactions = new List<Transaction>();
        List<Transaction> merchant3Transactions = new List<Transaction>();
        List<Transaction> merchant4Transactions = new List<Transaction>();

        DateTime todaysDateTime = DateTime.Now.AddHours(-1);
        // Merchants 1
        for (int i = 0; i < 25; i++)
        {
            Decimal amount = 100 + i;
            Transaction transaction = await helper.AddTransaction(todaysDateTime, 1, "Safaricom", 1, "0000", amount);
            merchant1Transactions.Add(transaction);
        }

        // Merchants 2
        for (int i = 0; i < 15; i++)
        {
            Decimal amount = 100 + i;
            Transaction transaction = await helper.AddTransaction(todaysDateTime, 2, "Safaricom", 2, "0000", amount);
            merchant2Transactions.Add(transaction);
        }

        // Merchants 3
        for (int i = 0; i < 45; i++)
        {
            Decimal amount = 100 + i;
            Transaction transaction = await helper.AddTransaction(todaysDateTime, 3, "Safaricom", 3, "0000", amount);
            merchant3Transactions.Add(transaction);
        }

        // Merchants 4
        for (int i = 0; i < 8; i++)
        {
            Decimal amount = 100 + i;
            Transaction transaction = await helper.AddTransaction(todaysDateTime, 4, "Safaricom", 4, "0000", amount);
            merchant4Transactions.Add(transaction);
        }

        await helper.AddMerchant(1, "Merchant 1", DateTime.Now);
        await helper.AddMerchant(1, "Merchant 2", DateTime.Now);
        await helper.AddMerchant(1, "Merchant 3", DateTime.Now);
        await helper.AddMerchant(1, "Merchant 4", DateTime.Now);

        HttpResponseMessage response = await this.CreateAndSendHttpRequestMessage($"api/facts/transactions/merchants/topbottombyvalue?count=3&topOrBottom=bottom");

        String content = await response.Content.ReadAsStringAsync(CancellationToken.None);
        List<TopBottomMerchantData>? topBottomMerchantData = JsonConvert.DeserializeObject<List<TopBottomMerchantData>>(content);

        topBottomMerchantData[0].MerchantName.ShouldBe("Merchant 4");
        topBottomMerchantData[0].SalesValue.ShouldBe(merchant4Transactions.Sum(p => p.TransactionAmount));
        topBottomMerchantData[1].MerchantName.ShouldBe("Merchant 2");
        topBottomMerchantData[1].SalesValue.ShouldBe(merchant2Transactions.Sum(p => p.TransactionAmount));
        topBottomMerchantData[2].MerchantName.ShouldBe("Merchant 1");
        topBottomMerchantData[2].SalesValue.ShouldBe(merchant1Transactions.Sum(p => p.TransactionAmount));
    }

    [Fact]
    public async Task FactTransactionsController_GetTopBottoMerchantsByValue_TopMerchants_MerchantsReturned()
    {
        EstateManagementGenericContext context = new EstateManagementSqlServerContext(ControllerTestsBase.GetLocalConnectionString($"EstateReportingReadModel{this.TestId.ToString()}"));

        DatabaseHelper helper = new DatabaseHelper(context);
        List<Transaction> merchant1Transactions = new List<Transaction>();
        List<Transaction> merchant2Transactions = new List<Transaction>();
        List<Transaction> merchant3Transactions = new List<Transaction>();
        List<Transaction> merchant4Transactions = new List<Transaction>();

        DateTime todaysDateTime = DateTime.Now.AddHours(-1);
        // Merchants 1
        for (int i = 0; i < 25; i++)
        {
            Decimal amount = 100 + i;
            Transaction transaction = await helper.AddTransaction(todaysDateTime, 1, "Safaricom", 1, "0000", amount);
            merchant1Transactions.Add(transaction);
        }

        // Merchants 2
        for (int i = 0; i < 15; i++)
        {
            Decimal amount = 100 + i;
            Transaction transaction = await helper.AddTransaction(todaysDateTime, 2, "Safaricom", 2, "0000", amount);
            merchant2Transactions.Add(transaction);
        }

        // Merchants 3
        for (int i = 0; i < 45; i++)
        {
            Decimal amount = 100 + i;
            Transaction transaction = await helper.AddTransaction(todaysDateTime, 3, "Safaricom", 3, "0000", amount);
            merchant3Transactions.Add(transaction);
        }

        // Merchants 4
        for (int i = 0; i < 8; i++)
        {
            Decimal amount = 100 + i;
            Transaction transaction = await helper.AddTransaction(todaysDateTime, 4, "Safaricom", 4, "0000", amount);
            merchant4Transactions.Add(transaction);
        }

        await helper.AddMerchant(1, "Merchant 1", DateTime.Now);
        await helper.AddMerchant(1, "Merchant 2", DateTime.Now);
        await helper.AddMerchant(1, "Merchant 3", DateTime.Now);
        await helper.AddMerchant(1, "Merchant 4", DateTime.Now);

        HttpResponseMessage response = await this.CreateAndSendHttpRequestMessage($"api/facts/transactions/merchants/topbottombyvalue?count=3&topOrBottom=top");

        String content = await response.Content.ReadAsStringAsync(CancellationToken.None);
        List<TopBottomMerchantData>? topBottomMerchantData = JsonConvert.DeserializeObject<List<TopBottomMerchantData>>(content);

        topBottomMerchantData[0].MerchantName.ShouldBe("Merchant 3");
        topBottomMerchantData[0].SalesValue.ShouldBe(merchant3Transactions.Sum(p => p.TransactionAmount));
        topBottomMerchantData[1].MerchantName.ShouldBe("Merchant 1");
        topBottomMerchantData[1].SalesValue.ShouldBe(merchant1Transactions.Sum(p => p.TransactionAmount));
        topBottomMerchantData[2].MerchantName.ShouldBe("Merchant 2");
        topBottomMerchantData[2].SalesValue.ShouldBe(merchant2Transactions.Sum(p => p.TransactionAmount));
    }

    [Fact]
    public async Task FactTransactionsControllerController_MerchantPerformance_AllMerchants_SalesReturned()
    {
        EstateManagementGenericContext context = new EstateManagementSqlServerContext(ControllerTestsBase.GetLocalConnectionString($"EstateReportingReadModel{this.TestId.ToString()}"));
        var todaysTransactions = new List<Transaction>();
        var comparisonDateTransactions = new List<Transaction>();
        DatabaseHelper helper = new DatabaseHelper(context);
        // TODO: make counts dynamic
        DateTime todaysDateTime = DateTime.Now;
        DateTime comparisonDate = DateTime.Now.AddDays(-1).AddHours(-1);

        List<Int32> merchantIds = new List<Int32>{
                                                     1,
                                                     2,
                                                     3
                                                 };

        foreach (Int32 merchantId in merchantIds)
        {

            for (int i = 0; i < 25; i++)
            {
                Decimal amount = 100 + i;
                Transaction transaction = await helper.AddTransaction(todaysDateTime.AddHours(-1), merchantId, "Safaricom", 1, "0000", amount);
                todaysTransactions.Add(transaction);
            }

            for (int i = 0; i < 21; i++)
            {
                Decimal amount = 100 + i;
                Transaction transaction = await helper.AddTransaction(comparisonDate, merchantId, "Safaricom", 1, "0000", amount);
                comparisonDateTransactions.Add(transaction);
            }
        }

        HttpResponseMessage response = await this.CreateAndSendHttpRequestMessage($"api/facts/transactions/merchants/performance?comparisonDate={comparisonDate.ToString("yyyy-MM-dd")}");

        response.IsSuccessStatusCode.ShouldBeTrue();
        String content = await response.Content.ReadAsStringAsync(CancellationToken.None);
        TodaysSales? todaysSales = JsonConvert.DeserializeObject<TodaysSales>(content);
        todaysSales.ComparisonSalesCount.ShouldBe(comparisonDateTransactions.Count);
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Sum(c => c.TransactionAmount));

        todaysSales.TodaysSalesCount.ShouldBe(todaysTransactions.Count);
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Sum(c => c.TransactionAmount));
    }

    [Fact]
    public async Task FactTransactionsControllerController_MerchantPerformance_SingleMerchant_SalesReturned()
    {
        EstateManagementGenericContext context = new EstateManagementSqlServerContext(ControllerTestsBase.GetLocalConnectionString($"EstateReportingReadModel{this.TestId.ToString()}"));
        var todaysTransactions = new List<Transaction>();
        var comparisonDateTransactions = new List<Transaction>();
        DatabaseHelper helper = new DatabaseHelper(context);
        // TODO: make counts dynamic
        DateTime todaysDateTime = DateTime.Now;
        DateTime comparisonDate = DateTime.Now.AddDays(-1).AddHours(-1);

        List<Int32> merchantIds = new List<Int32>{
                                                     1,
                                                     2,
                                                     3
                                                 };

        foreach (Int32 merchantId in merchantIds){

            for (int i = 0; i < 25; i++){
                Decimal amount = 100 + i;
                Transaction transaction = await helper.AddTransaction(todaysDateTime.AddHours(-1), merchantId, "Safaricom", 1, "0000", amount);
                todaysTransactions.Add(transaction);
            }

            for (int i = 0; i < 21; i++){
                Decimal amount = 100 + i;
                Transaction transaction = await helper.AddTransaction(comparisonDate, merchantId, "Safaricom", 1, "0000", amount);
                comparisonDateTransactions.Add(transaction);
            }
        }
        List<Int32> merchantFilterList = new List<Int32>{
                                                            2
                                                        };
        string serializedArray = string.Join(",", merchantFilterList);

        HttpResponseMessage response = await this.CreateAndSendHttpRequestMessage($"api/facts/transactions/merchants/performance?comparisonDate={comparisonDate.ToString("yyyy-MM-dd")}&merchantIds={serializedArray}");

        response.IsSuccessStatusCode.ShouldBeTrue();
        String content = await response.Content.ReadAsStringAsync(CancellationToken.None);
        TodaysSales? todaysSales = JsonConvert.DeserializeObject<TodaysSales>(content);
        todaysSales.ComparisonSalesCount.ShouldBe(comparisonDateTransactions.Count(c => merchantFilterList.Contains(c.MerchantReportingId)));
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(c => merchantFilterList.Contains(c.MerchantReportingId)).Sum(c => c.TransactionAmount));

        todaysSales.TodaysSalesCount.ShouldBe(todaysTransactions.Count(c => merchantFilterList.Contains(c.MerchantReportingId)));
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(c => merchantFilterList.Contains(c.MerchantReportingId)).Sum(c => c.TransactionAmount));
    }

    [Fact]
    public async Task FactTransactionsControllerController_MerchantPerformance_MultipleMerchants_SalesReturned()
    {
        EstateManagementGenericContext context = new EstateManagementSqlServerContext(ControllerTestsBase.GetLocalConnectionString($"EstateReportingReadModel{this.TestId.ToString()}"));
        var todaysTransactions = new List<Transaction>();
        var comparisonDateTransactions = new List<Transaction>();
        DatabaseHelper helper = new DatabaseHelper(context);
        // TODO: make counts dynamic
        DateTime todaysDateTime = DateTime.Now;
        DateTime comparisonDate = DateTime.Now.AddDays(-1).AddHours(-1);

        List<Int32> merchantIds = new List<Int32>{
                                                     1,
                                                     2,
                                                     3
                                                 };

        foreach (Int32 merchantId in merchantIds)
        {

            for (int i = 0; i < 25; i++)
            {
                Decimal amount = 100 + i;
                Transaction transaction = await helper.AddTransaction(todaysDateTime.AddHours(-1), merchantId, "Safaricom", 1, "0000", amount);
                todaysTransactions.Add(transaction);
            }

            for (int i = 0; i < 21; i++)
            {
                Decimal amount = 100 + i;
                Transaction transaction = await helper.AddTransaction(comparisonDate, merchantId, "Safaricom", 1, "0000", amount);
                comparisonDateTransactions.Add(transaction);
            }
        }
        List<Int32> merchantFilterList = new List<Int32>{
                                                            2,3
                                                        };
        string serializedArray = string.Join(",", merchantFilterList);

        HttpResponseMessage response = await this.CreateAndSendHttpRequestMessage($"api/facts/transactions/merchants/performance?comparisonDate={comparisonDate.ToString("yyyy-MM-dd")}&merchantIds={serializedArray}");

        response.IsSuccessStatusCode.ShouldBeTrue();
        String content = await response.Content.ReadAsStringAsync(CancellationToken.None);
        TodaysSales? todaysSales = JsonConvert.DeserializeObject<TodaysSales>(content);
        todaysSales.ComparisonSalesCount.ShouldBe(comparisonDateTransactions.Count(c => merchantFilterList.Contains(c.MerchantReportingId)));
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(c => merchantFilterList.Contains(c.MerchantReportingId)).Sum(c => c.TransactionAmount));

        todaysSales.TodaysSalesCount.ShouldBe(todaysTransactions.Count(c => merchantFilterList.Contains(c.MerchantReportingId)));
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(c => merchantFilterList.Contains(c.MerchantReportingId)).Sum(c => c.TransactionAmount));
    }


    [Fact]
    public async Task FactTransactionsControllerController_ProductPerformance_AllProducts_SalesReturned()
    {
        EstateManagementGenericContext context = new EstateManagementSqlServerContext(ControllerTestsBase.GetLocalConnectionString($"EstateReportingReadModel{this.TestId.ToString()}"));
        var todaysTransactions = new List<Transaction>();
        var comparisonDateTransactions = new List<Transaction>();
        DatabaseHelper helper = new DatabaseHelper(context);
        // TODO: make counts dynamic
        DateTime todaysDateTime = DateTime.Now;
        DateTime comparisonDate = DateTime.Now.AddDays(-1).AddHours(-1);

        List<Int32> productIds = new List<Int32>{
                                                    1,
                                                    2,
                                                    3
                                                };

        foreach (Int32 productId in productIds)
        {

            for (int i = 0; i < 25; i++)
            {
                Decimal amount = 100 + i;
                Transaction transaction = await helper.AddTransaction(todaysDateTime.AddHours(-1), 1, "Safaricom", productId, "0000", amount);
                todaysTransactions.Add(transaction);
            }

            for (int i = 0; i < 21; i++)
            {
                Decimal amount = 100 + i;
                Transaction transaction = await helper.AddTransaction(comparisonDate, 1, "Safaricom", productId, "0000", amount);
                comparisonDateTransactions.Add(transaction);
            }
        }

        HttpResponseMessage response = await this.CreateAndSendHttpRequestMessage($"api/facts/transactions/products/performance?comparisonDate={comparisonDate.ToString("yyyy-MM-dd")}");

        response.IsSuccessStatusCode.ShouldBeTrue();
        String content = await response.Content.ReadAsStringAsync(CancellationToken.None);
        TodaysSales? todaysSales = JsonConvert.DeserializeObject<TodaysSales>(content);
        todaysSales.ComparisonSalesCount.ShouldBe(comparisonDateTransactions.Count);
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Sum(c => c.TransactionAmount));

        todaysSales.TodaysSalesCount.ShouldBe(todaysTransactions.Count);
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Sum(c => c.TransactionAmount));
    }

    [Fact]
    public async Task FactTransactionsControllerController_ProductPerformance_SingleProduct_SalesReturned()
    {
        EstateManagementGenericContext context = new EstateManagementSqlServerContext(ControllerTestsBase.GetLocalConnectionString($"EstateReportingReadModel{this.TestId.ToString()}"));
        var todaysTransactions = new List<Transaction>();
        var comparisonDateTransactions = new List<Transaction>();
        DatabaseHelper helper = new DatabaseHelper(context);
        // TODO: make counts dynamic
        DateTime todaysDateTime = DateTime.Now;
        DateTime comparisonDate = DateTime.Now.AddDays(-1).AddHours(-1);

        List<Int32> productIds = new List<Int32>{
                                                     1,
                                                     2,
                                                     3
                                                 };

        foreach (Int32 productId in productIds)
        {

            for (int i = 0; i < 25; i++)
            {
                Decimal amount = 100 + i;
                Transaction transaction = await helper.AddTransaction(todaysDateTime.AddHours(-1), 1, "Safaricom", productId, "0000", amount);
                todaysTransactions.Add(transaction);
            }

            for (int i = 0; i < 21; i++)
            {
                Decimal amount = 100 + i;
                Transaction transaction = await helper.AddTransaction(comparisonDate, 1, "Safaricom", productId, "0000", amount);
                comparisonDateTransactions.Add(transaction);
            }
        }
        List<Int32> productFilterList = new List<Int32>{
                                                            2
                                                        };
        string serializedArray = string.Join(",", productFilterList);

        HttpResponseMessage response = await this.CreateAndSendHttpRequestMessage($"api/facts/transactions/products/performance?comparisonDate={comparisonDate.ToString("yyyy-MM-dd")}&productIds={serializedArray}");

        response.IsSuccessStatusCode.ShouldBeTrue();
        String content = await response.Content.ReadAsStringAsync(CancellationToken.None);
        TodaysSales? todaysSales = JsonConvert.DeserializeObject<TodaysSales>(content);
        todaysSales.ComparisonSalesCount.ShouldBe(comparisonDateTransactions.Count(c => productFilterList.Contains(c.ContractProductReportingId)));
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(c => productFilterList.Contains(c.ContractProductReportingId)).Sum(c => c.TransactionAmount));

        todaysSales.TodaysSalesCount.ShouldBe(todaysTransactions.Count(c => productFilterList.Contains(c.ContractProductReportingId)));
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(c => productFilterList.Contains(c.ContractProductReportingId)).Sum(c => c.TransactionAmount));
    }

    [Fact]
    public async Task FactTransactionsControllerController_ProductPerformance_MultipleProducts_SalesReturned()
    {
        EstateManagementGenericContext context = new EstateManagementSqlServerContext(ControllerTestsBase.GetLocalConnectionString($"EstateReportingReadModel{this.TestId.ToString()}"));
        var todaysTransactions = new List<Transaction>();
        var comparisonDateTransactions = new List<Transaction>();
        DatabaseHelper helper = new DatabaseHelper(context);
        // TODO: make counts dynamic
        DateTime todaysDateTime = DateTime.Now;
        DateTime comparisonDate = DateTime.Now.AddDays(-1).AddHours(-1);

        List<Int32> productIds = new List<Int32>{
                                                    1,
                                                    2,
                                                    3
                                                };

        foreach (Int32 productId in productIds)
        {

            for (int i = 0; i < 25; i++)
            {
                Decimal amount = 100 + i;
                Transaction transaction = await helper.AddTransaction(todaysDateTime.AddHours(-1), 1, "Safaricom", productId, "0000", amount);
                todaysTransactions.Add(transaction);
            }

            for (int i = 0; i < 21; i++)
            {
                Decimal amount = 100 + i;
                Transaction transaction = await helper.AddTransaction(comparisonDate, 1, "Safaricom", productId, "0000", amount);
                comparisonDateTransactions.Add(transaction);
            }
        }
        List<Int32> productFilterList = new List<Int32>{
                                                           2,3
                                                       };
        string serializedArray = string.Join(",", productFilterList);

        HttpResponseMessage response = await this.CreateAndSendHttpRequestMessage($"api/facts/transactions/products/performance?comparisonDate={comparisonDate.ToString("yyyy-MM-dd")}&productIds={serializedArray}");

        response.IsSuccessStatusCode.ShouldBeTrue();
        String content = await response.Content.ReadAsStringAsync(CancellationToken.None);
        TodaysSales? todaysSales = JsonConvert.DeserializeObject<TodaysSales>(content);
        todaysSales.ComparisonSalesCount.ShouldBe(comparisonDateTransactions.Count(c => productFilterList.Contains(c.ContractProductReportingId)));
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(c => productFilterList.Contains(c.ContractProductReportingId)).Sum(c => c.TransactionAmount));

        todaysSales.TodaysSalesCount.ShouldBe(todaysTransactions.Count(c => productFilterList.Contains(c.ContractProductReportingId)));
        todaysSales.ComparisonSalesValue.ShouldBe(comparisonDateTransactions.Where(c => productFilterList.Contains(c.ContractProductReportingId)).Sum(c => c.TransactionAmount));
    }


}
