namespace EstateReportingAPI.IntegrationTests
{
    using EstateManagement.Database.Contexts;
    using EstateManagement.Database.Entities;
    using EstateReportingAPI.DataTransferObjects;
    using Microsoft.EntityFrameworkCore;
    using Shouldly;
    using System.Diagnostics.Contracts;
    using Xunit;

    public class FactSettlementsControllerTests : ControllerTestsBase
    {
        protected Dictionary<(String Name,Guid contractId, String Description), List<String>> contractProducts;

        protected override async Task ClearStandingData()
        {
            await helper.DeleteAllContracts();
            await helper.DeleteAllMerchants();
        }

        protected override async Task SetupStandingData()
        {
            await helper.AddCalendarYear(2024);
     
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
                                            c.OperatorId,
                                            c.ContractId,
                                            c.Description,
                                            Products = productGroup.Select(p => new { p.ContractProductReportingId, p.ProductName })
                                                                                        .OrderBy(p => p.ContractProductReportingId)
                                                                                        .Select(p => p.ProductName)
                                                                                        .ToList()
                                        })
                             .ToList();

            var query2 = query1.Join(this.context.Operators,
                                     c => c.OperatorId,
                                     o => o.OperatorId,
                                     (c, o) => new{
                                                      o.Name,
                                                      c.ContractId,
                                                      c.Description,
                                                      c.Products
                                                  }).ToList();

