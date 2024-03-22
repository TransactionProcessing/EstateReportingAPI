namespace EstateReportingAPI.IntegrationTests
{
    using EstateManagement.Database.Contexts;
    using EstateReportingAPI.DataTransferObjects;
    using Microsoft.EntityFrameworkCore;
    using Shouldly;
    using Xunit;

    public class FactSettlementsControllerTests : ControllerTestsBase
    {
        protected override async Task ClearStandingData()
        {
            await helper.DeleteAllContracts();
            await helper.DeleteAllEstateOperator();
            await helper.DeleteAllMerchants();
        }

        protected override async Task SetupStandingData()
        {
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
                                           context.EstateOperators,
                                           c => c.OperatorId,
                                           o => o.OperatorId,
                                           (c, o) => new { c.Description, OperatorName = o.Name }
                                          )
                                     .ToList().Select(x => (x.Description, x.OperatorName))
                                     .ToList();


            var query1 = context.Contracts
                             .GroupJoin(
                                        context.ContractProducts,
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
        [Theory]
        [InlineData(ClientType.Api)]
        [InlineData(ClientType.Direct)]
        public async Task FactSettlementsController_TodaysSettlement_SettlementReturned(ClientType clientType)
        {

            int overallTodaysSettlementTransactionCount = 0;
            int overallTodaysPendingSettlementTransactionCount = 0;

            int overallComparisonSettlementTransactionCount = 0;
            int overallComparisonPendingSettlementTransactionCount = 0;
            List<(decimal settledTransactions, decimal pendingSettlementTransactions, decimal settlementFees, decimal pendingSettlementFees)> todayOverallTotals = new();
            List<(decimal settledTransactions, decimal pendingSettlementTransactions, decimal settlementFees, decimal pendingSettlementFees)> comparisonOverallTotals = new();
            var estateOperator = await context.EstateOperators.SingleOrDefaultAsync(o => o.Name == "Safaricom");
            foreach (string merchant in merchantsList)
            {
                int todaysSettlementTransactionCount = 15;
                int todaysPendingSettlementTransactionCount = 9;
                (decimal settledTransactions, decimal pendingSettlementTransactions, decimal settlementFees, decimal pendingSettlementFees) todayTotals = await helper.AddSettlementRecord(merchant, "Safaricom", DateTime.Now, todaysSettlementTransactionCount, todaysPendingSettlementTransactionCount);
                todayOverallTotals.Add(todayTotals);

                overallTodaysSettlementTransactionCount += todaysSettlementTransactionCount; ;
                overallTodaysPendingSettlementTransactionCount += todaysPendingSettlementTransactionCount;

                int comparisonSettlementTransactionCount = 12;
                int comparisonPendingSettlementTransactionCount = 11;
                var comparisonTotals = await helper.AddSettlementRecord(merchant, "Safaricom", DateTime.Now.AddDays(-1), comparisonSettlementTransactionCount, comparisonPendingSettlementTransactionCount);
                comparisonOverallTotals.Add(comparisonTotals);

                overallComparisonSettlementTransactionCount += comparisonSettlementTransactionCount;
                overallComparisonPendingSettlementTransactionCount += comparisonPendingSettlementTransactionCount;
            }

            Func<Task<TodaysSettlement>> asyncFunction = async () =>
                                                                {
                                                                    TodaysSettlement result = clientType switch
                                                                    {
                                                                        ClientType.Api => await ApiClient.GetTodaysSettlement(string.Empty, Guid.NewGuid(), null,null, DateTime.Now.AddDays(-1), CancellationToken.None),
                                                                        _ => await CreateAndSendHttpRequestMessage<TodaysSettlement>($"api/facts/settlements/todayssettlement?comparisonDate={DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd")}", CancellationToken.None)
                                                                    };
                                                                    return result;
                                                                };
            TodaysSettlement todaysSettlement = await ExecuteAsyncFunction(asyncFunction);

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

        [Theory]
        [InlineData(ClientType.Api)]
        [InlineData(ClientType.Direct)]
        public async Task FactSettlementsController_LastSettlement_SettlementReturned(ClientType clientType)
        {
            EstateManagementGenericContext context = new EstateManagementSqlServerContext(GetLocalConnectionString($"EstateReportingReadModel{TestId.ToString()}"));

            DatabaseHelper helper = new DatabaseHelper(context);

            int overallTodaysSettlementTransactionCount = 0;
            int overallTodaysPendingSettlementTransactionCount = 0;

            int overallComparisonSettlementTransactionCount = 0;
            int overallComparisonPendingSettlementTransactionCount = 0;
            List<(decimal settledTransactions, decimal pendingSettlementTransactions, decimal settlementFees, decimal pendingSettlementFees)> todayOverallTotals = new();
            List<(decimal settledTransactions, decimal pendingSettlementTransactions, decimal settlementFees, decimal pendingSettlementFees)> comparisonOverallTotals = new();
            var estateOperator = await this.context.EstateOperators.SingleOrDefaultAsync(o => o.Name == "Safaricom");
            foreach (string merchant in merchantsList)
            {
                int todaysSettlementTransactionCount = 15;
                int todaysPendingSettlementTransactionCount = 9;
                (decimal settledTransactions, decimal pendingSettlementTransactions, decimal settlementFees, decimal pendingSettlementFees) todayTotals = await helper.AddSettlementRecord(merchant, "Safaricom", DateTime.Now, todaysSettlementTransactionCount, todaysPendingSettlementTransactionCount);
                todayOverallTotals.Add(todayTotals);

                overallTodaysSettlementTransactionCount += todaysSettlementTransactionCount; ;
                overallTodaysPendingSettlementTransactionCount += todaysPendingSettlementTransactionCount;

                int comparisonSettlementTransactionCount = 12;
                int comparisonPendingSettlementTransactionCount = 11;
                var comparisonTotals = await helper.AddSettlementRecord(merchant, "Safaricom", DateTime.Now.AddDays(-1), comparisonSettlementTransactionCount, comparisonPendingSettlementTransactionCount);
                comparisonOverallTotals.Add(comparisonTotals);

                overallComparisonSettlementTransactionCount += comparisonSettlementTransactionCount;
                overallComparisonPendingSettlementTransactionCount += comparisonPendingSettlementTransactionCount;
            }

            //LastSettlement lastSettlement = await this.CreateAndSendHttpRequestMessage<LastSettlement>($"api/facts/settlements/lastsettlement", CancellationToken.None);

            Func<Task<LastSettlement>> asyncFunction = async () =>
                                                       {
                                                           LastSettlement result = clientType switch
                                                           {
                                                               ClientType.Api => await ApiClient.GetLastSettlement(string.Empty, Guid.NewGuid(), CancellationToken.None),
                                                               _ => await CreateAndSendHttpRequestMessage<LastSettlement>($"api/facts/settlements/lastsettlement", CancellationToken.None)
                                                           };
                                                           return result;
                                                       };
            LastSettlement lastSettlement = await ExecuteAsyncFunction(asyncFunction);

            lastSettlement.ShouldNotBeNull();
            lastSettlement.FeesValue.ShouldBe(todayOverallTotals.Sum(t => t.settlementFees));
            lastSettlement.SalesCount.ShouldBe(overallTodaysSettlementTransactionCount);
            lastSettlement.SalesValue.ShouldBe(todayOverallTotals.Sum(c => c.settledTransactions));
        }
    }
}

