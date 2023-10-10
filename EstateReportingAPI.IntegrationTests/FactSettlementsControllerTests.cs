namespace EstateReportingAPI.IntegrationTests
{
    using DataTransferObjects;
    using EstateManagement.Database.Contexts;
    using EstateManagement.Database.Entities;
    using Newtonsoft.Json;
    using Shouldly;
    using Xunit;

    public class FactSettlementsControllerTests : ControllerTestsBase, IDisposable
    {
        [Fact]
        public async Task FactSettlementsController_TodaysSettlement_SettlementReturned(){
            EstateManagementGenericContext context = new EstateManagementSqlServerContext(GetLocalConnectionString($"EstateReportingReadModel{this.TestId.ToString()}"));

            DatabaseHelper helper = new DatabaseHelper(context);
            Int32 todaysSettlementTransactionCount = 15;
            Int32 todaysPendingSettlementTransactionCount = 9;
            var todayTotals = await helper.AddSettlementRecord(DateTime.Now, 1, 1, todaysSettlementTransactionCount, todaysPendingSettlementTransactionCount);

            Int32 comparisonSettlementTransactionCount = 15;
            Int32 comparisonPendingSettlementTransactionCount = 9;
            var comparisonTotals = await helper.AddSettlementRecord(DateTime.Now.AddDays(-1), 1, 1, todaysSettlementTransactionCount, todaysPendingSettlementTransactionCount);

            HttpResponseMessage response = await this.CreateAndSendHttpRequestMessage($"api/facts/settlements/todayssettlement?comparisonDate={DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd")}");

            String content = await response.Content.ReadAsStringAsync(CancellationToken.None);
            TodaysSettlement? todaysSettlement = JsonConvert.DeserializeObject<TodaysSettlement>(content);
            todaysSettlement.ComparisonSettlementCount.ShouldBe(comparisonSettlementTransactionCount);
            todaysSettlement.ComparisonSettlementValue.ShouldBe(comparisonTotals.settlementFeesValue);
            todaysSettlement.ComparisonPendingSettlementCount.ShouldBe(comparisonPendingSettlementTransactionCount);
            todaysSettlement.ComparisonPendingSettlementValue.ShouldBe(comparisonTotals.pendingSettlementFeesValue);
            
            todaysSettlement.TodaysSettlementCount.ShouldBe(todaysSettlementTransactionCount);
            todaysSettlement.TodaysSettlementValue.ShouldBe(todayTotals.settlementFeesValue);
            todaysSettlement.TodaysPendingSettlementCount.ShouldBe(todaysPendingSettlementTransactionCount);
            todaysSettlement.TodaysPendingSettlementValue.ShouldBe(todayTotals.pendingSettlementFeesValue);
        }

        [Fact]
        public async Task FactSettlementsController_LastSettlement_SettlementReturned()
        {
            EstateManagementGenericContext context = new EstateManagementSqlServerContext(GetLocalConnectionString($"EstateReportingReadModel{this.TestId.ToString()}"));

            DatabaseHelper helper = new DatabaseHelper(context);
            
            Int32 todaysSettlementTransactionCount = 15;
            Int32 todaysPendingSettlementTransactionCount = 9;
            var todayTotals = await helper.AddSettlementRecord(DateTime.Now, 1, 1, todaysSettlementTransactionCount, todaysPendingSettlementTransactionCount);

            Int32 comparisonSettlementTransactionCount = 15;
            Int32 comparisonPendingSettlementTransactionCount = 9;
            var comparisonTotals = await helper.AddSettlementRecord(DateTime.Now.AddDays(-1), 1, 1, todaysSettlementTransactionCount, todaysPendingSettlementTransactionCount);

            HttpResponseMessage response = await this.CreateAndSendHttpRequestMessage($"api/facts/settlements/lastsettlement");

            response.IsSuccessStatusCode.ShouldBeTrue();
            String content = await response.Content.ReadAsStringAsync(CancellationToken.None);
            var lastSettlement = JsonConvert.DeserializeObject<LastSettlement>(content);
            lastSettlement.FeesValue.ShouldBe(todayTotals.settlementFeesValue);
            lastSettlement.SalesCount.ShouldBe(todaysSettlementTransactionCount);
            lastSettlement.SalesValue.ShouldBe(todayTotals.settledTransactionsValue);
        }
    }
}

