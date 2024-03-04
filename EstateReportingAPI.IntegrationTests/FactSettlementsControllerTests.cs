namespace EstateReportingAPI.IntegrationTests
{
    using DataTransferObjects;
    using EstateManagement.Database.Contexts;
    using EstateManagement.Database.Entities;
    using Microsoft.EntityFrameworkCore;
    using Newtonsoft.Json;
    using Shouldly;
    using Xunit;

    public class FactSettlementsControllerTests : ControllerTestsBase
    {
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
        [Fact]
        public async Task FactSettlementsController_TodaysSettlement_SettlementReturned(){

            Int32 overallTodaysSettlementTransactionCount = 0;
            Int32 overallTodaysPendingSettlementTransactionCount = 0;

            Int32 overallComparisonSettlementTransactionCount = 0;
            Int32 overallComparisonPendingSettlementTransactionCount = 0;
            List<(Decimal settledTransactions, Decimal pendingSettlementTransactions, Decimal settlementFees, Decimal pendingSettlementFees)> todayOverallTotals = new();
            List<(Decimal settledTransactions, Decimal pendingSettlementTransactions, Decimal settlementFees, Decimal pendingSettlementFees)> comparisonOverallTotals = new();
            var estateOperator = await this.context.EstateOperators.SingleOrDefaultAsync(o => o.Name == "Safaricom");
            foreach (String merchant in this.merchantsList){
                Int32 todaysSettlementTransactionCount = 15;
                Int32 todaysPendingSettlementTransactionCount = 9;
                (Decimal settledTransactions, Decimal pendingSettlementTransactions, Decimal settlementFees, Decimal pendingSettlementFees) todayTotals = await helper.AddSettlementRecord(merchant, "Safaricom", DateTime.Now, todaysSettlementTransactionCount, todaysPendingSettlementTransactionCount);
                todayOverallTotals.Add(todayTotals);

                overallTodaysSettlementTransactionCount += todaysSettlementTransactionCount; ;
                overallTodaysPendingSettlementTransactionCount += todaysPendingSettlementTransactionCount;

                Int32 comparisonSettlementTransactionCount = 12;
                Int32 comparisonPendingSettlementTransactionCount = 11;
                var comparisonTotals = await helper.AddSettlementRecord(merchant, "Safaricom", DateTime.Now.AddDays(-1), comparisonSettlementTransactionCount, comparisonPendingSettlementTransactionCount);
                comparisonOverallTotals.Add(comparisonTotals);

                overallComparisonSettlementTransactionCount += comparisonSettlementTransactionCount;
                overallComparisonPendingSettlementTransactionCount += comparisonPendingSettlementTransactionCount;
            }
            TodaysSettlement todaysSettlement = await this.CreateAndSendHttpRequestMessage<TodaysSettlement>($"api/facts/settlements/todayssettlement?comparisonDate={DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd")}", CancellationToken.None);
            todaysSettlement.ShouldNotBeNull();
            todaysSettlement.ComparisonSettlementCount.ShouldBe(overallComparisonSettlementTransactionCount);
            todaysSettlement.ComparisonSettlementValue.ShouldBe(comparisonOverallTotals.Sum(c => c.settlementFees));
            todaysSettlement.ComparisonPendingSettlementCount.ShouldBe(overallComparisonPendingSettlementTransactionCount);
            todaysSettlement.ComparisonPendingSettlementValue.ShouldBe(comparisonOverallTotals.Sum(c=> c.pendingSettlementFees));

            todaysSettlement.TodaysSettlementCount.ShouldBe(overallTodaysSettlementTransactionCount);
            todaysSettlement.TodaysSettlementValue.ShouldBe(todayOverallTotals.Sum(c => c.settlementFees));
            todaysSettlement.TodaysPendingSettlementCount.ShouldBe(overallTodaysPendingSettlementTransactionCount);
            todaysSettlement.TodaysPendingSettlementValue.ShouldBe(todayOverallTotals.Sum(c => c.pendingSettlementFees));
        }

        [Fact]
        public async Task FactSettlementsController_LastSettlement_SettlementReturned()
        {
            EstateManagementGenericContext context = new EstateManagementSqlServerContext(GetLocalConnectionString($"EstateReportingReadModel{this.TestId.ToString()}"));

            DatabaseHelper helper = new DatabaseHelper(context);

            Int32 overallTodaysSettlementTransactionCount = 0;
            Int32 overallTodaysPendingSettlementTransactionCount = 0;

            Int32 overallComparisonSettlementTransactionCount = 0;
            Int32 overallComparisonPendingSettlementTransactionCount = 0;
            List<(Decimal settledTransactions, Decimal pendingSettlementTransactions, Decimal settlementFees, Decimal pendingSettlementFees)> todayOverallTotals = new();
            List<(Decimal settledTransactions, Decimal pendingSettlementTransactions, Decimal settlementFees, Decimal pendingSettlementFees)> comparisonOverallTotals = new();
            var estateOperator = await this.context.EstateOperators.SingleOrDefaultAsync(o => o.Name == "Safaricom");
            foreach (String merchant in this.merchantsList)
            {
                Int32 todaysSettlementTransactionCount = 15;
                Int32 todaysPendingSettlementTransactionCount = 9;
                (Decimal settledTransactions, Decimal pendingSettlementTransactions, Decimal settlementFees, Decimal pendingSettlementFees) todayTotals = await helper.AddSettlementRecord(merchant, "Safaricom", DateTime.Now, todaysSettlementTransactionCount, todaysPendingSettlementTransactionCount);
                todayOverallTotals.Add(todayTotals);

                overallTodaysSettlementTransactionCount += todaysSettlementTransactionCount; ;
                overallTodaysPendingSettlementTransactionCount += todaysPendingSettlementTransactionCount;

                Int32 comparisonSettlementTransactionCount = 12;
                Int32 comparisonPendingSettlementTransactionCount = 11;
                var comparisonTotals = await helper.AddSettlementRecord(merchant, "Safaricom", DateTime.Now.AddDays(-1), comparisonSettlementTransactionCount, comparisonPendingSettlementTransactionCount);
                comparisonOverallTotals.Add(comparisonTotals);

                overallComparisonSettlementTransactionCount += comparisonSettlementTransactionCount;
                overallComparisonPendingSettlementTransactionCount += comparisonPendingSettlementTransactionCount;
            }

            LastSettlement lastSettlement = await this.CreateAndSendHttpRequestMessage<LastSettlement>($"api/facts/settlements/lastsettlement", CancellationToken.None);
            lastSettlement.ShouldNotBeNull();
            lastSettlement.FeesValue.ShouldBe(todayOverallTotals.Sum(t => t.settlementFees));
            lastSettlement.SalesCount.ShouldBe(overallTodaysSettlementTransactionCount);
            lastSettlement.SalesValue.ShouldBe(todayOverallTotals.Sum(c => c.settledTransactions));
        }
    }
}