            contractProducts = query2.ToDictionary(
                                                   item => (item.Name, item.ContractId, item.Description),
                                                   item => item.Products
                                                  );
        }
        
        [Fact]
        public async Task FactSettlementsController_TodaysSettlement_SettlementReturned()
        {
            int overallTodaysSettlementTransactionCount = 0;
            int overallTodaysPendingSettlementTransactionCount = 0;

            int overallComparisonSettlementTransactionCount = 0;
            int overallComparisonPendingSettlementTransactionCount = 0;
            List<(decimal settledTransactions, decimal pendingSettlementTransactions, decimal settlementFees, decimal pendingSettlementFees)> todayOverallTotals = new();
            List<(decimal settledTransactions, decimal pendingSettlementTransactions, decimal settlementFees, decimal pendingSettlementFees)> comparisonOverallTotals = new();

            DateTime todaysDate = DateTime.Now;
            DateTime comparisonDate = DateTime.Now.AddDays(-1);
            foreach (string merchant in merchantsList)
            {
                int todaysSettlementTransactionCount = 5;
                int todaysPendingSettlementTransactionCount = 9;
                (decimal settledTransactions, decimal pendingSettlementTransactions, decimal settlementFees, decimal pendingSettlementFees) todayTotals = await helper.AddSettlementRecord(merchant, "Safaricom", todaysDate, todaysSettlementTransactionCount, todaysPendingSettlementTransactionCount);
                todayOverallTotals.Add(todayTotals);

                overallTodaysSettlementTransactionCount += todaysSettlementTransactionCount; ;
                overallTodaysPendingSettlementTransactionCount += todaysPendingSettlementTransactionCount;

                int comparisonSettlementTransactionCount = 12;
                int comparisonPendingSettlementTransactionCount = 15;
                var comparisonTotals = await helper.AddSettlementRecord(merchant, "Safaricom",comparisonDate, comparisonSettlementTransactionCount, comparisonPendingSettlementTransactionCount);
                comparisonOverallTotals.Add(comparisonTotals);

                overallComparisonSettlementTransactionCount += comparisonSettlementTransactionCount;
                overallComparisonPendingSettlementTransactionCount += comparisonPendingSettlementTransactionCount;
            }

            await helper.RunTodaysTransactionsSummaryProcessing(comparisonDate.Date.AddDays(-1));
            await helper.RunHistoricTransactionsSummaryProcessing(comparisonDate.Date.AddDays(-1));
            await helper.RunTodaysTransactionsSummaryProcessing(todaysDate.Date.AddDays(-1));
            await helper.RunSettlementSummaryProcessing(comparisonDate.Date);

            var result = await ApiClient.GetTodaysSettlement(string.Empty, Guid.NewGuid(), 0, 0, DateTime.Now.AddDays(-1), CancellationToken.None);
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

        [Fact]
        public async Task FactSettlementsController_LastSettlement_SettlementReturned()
        {
            EstateManagementGenericContext context = new EstateManagementSqlServerContext(GetLocalConnectionString($"EstateReportingReadModel{TestId.ToString()}"));

            DatabaseHelper helper = new DatabaseHelper(context);

            List<(decimal settledTransactions, decimal pendingSettlementTransactions, decimal settlementFees, decimal
                pendingSettlementFees)> incompleteTotalsList = new();
            List<(decimal settledTransactions, decimal pendingSettlementTransactions, decimal settlementFees, decimal
                pendingSettlementFees)> completeTotalsList = new();
            
            // Add todays settlement (incomplete)
            Int32 totalSettledTransactionCount = 0;
            Int32 totalPendingSettlementTransactionCount = 21;
            foreach (string merchant in merchantsList)
            {
                Int32 settledTransactionCount = 0;
                totalSettledTransactionCount += settledTransactionCount;
                int pendingSettlementTransactionCount = 21;
                totalPendingSettlementTransactionCount += pendingSettlementTransactionCount;
                var incompleteTotals = await helper.AddSettlementRecord(merchant, "Safaricom", DateTime.Now, settledTransactionCount, pendingSettlementTransactionCount);
                incompleteTotalsList.Add(incompleteTotals);
            }

            // Add yesterdays settlement (complete)
            foreach (string merchant in merchantsList)
            {
                Int32 settledTransactionCount = 18;
                totalSettledTransactionCount += settledTransactionCount;
                int pendingSettlementTransactionCount = 0;
                totalPendingSettlementTransactionCount += pendingSettlementTransactionCount;
                var completeTotals = await helper.AddSettlementRecord(merchant, "Safaricom", DateTime.Now.AddDays(-1), settledTransactionCount, pendingSettlementTransactionCount);
                completeTotalsList.Add(completeTotals);
            }
            
            await helper.RunSettlementSummaryProcessing(DateTime.Now.AddDays(-1));

            var result = await ApiClient.GetLastSettlement(string.Empty, Guid.NewGuid(), CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            var lastSettlement = result.Data;

            lastSettlement.ShouldNotBeNull();
            lastSettlement.FeesValue.ShouldBe(completeTotalsList.Sum(t => t.settlementFees));
            lastSettlement.SalesCount.ShouldBe(totalSettledTransactionCount);
            lastSettlement.SalesValue.ShouldBe(completeTotalsList.Sum(c => c.settledTransactions));
        }

        [Fact]
        public async Task FactSettlementsController_LastSettlement_NoSettlementRecords_SettlementReturned()
        {
            EstateManagementGenericContext context = new EstateManagementSqlServerContext(GetLocalConnectionString($"EstateReportingReadModel{TestId.ToString()}"));

            DatabaseHelper helper = new DatabaseHelper(context);
            
            await helper.RunSettlementSummaryProcessing(DateTime.Now.AddDays(-1));

            var result = await ApiClient.GetLastSettlement(string.Empty, Guid.NewGuid(), CancellationToken.None);
            result.IsFailed.ShouldBeTrue();
        }


        [Fact(Skip = "To be fixed")]
        public async Task FactSettlementsController_UnsettledFees_ByOperator_SettlementReturned(){
            // Add some fees over a date range for multiple operators
            EstateManagementGenericContext context = new EstateManagementSqlServerContext(GetLocalConnectionString($"EstateReportingReadModel{TestId.ToString()}"));

            DatabaseHelper helper = new DatabaseHelper(context);

            List<DateTime> dates = new();
            dates.Add(new DateTime(2024, 5, 24));
            dates.Add(new DateTime(2024, 5, 25));
            dates.Add(new DateTime(2024, 5, 26));
            dates.Add(new DateTime(2024, 5, 27));

            
            foreach (DateTime dateTime in dates)
            {
                Guid settlementId = Guid.NewGuid();
                foreach (String merchant in this.merchantsList){
                    foreach ((String contract, String operatorname) contract in this.contractList){
                        var products = this.contractProducts.Single(cp => cp.Key.Description == contract.contract);

                        foreach (var product in products.Value){
                            await helper.AddMerchantSettlementFee(settlementId, dateTime, merchant, contract.contract, product, CancellationToken.None);
                        }
                    }
                }
            }

            DateTime startDate = dates.Min();
            DateTime endDate = dates.Max();

            var result = await ApiClient.GetUnsettledFees(string.Empty, Guid.NewGuid(), startDate, endDate, null, null, null, GroupByOption.Merchant, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            var unsettledFees = result.Data;

            unsettledFees.ShouldNotBeNull();
            unsettledFees.ShouldNotBeEmpty();
            unsettledFees.Count.ShouldBe(this.contractList.Count);
            foreach ((String contract, String operatorname) contract in this.contractList){
                var c = await this.context.Contracts.SingleOrDefaultAsync(c => c.Description == contract.contract, CancellationToken.None);
                var cps = await this.context.ContractProducts.Where(cp => cp.ContractId== c.ContractId).Select(cp => cp.ContractProductId).ToListAsync(CancellationToken.None);
                var tf = await context.ContractProductTransactionFees.Where(cptf => cps.Contains(cptf.ContractProductId)).Select(t => t.ContractProductTransactionFeeId).ToListAsync(CancellationToken.None);

                var expectedFees = this.context.MerchantSettlementFees.Where(f => tf.Contains(f.ContractProductTransactionFeeId));

                var u = unsettledFees.SingleOrDefault(u => u.DimensionName == contract.operatorname);

                u.ShouldNotBeNull();
                u.FeesCount.ShouldBe(await expectedFees.CountAsync(CancellationToken.None));
                u.FeesValue.ShouldBe(await expectedFees.SumAsync(f=> f.CalculatedValue,CancellationToken.None));
            }
        }

        [Fact(Skip = "To be fixed")]
        public async Task FactSettlementsController_UnsettledFees_ByMerchant_SettlementReturned()
        {
            // Add some fees over a date range for multiple operators
            EstateManagementGenericContext context = new EstateManagementSqlServerContext(GetLocalConnectionString($"EstateReportingReadModel{TestId.ToString()}"));

            DatabaseHelper helper = new DatabaseHelper(context);

            List<DateTime> dates = new();
            dates.Add(new DateTime(2024, 5, 24));
            dates.Add(new DateTime(2024, 5, 25));
            dates.Add(new DateTime(2024, 5, 26));
            dates.Add(new DateTime(2024, 5, 27));

            foreach (DateTime dateTime in dates)
            {
                Guid settlementId = Guid.NewGuid();
                foreach (String merchant in this.merchantsList)
                {
                    foreach ((String contract, String operatorname) contract in this.contractList)
                    {
                        var products = this.contractProducts.Single(cp => cp.Key.Description == contract.contract);

                        foreach (var product in products.Value)
                        {
                            await helper.AddMerchantSettlementFee(settlementId, dateTime, merchant, contract.contract, product, CancellationToken.None);
                        }
                    }
                }
            }

            DateTime startDate = dates.Min();
            DateTime endDate = dates.Max();

            var result = await ApiClient.GetUnsettledFees(string.Empty, Guid.NewGuid(), startDate, endDate, null, null, null, GroupByOption.Merchant, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            var unsettledFees = result.Data;
            unsettledFees.ShouldNotBeNull();
            unsettledFees.ShouldNotBeEmpty();
            unsettledFees.Count.ShouldBe(this.merchantsList.Count);
            foreach (var merchantName in this.merchantsList)
            {
                var merchant = await this.context.Merchants.SingleOrDefaultAsync(me => me.Name == merchantName);
                var expectedFees = this.context.MerchantSettlementFees.Where(f => f.MerchantId == merchant.MerchantId);
                
                var u = unsettledFees.SingleOrDefault(u => u.DimensionName == merchantName);

                u.ShouldNotBeNull();
                u.FeesCount.ShouldBe(await expectedFees.CountAsync(CancellationToken.None));
                u.FeesValue.ShouldBe(await expectedFees.SumAsync(f => f.CalculatedValue, CancellationToken.None));
            }
        }

        [Fact(Skip = "To be fixed")]
        public async Task FactSettlementsController_UnsettledFees_ByProduct_SettlementReturned()
        {
            // Add some fees over a date range for multiple operators
            EstateManagementGenericContext context = new EstateManagementSqlServerContext(GetLocalConnectionString($"EstateReportingReadModel{TestId.ToString()}"));

            DatabaseHelper helper = new DatabaseHelper(context);

            List<DateTime> dates = new();
            dates.Add(new DateTime(2024, 5, 24));
            dates.Add(new DateTime(2024, 5, 25));
            dates.Add(new DateTime(2024, 5, 26));
            dates.Add(new DateTime(2024, 5, 27));

            foreach (DateTime dateTime in dates)
            {
                Guid settlementId = Guid.NewGuid();
                foreach (String merchant in this.merchantsList)
                {
                    foreach ((String contract, String operatorname) contract in this.contractList)
                    {
                        var products = this.contractProducts.Single(cp => cp.Key.Description == contract.contract);

                        foreach (var product in products.Value)
                        {
                            await helper.AddMerchantSettlementFee(settlementId, dateTime, merchant, contract.contract, product, CancellationToken.None);
                        }
                    }
                }
            }

            DateTime startDate = dates.Min();
            DateTime endDate = dates.Max();

            var result = await ApiClient.GetUnsettledFees(string.Empty, Guid.NewGuid(), startDate, endDate, null, null, null, GroupByOption.Merchant, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            var unsettledFees = result.Data;

            unsettledFees.ShouldNotBeNull();
            unsettledFees.ShouldNotBeEmpty();
            
            List<(String,Guid,String)> allProducts = new();
            foreach (var contractProduct in this.contractProducts){
                foreach (String s in contractProduct.Value){
                    allProducts.Add((contractProduct.Key.Name, contractProduct.Key.contractId,s));
                }
            }
            unsettledFees.Count.ShouldBe(allProducts.Distinct().Count());
            foreach (var contractProduct in allProducts.Distinct())
            {
                var product = await this.context.ContractProducts.Where(cp => cp.ProductName == contractProduct.Item3 && cp.ContractId == contractProduct.Item2).SingleOrDefaultAsync(CancellationToken.None);
                var tf = await context.ContractProductTransactionFees.Where(cptf => cptf.ContractProductId == product.ContractProductId).ToListAsync(CancellationToken.None);
                var expectedFees = this.context.MerchantSettlementFees.Where(f => tf.Select(t => t.ContractProductTransactionFeeId).Contains(f.ContractProductTransactionFeeId));

                var u = unsettledFees.SingleOrDefault(u => u.DimensionName == $"{contractProduct.Item1} - {contractProduct.Item3}");

                u.ShouldNotBeNull($"{contractProduct.Item1} - {contractProduct.Item2}");
                u.FeesCount.ShouldBe(await expectedFees.CountAsync(CancellationToken.None));
                u.FeesValue.ShouldBe(await expectedFees.SumAsync(f => f.CalculatedValue, CancellationToken.None));
            }
        }
    }
}

