﻿namespace EstateReportingAPI.IntegrationTests
{
    using DataTransferObjects;
    using EstateManagement.Database.Contexts;
    using EstateManagement.Database.Entities;
    using Newtonsoft.Json;
    using Shouldly;
    using Xunit;

    public class FactSettlementsControllerTests : ControllerTestsBase, IDisposable
    {
        public void Dispose()
        {
            EstateManagementGenericContext context = new EstateManagementSqlServerContext(GetLocalConnectionString($"EstateReportingReadModel{this.TestId.ToString()}"));

            Console.WriteLine($"About to delete database EstateReportingReadModel{this.TestId.ToString()}");
            Boolean result = context.Database.EnsureDeleted();
            Console.WriteLine($"Delete result is {result}");
            result.ShouldBeTrue();
        }

        [Fact]
        public async Task FactSettlementsController_TodaysSettlement_SettlementReturned(){
            EstateManagementGenericContext context = new EstateManagementSqlServerContext(GetLocalConnectionString($"EstateReportingReadModel{this.TestId.ToString()}"));

            DatabaseHelper helper = new DatabaseHelper(context);
            List<(Decimal feeValue, Decimal calulatedValue, Int32 transactionFeeReportingId)> todaysSettlementFees = new();

            Int32 todaysSettlementTransactionCount = 15;
            for (int i = 1; i <= todaysSettlementTransactionCount; i++){
                todaysSettlementFees.Add((0.5m, 0.5m * i, i));
            }

            await helper.AddSettlementRecordWithFees(DateTime.Now, 1, 1, todaysSettlementFees);

            List<(Decimal feeValue, Decimal calulatedValue, Int32 transactionFeeReportingId)> comparisonDateSettlementFees = new();

            Int32 comparisonDateSettlementTransactionCount = 9;
            for (int i = 1; i <= comparisonDateSettlementTransactionCount; i++)
            {
                comparisonDateSettlementFees.Add((0.5m, 0.5m * i, i));
            }

            DateTime comparisonDate = DateTime.Now.AddDays(-1);
            await helper.AddSettlementRecordWithFees(comparisonDate, 1, 1, comparisonDateSettlementFees);

            HttpResponseMessage response = await this.CreateAndSendHttpRequestMessage($"api/facts/settlements/todayssettlement?comparisonDate={comparisonDate.ToString("yyyy-MM-dd")}");

            response.IsSuccessStatusCode.ShouldBeTrue();
            String content = await response.Content.ReadAsStringAsync(CancellationToken.None);
            TodaysSettlement? todaysSettlement = JsonConvert.DeserializeObject<TodaysSettlement>(content);
            todaysSettlement.ComparisonSettlementCount.ShouldBe(comparisonDateSettlementTransactionCount);
            todaysSettlement.ComparisonSettlementValue.ShouldBe(comparisonDateSettlementFees.Sum(c => c.calulatedValue));

            todaysSettlement.TodaysSettlementCount.ShouldBe(todaysSettlementTransactionCount);
            todaysSettlement.TodaysSettlementValue.ShouldBe(todaysSettlementFees.Sum(c => c.calulatedValue));
        }

        [Fact]
        public async Task FactSettlementsController_LastSettlement_SettlementReturned()
        {
            EstateManagementGenericContext context = new EstateManagementSqlServerContext(GetLocalConnectionString($"EstateReportingReadModel{this.TestId.ToString()}"));

            DatabaseHelper helper = new DatabaseHelper(context);
            List<(Decimal feeValue, Decimal calulatedValue, Int32 transactionFeeReportingId)> todaysSettlementFees = new();
            List<Transaction> todaysSettlementTransactions = new();
            List<Transaction> yeterdaysSettlementTransactions = new();
            Int32 todaysSettlementTransactionCount = 15;
            for (int i = 1; i <= todaysSettlementTransactionCount; i++){
                var transaction = await helper.AddTransaction(DateTime.Now, 1, "Safaricom", 1, "0000", i);
                todaysSettlementTransactions.Add(transaction);
                todaysSettlementFees.Add((0.5m, 0.5m * i, i));
            }
            await helper.AddSettlementRecordWithFees(DateTime.Now, 1, 1, todaysSettlementFees);

            List<(Decimal feeValue, Decimal calulatedValue, Int32 transactionFeeReportingId)> yesterdaysSettlementFees = new();

            DateTime comparisonDate = DateTime.Now.AddDays(-1);
            Int32 yesterdaysSettlementTransactionCount = 9;
            for (int i = 1; i <= yesterdaysSettlementTransactionCount; i++)
            {
                var transaction = await helper.AddTransaction(comparisonDate, 1, "Safaricom", 1, "0000", i);
                yeterdaysSettlementTransactions.Add(transaction);
                yesterdaysSettlementFees.Add((0.5m, 0.5m * i, i));
            }
            
            await helper.AddSettlementRecordWithFees(comparisonDate, 1, 1, yesterdaysSettlementFees);

            HttpResponseMessage response = await this.CreateAndSendHttpRequestMessage($"api/facts/settlements/lastsettlement");

            response.IsSuccessStatusCode.ShouldBeTrue();
            String content = await response.Content.ReadAsStringAsync(CancellationToken.None);
            var lastSettlement = JsonConvert.DeserializeObject<LastSettlement>(content);
            lastSettlement.FeesValue.ShouldBe(todaysSettlementFees.Sum(f => f.calulatedValue));
            lastSettlement.SalesCount.ShouldBe(todaysSettlementTransactionCount);
            lastSettlement.SalesValue.ShouldBe(todaysSettlementTransactions.Sum(t => t.TransactionAmount));
        }
    }
}

