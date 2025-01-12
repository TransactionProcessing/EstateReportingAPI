using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EstateReportingAPI.IntegrationTests {
    using EstateManagement.Database.Contexts;
    using EstateManagement.Database.Entities;
    using EstateReportingAPI.DataTransferObjects;
    using Microsoft.EntityFrameworkCore;
    using Shouldly;
    using System.Diagnostics.Contracts;
    using Xunit;
    using System.Linq;

    public class FactSettlementsControllerTests : ControllerTestsBase {

        protected override async Task ClearStandingData() {
            await helper.DeleteAllContracts();
            await helper.DeleteAllMerchants();
        }

        protected override async Task SetupStandingData() {
            await helper.AddCalendarYear(2024);
            await helper.AddCalendarYear(2025);

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
            List<(string productName, int productType, decimal? value)> safaricomProductList = new() { ("200 KES Topup", 0, 200.00m), ("100 KES Topup", 0, 100.00m), ("50 KES Topup", 0, 50.00m), ("Custom", 0, null) };
            await helper.AddContractWithProducts("Test Estate", "Safaricom Contract", "Safaricom", safaricomProductList);

            List<(string productName, int productType, decimal? value)> voucherProductList = new() { ("10 KES Voucher", 0, 10.00m), ("Custom", 0, null) };
            await helper.AddContractWithProducts("Test Estate", "Healthcare Centre 1 Contract", "Voucher", voucherProductList);

            List<(string productName, int productType, decimal? value)> postPayProductList = new() { ("Post Pay Bill Pay", 0, null) };
            await helper.AddContractWithProducts("Test Estate", "PataPawa PostPay Contract", "PataPawa PostPay", postPayProductList);

            List<(string productName, int productType, decimal? value)> prePayProductList = new() { ("Pre Pay Bill Pay", 0, null) };
            await helper.AddContractWithProducts("Test Estate", "PataPawa PrePay Contract", "PataPawa PrePay", prePayProductList);

            // Response Codes
            await helper.AddResponseCode(0, "Success");
            await helper.AddResponseCode(1000, "Unknown Device");
            await helper.AddResponseCode(1001, "Unknown Estate");
            await helper.AddResponseCode(1002, "Unknown Merchant");
            await helper.AddResponseCode(1003, "No Devices Configured");

            merchantsList = context.Merchants.Select(m => m).ToList();

            contractList = context.Contracts.Join(context.Operators, c => c.OperatorId, o => o.OperatorId, (c,
                                                                                                            o) => new { c.ContractId, c.Description, o.OperatorId, o.Name }).ToList().Select(x => (x.ContractId, x.Description, x.OperatorId, x.Name)).ToList();

            var query1 = context.Contracts.GroupJoin(context.ContractProducts, c => c.ContractId, cp => cp.ContractId, (c,
                                                                                                                        productGroup) => new { c.ContractId, Products = productGroup.Select(p => new { p.ContractProductId, p.ProductName, p.Value }).OrderBy(p => p.ContractProductId).Select(p => new { p.ContractProductId, p.ProductName, p.Value }).ToList() }).ToList();

            contractProducts = query1.ToDictionary(item => item.ContractId, item => item.Products.Select(i => (i.ContractProductId, i.ProductName, i.Value)).ToList());
        }

        [Fact]
        public async Task FactSettlementsController_TodaysSettlement_SettlementReturned() {
            int overallTodaysSettlementTransactionCount = 0;
            int overallTodaysPendingSettlementTransactionCount = 0;

            int overallComparisonSettlementTransactionCount = 0;
            int overallComparisonPendingSettlementTransactionCount = 0;
            List<(decimal settledTransactions, decimal pendingSettlementTransactions, decimal settlementFees, decimal pendingSettlementFees)> todayOverallTotals = new();
            List<(decimal settledTransactions, decimal pendingSettlementTransactions, decimal settlementFees, decimal pendingSettlementFees)> comparisonOverallTotals = new();

            DateTime todaysDate = DateTime.Now;
            DateTime comparisonDate = DateTime.Now.AddDays(-1);
            foreach (var merchant in merchantsList) {
                int todaysSettlementTransactionCount = 5;
                int todaysPendingSettlementTransactionCount = 9;
                var contract = this.contractList.Single(c => c.operatorName == "Safaricom");
                (decimal settledTransactions, decimal pendingSettlementTransactions, decimal settlementFees, decimal pendingSettlementFees) todayTotals = await helper.AddSettlementRecord(merchant.EstateId, merchant.MerchantId, contract.operatorId, todaysDate, todaysSettlementTransactionCount, todaysPendingSettlementTransactionCount);
                todayOverallTotals.Add(todayTotals);

                overallTodaysSettlementTransactionCount += todaysSettlementTransactionCount;
                ;
                overallTodaysPendingSettlementTransactionCount += todaysPendingSettlementTransactionCount;

                int comparisonSettlementTransactionCount = 12;
                int comparisonPendingSettlementTransactionCount = 15;
                var comparisonTotals = await helper.AddSettlementRecord(merchant.EstateId, merchant.MerchantId, contract.operatorId, comparisonDate, comparisonSettlementTransactionCount, comparisonPendingSettlementTransactionCount);
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
        public async Task FactSettlementsController_TodaysSettlement_MerchantFilter_SettlementReturned()
        {
            int overallTodaysSettlementTransactionCount = 0;
            int overallTodaysPendingSettlementTransactionCount = 0;

            int overallComparisonSettlementTransactionCount = 0;
            int overallComparisonPendingSettlementTransactionCount = 0;
            List<(decimal settledTransactions, decimal pendingSettlementTransactions, decimal settlementFees, decimal pendingSettlementFees)> todayOverallTotals = new();
            List<(decimal settledTransactions, decimal pendingSettlementTransactions, decimal settlementFees, decimal pendingSettlementFees)> comparisonOverallTotals = new();

            DateTime todaysDate = DateTime.Now;
            DateTime comparisonDate = DateTime.Now.AddDays(-1);
            Dictionary<Int32, Decimal> merchantTodaysSettlementFees = new();
            Dictionary<Int32, Decimal> merchantTodaysPendingSettlementFees = new();
            Dictionary<Int32, Decimal> merchantComparisonSettlementFees = new();
            Dictionary<Int32, Decimal> merchantComparisonPendingSettlementFees = new();

            Dictionary<Int32, Int32> merchantTodaysSettlementTransactionCount = new();
            Dictionary<Int32, Int32> merchantTodaysPendingSettlementTransactionCount = new();
            Dictionary<Int32, Int32> merchantComparisonSettlementTransactionCount = new();
            Dictionary<Int32, Int32> merchantComparisonPendingSettlementTransactionCount = new();

                foreach (var merchant in merchantsList)
                {
                    merchantTodaysSettlementTransactionCount.Add(merchant.MerchantReportingId, 0);
                    merchantTodaysPendingSettlementTransactionCount.Add(merchant.MerchantReportingId, 0);
                    merchantComparisonSettlementTransactionCount.Add(merchant.MerchantReportingId, 0);
                    merchantComparisonPendingSettlementTransactionCount.Add(merchant.MerchantReportingId, 0);

                    merchantTodaysSettlementFees.Add(merchant.MerchantReportingId, 0);
                    merchantTodaysPendingSettlementFees.Add(merchant.MerchantReportingId, 0);
                    merchantComparisonSettlementFees.Add(merchant.MerchantReportingId, 0);
                    merchantComparisonPendingSettlementFees.Add(merchant.MerchantReportingId, 0);

                    int todaysSettlementTransactionCount = 5;
                    int todaysPendingSettlementTransactionCount = 9;
                    var contract = this.contractList.Single(c => c.operatorName == "Safaricom");
                    (decimal settledTransactions, decimal pendingSettlementTransactions, decimal settlementFees, decimal pendingSettlementFees) todayTotals = await helper.AddSettlementRecord(merchant.EstateId, merchant.MerchantId, contract.operatorId, todaysDate, todaysSettlementTransactionCount, todaysPendingSettlementTransactionCount);
                    todayOverallTotals.Add(todayTotals);

                    overallTodaysSettlementTransactionCount += todaysSettlementTransactionCount;
                    overallTodaysPendingSettlementTransactionCount += todaysPendingSettlementTransactionCount;

                    merchantTodaysSettlementTransactionCount[merchant.MerchantReportingId] = todaysSettlementTransactionCount;
                    merchantTodaysPendingSettlementTransactionCount[merchant.MerchantReportingId] = todaysPendingSettlementTransactionCount;

                    merchantTodaysSettlementFees[merchant.MerchantReportingId] = todayTotals.settlementFees;
                    merchantTodaysPendingSettlementFees[merchant.MerchantReportingId] = todayTotals.pendingSettlementFees;

                    int comparisonSettlementTransactionCount = 12;
                    int comparisonPendingSettlementTransactionCount = 15;
                    var comparisonTotals = await helper.AddSettlementRecord(merchant.EstateId, merchant.MerchantId, contract.operatorId, comparisonDate, comparisonSettlementTransactionCount, comparisonPendingSettlementTransactionCount);
                    comparisonOverallTotals.Add(comparisonTotals);

                    overallComparisonSettlementTransactionCount += comparisonSettlementTransactionCount;
                    overallComparisonPendingSettlementTransactionCount += comparisonPendingSettlementTransactionCount;

                    merchantComparisonSettlementTransactionCount[merchant.MerchantReportingId] = comparisonSettlementTransactionCount;
                    merchantComparisonPendingSettlementTransactionCount[merchant.MerchantReportingId] = comparisonPendingSettlementTransactionCount;

                    merchantComparisonSettlementFees[merchant.MerchantReportingId] = comparisonTotals.settlementFees;
                    merchantComparisonPendingSettlementFees[merchant.MerchantReportingId] = comparisonTotals.pendingSettlementFees;
                }
            
                await helper.RunTodaysTransactionsSummaryProcessing(comparisonDate.Date.AddDays(-1));
            await helper.RunHistoricTransactionsSummaryProcessing(comparisonDate.Date.AddDays(-1));
            await helper.RunTodaysTransactionsSummaryProcessing(todaysDate.Date.AddDays(-1));
            await helper.RunSettlementSummaryProcessing(comparisonDate.Date);

            var result = await ApiClient.GetTodaysSettlement(string.Empty, Guid.NewGuid(), 1, 0, DateTime.Now.AddDays(-1), CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            var todaysSettlement = result.Data;
            todaysSettlement.ShouldNotBeNull();
            todaysSettlement.ComparisonSettlementCount.ShouldBe(merchantComparisonSettlementTransactionCount[1]);
            todaysSettlement.ComparisonSettlementValue.ShouldBe(merchantComparisonSettlementFees[1]);
            todaysSettlement.ComparisonPendingSettlementCount.ShouldBe(merchantComparisonPendingSettlementTransactionCount[1]);
            todaysSettlement.ComparisonPendingSettlementValue.ShouldBe(merchantComparisonPendingSettlementFees[1]);

            todaysSettlement.TodaysSettlementCount.ShouldBe(merchantTodaysSettlementTransactionCount[1]);
            todaysSettlement.TodaysSettlementValue.ShouldBe(merchantTodaysSettlementFees[1]);
            todaysSettlement.TodaysPendingSettlementCount.ShouldBe(merchantTodaysPendingSettlementTransactionCount[1]);
            todaysSettlement.TodaysPendingSettlementValue.ShouldBe(merchantTodaysPendingSettlementFees[1]);
        }

        [Fact]
        public async Task FactSettlementsController_TodaysSettlement_OperatorFilter_SettlementReturned()
        {
            int overallTodaysSettlementTransactionCount = 0;
            int overallTodaysPendingSettlementTransactionCount = 0;

            int overallComparisonSettlementTransactionCount = 0;
            int overallComparisonPendingSettlementTransactionCount = 0;
            List<(decimal settledTransactions, decimal pendingSettlementTransactions, decimal settlementFees, decimal pendingSettlementFees)> todayOverallTotals = new();
            List<(decimal settledTransactions, decimal pendingSettlementTransactions, decimal settlementFees, decimal pendingSettlementFees)> comparisonOverallTotals = new();

            DateTime todaysDate = DateTime.Now;
            DateTime comparisonDate = DateTime.Now.AddDays(-1);
            Dictionary<Int32, Decimal> operatorTodaysSettlementFees = new();
            Dictionary<Int32, Decimal> operatorTodaysPendingSettlementFees = new();
            Dictionary<Int32, Decimal> operatorComparisonSettlementFees = new();
            Dictionary<Int32, Decimal> operatorComparisonPendingSettlementFees = new();

            Dictionary<Int32, Int32> operatorTodaysSettlementTransactionCount = new();
            Dictionary<Int32, Int32> operatorTodaysPendingSettlementTransactionCount = new();
            Dictionary<Int32, Int32> operatorComparisonSettlementTransactionCount = new();
            Dictionary<Int32, Int32> operatorComparisonPendingSettlementTransactionCount = new();

            foreach (var @operator in this.contractList.Select(c => c.operatorName)) {
                var operatorRecord = this.context.Operators.Single(o => o.Name == @operator);
                operatorTodaysSettlementTransactionCount.Add(operatorRecord.OperatorReportingId, 0);
                operatorTodaysPendingSettlementTransactionCount.Add(operatorRecord.OperatorReportingId, 0);
                operatorComparisonSettlementTransactionCount.Add(operatorRecord.OperatorReportingId, 0);
                operatorComparisonPendingSettlementTransactionCount.Add(operatorRecord.OperatorReportingId, 0);

                operatorTodaysSettlementFees.Add(operatorRecord.OperatorReportingId, 0);
                operatorTodaysPendingSettlementFees.Add(operatorRecord.OperatorReportingId, 0);
                operatorComparisonSettlementFees.Add(operatorRecord.OperatorReportingId, 0);
                operatorComparisonPendingSettlementFees.Add(operatorRecord.OperatorReportingId, 0);
                foreach (var merchant in merchantsList) {
                    
                    int todaysSettlementTransactionCount = 5;
                    int todaysPendingSettlementTransactionCount = 9;
                    var contract = this.contractList.Single(c => c.operatorName == @operator);
                    (decimal settledTransactions, decimal pendingSettlementTransactions, decimal settlementFees, decimal pendingSettlementFees) todayTotals = await helper.AddSettlementRecord(merchant.EstateId, merchant.MerchantId, contract.operatorId, todaysDate, todaysSettlementTransactionCount, todaysPendingSettlementTransactionCount);
                    todayOverallTotals.Add(todayTotals);

                    overallTodaysSettlementTransactionCount += todaysSettlementTransactionCount;
                    overallTodaysPendingSettlementTransactionCount += todaysPendingSettlementTransactionCount;

                    operatorTodaysSettlementTransactionCount[operatorRecord.OperatorReportingId] += todaysSettlementTransactionCount;
                    operatorTodaysPendingSettlementTransactionCount[operatorRecord.OperatorReportingId] += todaysPendingSettlementTransactionCount;

                    operatorTodaysSettlementFees[operatorRecord.OperatorReportingId] += todayTotals.settlementFees;
                    operatorTodaysPendingSettlementFees[operatorRecord.OperatorReportingId] += todayTotals.pendingSettlementFees;

                    int comparisonSettlementTransactionCount = 12;
                    int comparisonPendingSettlementTransactionCount = 15;
                    var comparisonTotals = await helper.AddSettlementRecord(merchant.EstateId, merchant.MerchantId, contract.operatorId, comparisonDate, comparisonSettlementTransactionCount, comparisonPendingSettlementTransactionCount);
                    comparisonOverallTotals.Add(comparisonTotals);

                    overallComparisonSettlementTransactionCount += comparisonSettlementTransactionCount;
                    overallComparisonPendingSettlementTransactionCount += comparisonPendingSettlementTransactionCount;

                    operatorComparisonSettlementTransactionCount[operatorRecord.OperatorReportingId] += comparisonSettlementTransactionCount;
                    operatorComparisonPendingSettlementTransactionCount[operatorRecord.OperatorReportingId] += comparisonPendingSettlementTransactionCount;

                    operatorComparisonSettlementFees[operatorRecord.OperatorReportingId] += comparisonTotals.settlementFees;
                    operatorComparisonPendingSettlementFees[operatorRecord.OperatorReportingId] += comparisonTotals.pendingSettlementFees;
                }

            }

            await helper.RunTodaysTransactionsSummaryProcessing(comparisonDate.Date.AddDays(-1));
            await helper.RunHistoricTransactionsSummaryProcessing(comparisonDate.Date.AddDays(-1));
            await helper.RunTodaysTransactionsSummaryProcessing(todaysDate.Date.AddDays(-1));
            await helper.RunSettlementSummaryProcessing(comparisonDate.Date);

            var result = await ApiClient.GetTodaysSettlement(string.Empty, Guid.NewGuid(), 0, 1, DateTime.Now.AddDays(-1), CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            var todaysSettlement = result.Data;
            todaysSettlement.ShouldNotBeNull();
            todaysSettlement.ComparisonSettlementCount.ShouldBe(operatorComparisonSettlementTransactionCount[1]);
            todaysSettlement.ComparisonSettlementValue.ShouldBe(operatorComparisonSettlementFees[1]);
            todaysSettlement.ComparisonPendingSettlementCount.ShouldBe(operatorComparisonPendingSettlementTransactionCount[1]);
            todaysSettlement.ComparisonPendingSettlementValue.ShouldBe(operatorComparisonPendingSettlementFees[1]);

            todaysSettlement.TodaysSettlementCount.ShouldBe(operatorTodaysSettlementTransactionCount[1]);
            todaysSettlement.TodaysSettlementValue.ShouldBe(operatorTodaysSettlementFees[1]);
            todaysSettlement.TodaysPendingSettlementCount.ShouldBe(operatorTodaysPendingSettlementTransactionCount[1]);
            todaysSettlement.TodaysPendingSettlementValue.ShouldBe(operatorTodaysPendingSettlementFees[1]);
        }

        [Fact]
        public async Task FactSettlementsController_LastSettlement_SettlementReturned() {
            EstateManagementGenericContext context = new EstateManagementSqlServerContext(GetLocalConnectionString($"EstateReportingReadModel{TestId.ToString()}"));

            DatabaseHelper helper = new DatabaseHelper(context);

            List<(decimal settledTransactions, decimal pendingSettlementTransactions, decimal settlementFees, decimal pendingSettlementFees)> incompleteTotalsList = new();
            List<(decimal settledTransactions, decimal pendingSettlementTransactions, decimal settlementFees, decimal pendingSettlementFees)> completeTotalsList = new();

            // Add todays settlement (incomplete)
            Int32 totalSettledTransactionCount = 0;
            Int32 totalPendingSettlementTransactionCount = 21;
            var contract = this.contractList.Single(c => c.operatorName == "Safaricom");
            foreach (Merchant merchant in merchantsList) {
                Int32 settledTransactionCount = 0;
                totalSettledTransactionCount += settledTransactionCount;
                int pendingSettlementTransactionCount = 21;
                totalPendingSettlementTransactionCount += pendingSettlementTransactionCount;
                var incompleteTotals = await helper.AddSettlementRecord(merchant.EstateId, merchant.MerchantId, contract.operatorId, DateTime.Now, settledTransactionCount, pendingSettlementTransactionCount);
                incompleteTotalsList.Add(incompleteTotals);
            }

            // Add yesterdays settlement (complete)
            foreach (Merchant merchant in merchantsList) {
                Int32 settledTransactionCount = 18;
                totalSettledTransactionCount += settledTransactionCount;
                int pendingSettlementTransactionCount = 0;
                totalPendingSettlementTransactionCount += pendingSettlementTransactionCount;
                var completeTotals = await helper.AddSettlementRecord(merchant.EstateId, merchant.MerchantId, contract.operatorId, DateTime.Now.AddDays(-1), settledTransactionCount, pendingSettlementTransactionCount);
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
        public async Task FactSettlementsController_LastSettlement_NoSettlementRecords_SettlementReturned() {
            EstateManagementGenericContext context = new EstateManagementSqlServerContext(GetLocalConnectionString($"EstateReportingReadModel{TestId.ToString()}"));

            DatabaseHelper helper = new DatabaseHelper(context);

            await helper.RunSettlementSummaryProcessing(DateTime.Now.AddDays(-1));

            var result = await ApiClient.GetLastSettlement(string.Empty, Guid.NewGuid(), CancellationToken.None);
            result.IsFailed.ShouldBeTrue();
        }


        [Fact]
        public async Task FactSettlementsController_UnsettledFees_ByOperator_SettlementReturned() {
            // Add some fees over a date range for multiple operators
            EstateManagementGenericContext context = new EstateManagementSqlServerContext(GetLocalConnectionString($"EstateReportingReadModel{TestId.ToString()}"));

            DatabaseHelper helper = new DatabaseHelper(context);

            List<DateTime> dates = new();
            dates.Add(new DateTime(2024, 5, 24));
            dates.Add(new DateTime(2024, 5, 25));
            dates.Add(new DateTime(2024, 5, 26));
            dates.Add(new DateTime(2024, 5, 27));


            foreach (DateTime dateTime in dates) {
                Guid settlementId = Guid.NewGuid();
                foreach (Merchant merchant in this.merchantsList) {
                    foreach ((Guid contractId, String contractName, Guid operatorId, String operatorName) contract in this.contractList) {
                        var products = this.contractProducts.Single(cp => cp.Key == contract.contractId);

                        foreach (var product in products.Value) {
                            await helper.AddMerchantSettlementFee(settlementId, dateTime, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, CancellationToken.None);
                        }
                    }
                }
            }

            DateTime startDate = dates.Min();
            DateTime endDate = dates.Max();

            var result = await ApiClient.GetUnsettledFees(string.Empty, Guid.NewGuid(), startDate, endDate, null, null, null, GroupByOption.Operator, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            var unsettledFees = result.Data;

            unsettledFees.ShouldNotBeNull();
            unsettledFees.ShouldNotBeEmpty();
            unsettledFees.Count.ShouldBe(this.contractList.Count);
            foreach ((Guid contractId, String contractName, Guid operatorId, String operatorName) contract in this.contractList) {
                var c = await this.context.Contracts.SingleOrDefaultAsync(c => c.ContractId == contract.contractId, CancellationToken.None);
                var cps = await this.context.ContractProducts.Where(cp => cp.ContractId == c.ContractId).Select(cp => cp.ContractProductId).ToListAsync(CancellationToken.None);
                var tf = await context.ContractProductTransactionFees.Where(cptf => cps.Contains(cptf.ContractProductId)).Select(t => t.ContractProductTransactionFeeId).ToListAsync(CancellationToken.None);

                var expectedFees = this.context.MerchantSettlementFees.Where(f => tf.Contains(f.ContractProductTransactionFeeId));

                var u = unsettledFees.SingleOrDefault(u => u.DimensionName == contract.operatorName);

                u.ShouldNotBeNull();
                u.FeesCount.ShouldBe(await expectedFees.CountAsync(CancellationToken.None));
                u.FeesValue.ShouldBe(await expectedFees.SumAsync(f => f.CalculatedValue, CancellationToken.None));
            }
        }

        [Fact]
        public async Task FactSettlementsController_UnsettledFees_ByOperator_OperatorFilter_SettlementReturned() {
            // Add some fees over a date range for multiple operators
            EstateManagementGenericContext context = new EstateManagementSqlServerContext(GetLocalConnectionString($"EstateReportingReadModel{TestId.ToString()}"));

            DatabaseHelper helper = new DatabaseHelper(context);

            List<DateTime> dates = new();
            dates.Add(new DateTime(2024, 5, 24));
            dates.Add(new DateTime(2024, 5, 25));
            dates.Add(new DateTime(2024, 5, 26));
            dates.Add(new DateTime(2024, 5, 27));


            foreach (DateTime dateTime in dates) {
                Guid settlementId = Guid.NewGuid();
                foreach (Merchant merchant in this.merchantsList) {
                    foreach ((Guid contractId, String contractName, Guid operatorId, String operatorName) contract in this.contractList) {
                        var products = this.contractProducts.Single(cp => cp.Key == contract.contractId);

                        foreach (var product in products.Value) {
                            await helper.AddMerchantSettlementFee(settlementId, dateTime, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, CancellationToken.None);
                        }
                    }
                }
            }

            DateTime startDate = dates.Min();
            DateTime endDate = dates.Max();

            var result = await ApiClient.GetUnsettledFees(string.Empty, Guid.NewGuid(), startDate, endDate, null, [1], null, GroupByOption.Operator, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            var unsettledFees = result.Data;

            unsettledFees.ShouldNotBeNull();
            unsettledFees.ShouldNotBeEmpty();

            var @operator = await this.context.Operators.SingleOrDefaultAsync(o => o.OperatorReportingId == 1, CancellationToken.None);
            var c = await this.context.Contracts.SingleOrDefaultAsync(c => c.OperatorId == @operator.OperatorId, CancellationToken.None);
            var cps = await this.context.ContractProducts.Where(cp => cp.ContractId == c.ContractId).Select(cp => cp.ContractProductId).ToListAsync(CancellationToken.None);
            var tf = await context.ContractProductTransactionFees.Where(cptf => cps.Contains(cptf.ContractProductId)).Select(t => t.ContractProductTransactionFeeId).ToListAsync(CancellationToken.None);

            var expectedFees = this.context.MerchantSettlementFees.Where(f => tf.Contains(f.ContractProductTransactionFeeId));

            var u = unsettledFees.SingleOrDefault(u => u.DimensionName == @operator.Name);

            unsettledFees.Sum(d => d.FeesCount).ShouldBe(await expectedFees.CountAsync(CancellationToken.None));
            unsettledFees.Sum(d => d.FeesValue).ShouldBe(await expectedFees.SumAsync(f => f.CalculatedValue, CancellationToken.None));

        }

        [Fact]
        public async Task FactSettlementsController_UnsettledFees_ByMerchant_SettlementReturned() {
            // Add some fees over a date range for multiple operators
            EstateManagementGenericContext context = new EstateManagementSqlServerContext(GetLocalConnectionString($"EstateReportingReadModel{TestId.ToString()}"));

            DatabaseHelper helper = new DatabaseHelper(context);

            List<DateTime> dates = new();
            dates.Add(new DateTime(2024, 5, 24));
            dates.Add(new DateTime(2024, 5, 25));
            dates.Add(new DateTime(2024, 5, 26));
            dates.Add(new DateTime(2024, 5, 27));

            foreach (DateTime dateTime in dates) {
                Guid settlementId = Guid.NewGuid();
                foreach (Merchant merchant in this.merchantsList) {
                    foreach ((Guid contractId, String contractName, Guid operatorId, String operatorName) contract in this.contractList) {
                        var products = this.contractProducts.Single(cp => cp.Key == contract.contractId);

                        foreach (var product in products.Value) {
                            await helper.AddMerchantSettlementFee(settlementId, dateTime, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, CancellationToken.None);
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
            foreach (var merchant in this.merchantsList) {
                var expectedFees = this.context.MerchantSettlementFees.Where(f => f.MerchantId == merchant.MerchantId);

                var u = unsettledFees.SingleOrDefault(u => u.DimensionName == merchant.Name);

                u.ShouldNotBeNull();
                u.FeesCount.ShouldBe(await expectedFees.CountAsync(CancellationToken.None));
                u.FeesValue.ShouldBe(await expectedFees.SumAsync(f => f.CalculatedValue, CancellationToken.None));
            }
        }

        [Fact]
        public async Task FactSettlementsController_UnsettledFees_ByMerchant_MerchantFilter_SettlementReturned() {
            // Add some fees over a date range for multiple operators
            EstateManagementGenericContext context = new EstateManagementSqlServerContext(GetLocalConnectionString($"EstateReportingReadModel{TestId.ToString()}"));

            DatabaseHelper helper = new DatabaseHelper(context);

            List<DateTime> dates = new();
            dates.Add(new DateTime(2024, 5, 24));
            dates.Add(new DateTime(2024, 5, 25));
            dates.Add(new DateTime(2024, 5, 26));
            dates.Add(new DateTime(2024, 5, 27));

            foreach (DateTime dateTime in dates) {
                Guid settlementId = Guid.NewGuid();
                foreach (Merchant merchant in this.merchantsList) {
                    foreach ((Guid contractId, String contractName, Guid operatorId, String operatorName) contract in this.contractList) {
                        var products = this.contractProducts.Single(cp => cp.Key == contract.contractId);

                        foreach (var product in products.Value) {
                            await helper.AddMerchantSettlementFee(settlementId, dateTime, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, CancellationToken.None);
                        }
                    }
                }
            }

            DateTime startDate = dates.Min();
            DateTime endDate = dates.Max();

            var result = await ApiClient.GetUnsettledFees(string.Empty, Guid.NewGuid(), startDate, endDate, [1], null, null, GroupByOption.Merchant, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            var unsettledFees = result.Data;
            unsettledFees.ShouldNotBeNull();
            unsettledFees.ShouldNotBeEmpty();

            var merchantRecord = await this.context.Merchants.SingleOrDefaultAsync(me => me.MerchantReportingId == 1);
            var expectedFees = this.context.MerchantSettlementFees.Where(f => f.MerchantId == merchantRecord.MerchantId);

            var u = unsettledFees.SingleOrDefault(u => u.DimensionName == merchantRecord.Name);

            u.ShouldNotBeNull();
            u.FeesCount.ShouldBe(await expectedFees.CountAsync(CancellationToken.None));
            u.FeesValue.ShouldBe(await expectedFees.SumAsync(f => f.CalculatedValue, CancellationToken.None));
        }

        [Fact]
        public async Task FactSettlementsController_UnsettledFees_ByProduct_SettlementReturned() {
            // Add some fees over a date range for multiple operators
            EstateManagementGenericContext context = new EstateManagementSqlServerContext(GetLocalConnectionString($"EstateReportingReadModel{TestId.ToString()}"));

            DatabaseHelper helper = new DatabaseHelper(context);

            List<DateTime> dates = new();
            dates.Add(new DateTime(2024, 5, 24));
            dates.Add(new DateTime(2024, 5, 25));
            dates.Add(new DateTime(2024, 5, 26));
            dates.Add(new DateTime(2024, 5, 27));

            foreach (DateTime dateTime in dates) {
                Guid settlementId = Guid.NewGuid();
                foreach (Merchant merchant in this.merchantsList) {
                    foreach ((Guid contractId, String contractName, Guid operatorId, String operatorName) contract in this.contractList) {
                        var products = this.contractProducts.Single(cp => cp.Key == contract.contractId);

                        foreach (var product in products.Value) {
                            await helper.AddMerchantSettlementFee(settlementId, dateTime, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, CancellationToken.None);
                        }
                    }
                }
            }

            DateTime startDate = dates.Min();
            DateTime endDate = dates.Max();

            var result = await ApiClient.GetUnsettledFees(string.Empty, Guid.NewGuid(), startDate, endDate, null, null, null, GroupByOption.Product, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            var unsettledFees = result.Data;

            unsettledFees.ShouldNotBeNull();
            unsettledFees.ShouldNotBeEmpty();

            List<(String, Guid, String)> allProducts = new();
            foreach (KeyValuePair<Guid, List<(Guid productId, String productName, Decimal? productValue)>> contractProduct in this.contractProducts) {
                var contract = this.contractList.Single(c => c.contractId == contractProduct.Key);
                var @operator = await this.context.Operators.SingleOrDefaultAsync(o => o.OperatorId == contract.operatorId, CancellationToken.None);

                foreach ((Guid productId, String productName, Decimal? productValue) s in contractProduct.Value) {
                    allProducts.Add((@operator.Name, contractProduct.Key, s.productName));
                }
            }

            unsettledFees.Count.ShouldBe(allProducts.Distinct().Count());
            foreach (var contractProduct in allProducts.Distinct()) {
                var product = await this.context.ContractProducts.Where(cp => cp.ProductName == contractProduct.Item3 && cp.ContractId == contractProduct.Item2).SingleOrDefaultAsync(CancellationToken.None);
                var tf = await context.ContractProductTransactionFees.Where(cptf => cptf.ContractProductId == product.ContractProductId).ToListAsync(CancellationToken.None);
                var expectedFees = this.context.MerchantSettlementFees.Where(f => tf.Select(t => t.ContractProductTransactionFeeId).Contains(f.ContractProductTransactionFeeId));

                var u = unsettledFees.SingleOrDefault(u => u.DimensionName == $"{contractProduct.Item1} - {contractProduct.Item3}");

                u.ShouldNotBeNull($"{contractProduct.Item1} - {contractProduct.Item2}");
                u.FeesCount.ShouldBe(await expectedFees.CountAsync(CancellationToken.None));
                u.FeesValue.ShouldBe(await expectedFees.SumAsync(f => f.CalculatedValue, CancellationToken.None));
            }
        }

        [Fact]
        public async Task FactSettlementsController_UnsettledFees_ByProduct_ProductFilter_SettlementReturned() {
            // Add some fees over a date range for multiple operators
            EstateManagementGenericContext context = new EstateManagementSqlServerContext(GetLocalConnectionString($"EstateReportingReadModel{TestId.ToString()}"));

            DatabaseHelper helper = new DatabaseHelper(context);

            List<DateTime> dates = new();
            dates.Add(new DateTime(2024, 5, 24));
            dates.Add(new DateTime(2024, 5, 25));
            dates.Add(new DateTime(2024, 5, 26));
            dates.Add(new DateTime(2024, 5, 27));

            foreach (DateTime dateTime in dates) {
                Guid settlementId = Guid.NewGuid();
                foreach (Merchant merchant in this.merchantsList) {
                    foreach ((Guid contractId, String contractName, Guid operatorId, String operatorName) contract in this.contractList) {
                        var products = this.contractProducts.Single(cp => cp.Key == contract.contractId);

                        foreach (var product in products.Value) {
                            await helper.AddMerchantSettlementFee(settlementId, dateTime, merchant.MerchantId, contract.operatorId, contract.contractId, product.productId, CancellationToken.None);
                        }
                    }
                }
            }

            DateTime startDate = dates.Min();
            DateTime endDate = dates.Max();

            var result = await ApiClient.GetUnsettledFees(string.Empty, Guid.NewGuid(), startDate, endDate, null, null, [1], GroupByOption.Product, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            var unsettledFees = result.Data;

            unsettledFees.ShouldNotBeNull();
            unsettledFees.ShouldNotBeEmpty();

            var contractProduct = await this.context.ContractProducts.Where(cp => cp.ContractProductReportingId == 1).SingleOrDefaultAsync(CancellationToken.None);
            var contractRecord = this.contractList.Single(c => c.contractId == contractProduct.ContractId);
            var @operator = await this.context.Operators.SingleOrDefaultAsync(o => o.OperatorId == contractRecord.operatorId, CancellationToken.None);
            var tf = await context.ContractProductTransactionFees.Where(cptf => cptf.ContractProductId == contractProduct.ContractProductId).ToListAsync(CancellationToken.None);
            var expectedFees = this.context.MerchantSettlementFees.Where(f => tf.Select(t => t.ContractProductTransactionFeeId).Contains(f.ContractProductTransactionFeeId));

            var u = unsettledFees.SingleOrDefault(u => u.DimensionName == $"{@operator.Name} - {contractProduct.ProductName}");

            u.ShouldNotBeNull($"{@operator.Name} - {contractProduct.ProductName}");
            u.FeesCount.ShouldBe(await expectedFees.CountAsync(CancellationToken.None));
            u.FeesValue.ShouldBe(await expectedFees.SumAsync(f => f.CalculatedValue, CancellationToken.None));

        }
    }
}

