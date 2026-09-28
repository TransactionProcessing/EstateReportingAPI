using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.Models;
using Imposter.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Shared.EntityFramework;
using TransactionProcessor.Database.Contexts;
using Db = TransactionProcessor.Database.Entities;
using BalanceState = TransactionProcessor.ProjectionEngine.Database.Database.Entities.MerchantBalanceProjectionState;

namespace EstateReportingAPI.BusinessLogic.UnitTests;

public sealed class ReportingManagerReportTests
{
    private static readonly Guid EstateId = Guid.Parse("F64241E7-F778-4F77-8A64-099CB51BF4CE");

    [Fact]
    public async Task GetCalendarComparisonDates_ReturnsCalendarRows()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        database.AddCalendar(new DateTime(2026, 9, 1));
        await database.SaveAsync();

        var result = await database.Manager.GetCalendarComparisonDates(
            new CalendarQueries.GetComparisonDatesQuery(EstateId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Data.Count.ShouldBe(1);
        result.Data[0].Date.ShouldBe(new DateTime(2026, 9, 1));
    }

    [Fact]
    public async Task GetCalendarDates_ReturnsCalendarRows()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        database.AddCalendar(new DateTime(2026, 9, 1));
        await database.SaveAsync();

        var result = await database.Manager.GetCalendarDates(
            new CalendarQueries.GetAllDatesQuery(EstateId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Data.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetContract_ReturnsContractWithProducts()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        database.AddContractFees();
        await database.SaveAsync();

        var result = await database.Manager.GetContract(
            new ContractQueries.GetContractQuery(EstateId, database.ContractId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Data.ContractId.ShouldBe(database.ContractId);
        result.Data.Products.Count.ShouldBe(1);
        result.Data.Products[0].TransactionFees.Count.ShouldBe(2);
        result.Data.Products[0].TransactionFees.Select(fee => fee.Description).ShouldContain("Enabled fee");
        result.Data.Products[0].TransactionFees.Select(fee => fee.Description).ShouldContain("Disabled fee");
    }

    [Fact]
    public async Task GetRecentContracts_ReturnsRecentContracts()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();

        var result = await database.Manager.GetRecentContracts(
            new ContractQueries.GetRecentContractsQuery(EstateId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Data.Count.ShouldBe(1);
        result.Data[0].ContractId.ShouldBe(database.ContractId);
    }

    [Fact]
    public async Task GetEstate_ReturnsEstateAndRelatedData()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        database.AddEstateUser();
        await database.SaveAsync();

        var result = await database.Manager.GetEstate(
            new EstateQueries.GetEstateQuery(EstateId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Data.EstateId.ShouldBe(EstateId);
        result.Data.Merchants.Count.ShouldBe(2);
        result.Data.Contracts.Count.ShouldBe(1);
        result.Data.Users.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetEstateOperators_ReturnsEstateOperators()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        database.AddEstateOperator();
        await database.SaveAsync();

        var result = await database.Manager.GetEstateOperators(
            new EstateQueries.GetEstateOperatorsQuery(EstateId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Data.Count.ShouldBe(1);
        result.Data[0].OperatorId.ShouldBe(database.OperatorId);
    }

    [Fact]
    public async Task GetRecentMerchants_ReturnsMerchantsWithBalances()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        database.AddBalance(database.MerchantId, 10m);
        database.AddBalance(database.SecondMerchantId, 20m);
        await database.SaveAsync();

        var result = await database.Manager.GetRecentMerchants(
            new MerchantQueries.GetRecentMerchantsQuery(EstateId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Data.Count.ShouldBe(2);
        result.Data[0].Balance.ShouldBe(10m);
    }

    [Fact]
    public async Task GetMerchantsTransactionKpis_ReturnsKpis()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        database.AddBalance(database.MerchantId, 10m);
        await database.SaveAsync();

        var result = await database.Manager.GetMerchantsTransactionKpis(
            new MerchantQueries.GetTransactionKpisQuery(EstateId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Data.MerchantsWithNoSaleInLast7Days.ShouldBe(1);
    }

    [Fact]
    public async Task GetOperators_ReturnsOperators()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();

        var result = await database.Manager.GetOperators(
            new OperatorQueries.GetOperatorsQuery(EstateId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Data.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetOperator_ReturnsOperator()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();

        var result = await database.Manager.GetOperator(
            new OperatorQueries.GetOperatorQuery(EstateId, database.OperatorId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Data.OperatorId.ShouldBe(database.OperatorId);
    }

    [Fact]
    public async Task GetFileImportLogList_WhenNoLogs_ReturnsEmptyList()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();

        var result = await database.Manager.GetFileImportLogList(
            new FileImportLogQueries.GetFileImportLogListQuery(EstateId, null, DateTime.Today.AddDays(-1), DateTime.Today),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetFileImportLogList_WithFileDataBuildsHierarchicalResult()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        Guid logId = database.AddFileImportData();
        await database.SaveAsync();

        var result = await database.Manager.GetFileImportLogList(
            new FileImportLogQueries.GetFileImportLogListQuery(EstateId, null, DateTime.Today.AddDays(-1), DateTime.Today),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Data.Count.ShouldBe(1);
        result.Data[0].FileImportLogId.ShouldBe(logId);
        result.Data[0].FileDetailsList.Count.ShouldBe(1);
        result.Data[0].FileDetailsList[0].FileLines.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetFileImportLog_WhenLogDoesNotExist_ReturnsNotFound()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();

        var result = await database.Manager.GetFileImportLog(
            new FileImportLogQueries.GetFileImportLogQuery(EstateId, null, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task GetFileImportLog_WithFileDataBuildsHierarchicalResult()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        Guid logId = database.AddFileImportData();
        await database.SaveAsync();

        var result = await database.Manager.GetFileImportLog(
            new FileImportLogQueries.GetFileImportLogQuery(EstateId, null, logId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Data.FileImportLogId.ShouldBe(logId);
        result.Data.FileDetailsList.Count.ShouldBe(1);
        result.Data.FileDetailsList[0].FileLines.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetFileProfileConfigurationList_WhenNoConfigurations_ReturnsEmptyList()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();

        var result = await database.Manager.GetFileProfileConfigurationList(
            new FileProfileConfigurationQueries.GetFileProfileConfigurationListQuery(EstateId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetTransactionSummaryByMerchantReport_AverageUsesTransactionCount()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        database.AddSale(database.MerchantId, database.OperatorId, 10m, new DateTime(2026, 9, 1));
        database.AddSale(database.MerchantId, database.OperatorId, 20m, new DateTime(2026, 9, 1));
        database.AddSale(database.MerchantId, database.SecondOperatorId, 30m, new DateTime(2026, 9, 1));
        await database.SaveAsync();

        var result = await database.Manager.GetTransactionSummaryByMerchantReport(
            new TransactionQueries.TransactionSummaryByMerchantQuery(
                EstateId,
                new TransactionSummaryByMerchantRequest {
                    StartDate = new DateTime(2026, 9, 1),
                    EndDate = new DateTime(2026, 9, 1)
                }),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Data.Merchants.Single().AverageValue.ShouldBe(20m);
        result.Data.Summary.AverageValue.ShouldBe(20m);
    }

    [Fact]
    public async Task GetTransactionSummaryByOperatorReport_SeparatesAuthorisedAndDeclinedTotals()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        database.AddSale(database.MerchantId, database.OperatorId, 10m, new DateTime(2026, 9, 1), authorised: true);
        database.AddSale(database.MerchantId, database.OperatorId, 20m, new DateTime(2026, 9, 1), authorised: false);
        database.AddSale(database.SecondMerchantId, database.OperatorId, 30m, new DateTime(2026, 9, 1), authorised: true);
        await database.SaveAsync();

        var result = await database.Manager.GetTransactionSummaryByOperatorReport(
            new TransactionQueries.TransactionSummaryByOperatorQuery(
                EstateId,
                new TransactionSummaryByOperatorRequest {
                    StartDate = new DateTime(2026, 9, 1),
                    EndDate = new DateTime(2026, 9, 1)
                }),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var summary = result.Data.Operators.Single();
        summary.TotalCount.ShouldBe(3);
        summary.TotalValue.ShouldBe(60m);
        summary.AuthorisedCount.ShouldBe(2);
        summary.DeclinedCount.ShouldBe(1);
        summary.AuthorisedPercentage.ShouldBe(2m / 3m);
        summary.AverageValue.ShouldBe(20m);
    }

    [Fact]
    public async Task GetTransactionDetailReport_AppliesMerchantProductAndOperatorFilters()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        database.AddSale(database.MerchantId, database.OperatorId, 10m, new DateTime(2026, 9, 1));
        database.AddSale(database.SecondMerchantId, database.SecondOperatorId, 20m, new DateTime(2026, 9, 1));
        await database.SaveAsync();

        var result = await database.Manager.GetTransactionDetailReport(
            new TransactionQueries.TransactionDetailReportQuery(
                EstateId,
                new TransactionDetailReportRequest {
                    StartDate = new DateTime(2026, 9, 1),
                    EndDate = new DateTime(2026, 9, 1),
                    Merchants = [20],
                    Products = [40],
                    Operators = [10]
                }),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Data.Summary.TransactionCount.ShouldBe(1);
        result.Data.Summary.TotalValue.ShouldBe(10m);
    }

    [Fact]
    public async Task GetTransactionSummaryByOperatorReport_AppliesMerchantAndOperatorFilters()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        database.AddSale(database.MerchantId, database.OperatorId, 10m, new DateTime(2026, 9, 1));
        database.AddSale(database.SecondMerchantId, database.SecondOperatorId, 20m, new DateTime(2026, 9, 1));
        await database.SaveAsync();

        var result = await database.Manager.GetTransactionSummaryByOperatorReport(
            new TransactionQueries.TransactionSummaryByOperatorQuery(
                EstateId,
                new TransactionSummaryByOperatorRequest {
                    StartDate = new DateTime(2026, 9, 1),
                    EndDate = new DateTime(2026, 9, 1),
                    Merchants = [20],
                    Operators = [10]
                }),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Data.Operators.Single().TotalValue.ShouldBe(10m);
    }

    [Fact]
    public async Task GetTransactionSummaryByMerchantReport_WhenNoSales_ReturnsZeroResult()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();

        var result = await database.Manager.GetTransactionSummaryByMerchantReport(
            new TransactionQueries.TransactionSummaryByMerchantQuery(
                EstateId,
                new TransactionSummaryByMerchantRequest {
                    StartDate = new DateTime(2026, 9, 1),
                    EndDate = new DateTime(2026, 9, 1)
                }),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Data.Merchants.ShouldBeEmpty();
        result.Data.Summary.TotalCount.ShouldBe(0);
        result.Data.Summary.TotalValue.ShouldBe(0m);
    }

    [Fact]
    public async Task GetTransactionMixSummary_WhenDateRangeIsInvalid_ReturnsFailure()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();

        var result = await database.Manager.GetTransactionMixSummary(
            new TransactionQueries.TransactionMixSummaryQuery(
                EstateId,
                new TransactionMixSummaryRequest {
                    StartDate = new DateTime(2026, 9, 2),
                    EndDate = new DateTime(2026, 9, 1),
                    Breakdown = TransactionMixBreakdown.Product,
                    Measure = TransactionMixMeasure.Count
                }),
            CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task GetTransactionMixSummary_WhenBreakdownIsUnsupported_ReturnsFailure()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();

        var result = await database.Manager.GetTransactionMixSummary(
            new TransactionQueries.TransactionMixSummaryQuery(
                EstateId,
                new TransactionMixSummaryRequest {
                    StartDate = new DateTime(2026, 9, 1),
                    EndDate = new DateTime(2026, 9, 1),
                    Breakdown = (TransactionMixBreakdown)999,
                    Measure = TransactionMixMeasure.Count
                }),
            CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task GetRecentActivityReceiptReport_NormalizesInvalidPagingAndReturnsEmptyResult()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();

        var result = await database.Manager.GetRecentActivityReceiptReport(
            new TransactionQueries.GetRecentActivityReceiptReportQuery(
                EstateId,
                new GetRecentActivityReceiptReportRequest {
                    ReportDate = new DateTime(2026, 9, 1),
                    PageNumber = 0,
                    PageSize = 0
                }),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Data.PageNumber.ShouldBe(1);
        result.Data.PageSize.ShouldBe(10);
        result.Data.TotalCount.ShouldBe(0);
        result.Data.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetRecentActivityReceiptReport_PaginatesOrderedRowsAndPreservesTotalCount()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        database.AddSale(database.MerchantId, database.OperatorId, 10m, new DateTime(2026, 9, 1, 9, 0, 0));
        database.AddSale(database.MerchantId, database.OperatorId, 20m, new DateTime(2026, 9, 1, 10, 0, 0));
        database.AddSale(database.MerchantId, database.OperatorId, 30m, new DateTime(2026, 9, 1, 11, 0, 0));
        await database.SaveAsync();

        var result = await database.Manager.GetRecentActivityReceiptReport(
            new TransactionQueries.GetRecentActivityReceiptReportQuery(
                EstateId,
                new GetRecentActivityReceiptReportRequest {
                    ReportDate = new DateTime(2026, 9, 1),
                    PageNumber = 2,
                    PageSize = 2
                }),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Data.TotalCount.ShouldBe(3);
        result.Data.Items.Count.ShouldBe(1);
        result.Data.Items.Single().Amount.ShouldBe(10m);
    }

    [Fact]
    public async Task GetTransactionDetailReport_IncludesFeesAndZeroFeeForUnsettledRows()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        Guid transactionWithFee = database.AddSale(database.MerchantId, database.OperatorId, 10m, new DateTime(2026, 9, 1));
        database.AddSale(database.MerchantId, database.OperatorId, 20m, new DateTime(2026, 9, 1));
        database.AddSettlementFee(transactionWithFee, 0.5m);
        await database.SaveAsync();

        var result = await database.Manager.GetTransactionDetailReport(
            new TransactionQueries.TransactionDetailReportQuery(
                EstateId,
                new TransactionDetailReportRequest {
                    StartDate = new DateTime(2026, 9, 1),
                    EndDate = new DateTime(2026, 9, 1)
                }),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Data.Summary.TransactionCount.ShouldBe(2);
        result.Data.Summary.TotalValue.ShouldBe(30m);
        result.Data.Summary.TotalFees.ShouldBe(0.5m);
        result.Data.Transactions.Sum(t => t.TotalFees).ShouldBe(0.5m);
        result.Data.Transactions.ShouldContain(t => t.TotalFees == 0m);
    }

    [Fact]
    public async Task GetTransactionDetailReport_IncludesBothDateBoundaries()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        database.AddSale(database.MerchantId, database.OperatorId, 10m, new DateTime(2026, 9, 1));
        database.AddSale(database.MerchantId, database.OperatorId, 20m, new DateTime(2026, 9, 2));
        database.AddSale(database.MerchantId, database.OperatorId, 30m, new DateTime(2026, 8, 31));
        await database.SaveAsync();

        var result = await database.Manager.GetTransactionDetailReport(
            new TransactionQueries.TransactionDetailReportQuery(
                EstateId,
                new TransactionDetailReportRequest {
                    StartDate = new DateTime(2026, 9, 1),
                    EndDate = new DateTime(2026, 9, 2)
                }),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Data.Summary.TransactionCount.ShouldBe(2);
        result.Data.Summary.TotalValue.ShouldBe(30m);
    }

    [Fact]
    public async Task GetTodaysSettlement_WhenNoSettlementRows_ReturnsZeroTotals()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();

        var result = await database.Manager.GetTodaysSettlement(
            new SettlementQueries.TodaysSettlementQuery(EstateId, DateTime.Today.AddDays(-1)),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Data.TodaysSettlementCount.ShouldBe(0);
        result.Data.TodaysPendingSettlementCount.ShouldBe(0);
        result.Data.ComparisonSettlementCount.ShouldBe(0);
        result.Data.ComparisonPendingSettlementCount.ShouldBe(0);
        result.Data.TodaysSettlementValue.ShouldBe(0m);
        result.Data.ComparisonSettlementValue.ShouldBe(0m);
    }

    [Fact]
    public async Task GetTodaysSales_WhenReadModelsAreEmpty_ReturnsZeroTotals()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        DateTime comparisonDate = DateTime.Today.AddDays(-1);

        var result = await database.Manager.GetTodaysSales(
            new TransactionQueries.TodaysSalesQuery(EstateId, 20, 10, comparisonDate),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Data.TodaysSalesCount.ShouldBe(0);
        result.Data.TodaysSalesValue.ShouldBe(0m);
        result.Data.TodaysAverageSalesValue.ShouldBe(0m);
        result.Data.ComparisonSalesCount.ShouldBe(0);
        result.Data.ComparisonSalesValue.ShouldBe(0m);
        result.Data.ComparisonAverageSalesValue.ShouldBe(0m);
    }

    [Fact]
    public async Task GetTodaysFailedSales_WhenReadModelsAreEmpty_ReturnsZeroTotals()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        DateTime comparisonDate = DateTime.Today.AddDays(-1);

        var result = await database.Manager.GetTodaysFailedSales(
            new TransactionQueries.TodaysFailedSales(EstateId, comparisonDate, "1000"),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Data.TodaysSalesCount.ShouldBe(0);
        result.Data.TodaysSalesValue.ShouldBe(0m);
        result.Data.TodaysAverageSalesValue.ShouldBe(0m);
        result.Data.ComparisonSalesCount.ShouldBe(0);
        result.Data.ComparisonSalesValue.ShouldBe(0m);
        result.Data.ComparisonAverageSalesValue.ShouldBe(0m);
    }

    [Fact]
    public async Task GetMerchantDailyPerformanceSummary_ReturnsMetricsTopProductAndRecentSales()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        database.AddSale(database.MerchantId, database.OperatorId, 10m, new DateTime(2026, 9, 1, 9, 0, 0), authorised: true);
        database.AddSale(database.MerchantId, database.OperatorId, 20m, new DateTime(2026, 9, 1, 10, 0, 0), authorised: false);
        database.AddSale(database.MerchantId, database.OperatorId, 30m, new DateTime(2026, 9, 2, 11, 0, 0), authorised: true);
        await database.SaveAsync();

        var result = await database.Manager.GetMerchantDailyPerformanceSummary(
            new TransactionQueries.MerchantDailyPerformanceSummaryQuery(
                EstateId,
                new MerchantDailyPerformanceSummaryRequest {
                    MerchantReportingId = 20,
                    StartDate = new DateTime(2026, 9, 1),
                    EndDate = new DateTime(2026, 9, 2)
                }),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Data.Metrics.Count.ShouldBe(9);
        result.Data.Metrics.Single(m => m.Title == "Total Sales Count").Value.ShouldBe(3);
        result.Data.Metrics.Single(m => m.Title == "Total Sales Value").Value.ShouldBe(60m);
        result.Data.Metrics.Single(m => m.Title == "Successful Sales Count").Value.ShouldBe(2);
        result.Data.Metrics.Single(m => m.Title == "Failed Sales Count").Value.ShouldBe(1);
        result.Data.Metrics.Single(m => m.Title == "Average Sales Count").Value.ShouldBe(1.5m);
        result.Data.Metrics.Single(m => m.Title == "Average Sales Value").Value.ShouldBe(20m);
        result.Data.Metrics.Single(m => m.Title == "Top Product Sales Count").Value.ShouldBe(3);
        result.Data.DrillDownTransactions.Count.ShouldBe(3);
        result.Data.DrillDownTransactions.First().Amount.ShouldBe(30m);
        result.Data.DrillDownTransactions.First().Status.ShouldBe("Successful");
    }

    [Fact]
    public async Task GetMerchantDailyPerformanceSummary_WhenNoTransactions_ReturnsBaseMetrics()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();

        var result = await database.Manager.GetMerchantDailyPerformanceSummary(
            new TransactionQueries.MerchantDailyPerformanceSummaryQuery(
                EstateId,
                new MerchantDailyPerformanceSummaryRequest {
                    MerchantReportingId = 20,
                    StartDate = new DateTime(2026, 9, 1),
                    EndDate = new DateTime(2026, 9, 2)
                }),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Data.Metrics.Count.ShouldBe(8);
        result.Data.Metrics.ShouldAllBe(metric => metric.Value == 0);
        result.Data.DrillDownTransactions.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetMerchants_AppliesAllQueryFilters()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        database.AddBalance(database.MerchantId, 10m);
        database.AddBalance(database.SecondMerchantId, 20m);
        await database.SaveAsync();

        var result = await database.Manager.GetMerchants(
            new MerchantQueries.GetMerchantsQuery(
                EstateId,
                new MerchantQueries.MerchantQueryOptions("Merchant 1", "M1", 2, "Region", "P1")),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Data.Count.ShouldBe(1);
        result.Data.Single().Name.ShouldBe("Merchant 1");
    }

    [Fact]
    public async Task GetMerchant_MapsAddressContactOpeningHoursAndBalance()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        database.AddBalance(database.MerchantId, 123.45m);
        database.AddMerchantRelatedData();
        await database.SaveAsync();

        var result = await database.Manager.GetMerchant(
            new MerchantQueries.GetMerchantQuery(EstateId, database.MerchantId),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Data.Name.ShouldBe("Merchant 1");
        result.Data.Reference.ShouldBe("M1");
        result.Data.Balance.ShouldBe(123.45m);
        result.Data.AddressLine1.ShouldBe("Address 1");
        result.Data.ContactName.ShouldBe("Contact 1");
        result.Data.ContactEmail.ShouldBe("one@example.test");
        result.Data.SettlementSchedule.ShouldBe(2);
    }

    [Fact]
    public async Task GetProductPerformanceReport_WithSalesMapsDetailsAndSummary()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        database.AddSale(database.MerchantId, database.OperatorId, 10m, new DateTime(2026, 9, 1));
        database.AddSale(database.MerchantId, database.OperatorId, 30m, new DateTime(2026, 9, 1));
        await database.SaveAsync();

        var result = await database.Manager.GetProductPerformanceReport(
            new TransactionQueries.ProductPerformanceQuery(EstateId, new DateTime(2026, 9, 1), new DateTime(2026, 9, 1)),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Data.ProductDetails.Count.ShouldBe(1);
        result.Data.ProductDetails.Single().ProductName.ShouldBe("Product 1");
        result.Data.ProductDetails.Single().TransactionCount.ShouldBe(2);
        result.Data.ProductDetails.Single().TransactionValue.ShouldBe(40m);
        result.Data.Summary.TotalCount.ShouldBe(2);
        result.Data.Summary.TotalValue.ShouldBe(40m);
        result.Data.Summary.AveragePerProduct.ShouldBe(40m);
        result.Data.Summary.TotalProducts.ShouldBe(1);
    }

    [Fact]
    public async Task GetMerchantOperators_MapsConfiguredRows()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        database.AddMerchantRelatedData();
        await database.SaveAsync();

        var operators = await database.Manager.GetMerchantOperators(
            new MerchantQueries.GetMerchantOperatorsQuery(EstateId, database.MerchantId), CancellationToken.None);

        operators.IsSuccess.ShouldBeTrue();
        operators.Data.Single().OperatorName.ShouldBe("Merchant Operator");
    }

    [Fact]
    public async Task GetMerchantContracts_MapsConfiguredRows()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        database.AddMerchantRelatedData();
        await database.SaveAsync();

        var contracts = await database.Manager.GetMerchantContracts(
            new MerchantQueries.GetMerchantContractsQuery(EstateId, database.MerchantId), CancellationToken.None);

        contracts.IsSuccess.ShouldBeTrue();
        contracts.Data.Single().ContractName.ShouldBe("Contract 1");
        contracts.Data.Single().ContractProducts.Single().ProductName.ShouldBe("Product 1");
    }

    [Fact]
    public async Task GetMerchantDevices_MapsConfiguredRows()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        database.AddMerchantRelatedData();
        await database.SaveAsync();

        var devices = await database.Manager.GetMerchantDevices(
            new MerchantQueries.GetMerchantDevicesQuery(EstateId, database.MerchantId), CancellationToken.None);

        devices.IsSuccess.ShouldBeTrue();
        devices.Data.Single().DeviceIdentifier.ShouldBe("device-1");
    }

    [Fact]
    public async Task GetMerchantOpeningHours_MapsAllDays()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        database.AddMerchantRelatedData();
        await database.SaveAsync();

        var openingHours = await database.Manager.GetMerchantOpeningHours(
            new MerchantQueries.GetMerchantOpeningHoursQuery(EstateId, database.MerchantId), CancellationToken.None);

        openingHours.IsSuccess.ShouldBeTrue();
        openingHours.Data.Count.ShouldBe(7);
        openingHours.Data.Single(x => x.DayOfWeek == DayOfWeek.Monday).OpeningTime.ShouldBe("08:00");
    }

    [Fact]
    public async Task GetMerchantSchedule_ParsesClosedDays()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        database.AddMerchantRelatedData();
        await database.SaveAsync();

        var schedule = await database.Manager.GetMerchantSchedule(
            new MerchantQueries.GetMerchantScheduleQuery(EstateId, database.MerchantId, 2026), CancellationToken.None);

        schedule.IsSuccess.ShouldBeTrue();
        schedule.Data.Year.ShouldBe(2026);
        schedule.Data.Months.Single().ClosedDays.ShouldBe([1, 15]);
    }

    [Fact]
    public async Task GetTodaysSalesByHour_WhenReadModelsAreEmpty_ReturnsEmptyResult()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();

        var result = await database.Manager.GetTodaysSalesByHour(
            new TransactionQueries.TodaysSalesByHour(EstateId, DateTime.Today.AddDays(-1)),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetContracts_MapsProductsAndOnlyEnabledFees()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        database.AddContractFees();
        await database.SaveAsync();

        var result = await database.Manager.GetContracts(
            new ContractQueries.GetContractsQuery(EstateId),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var contract = result.Data.Single();
        contract.OperatorName.ShouldBe("Operator 1");
        contract.Products.Single().ProductName.ShouldBe("Product 1");
        contract.Products.Single().TransactionFees.Count.ShouldBe(1);
        contract.Products.Single().TransactionFees.Single().Description.ShouldBe("Enabled fee");
    }

    [Fact]
    public async Task GetTransactionMixSummary_GroupsByStatusAndOrdersByValue()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        database.AddSale(database.MerchantId, database.OperatorId, 10m, new DateTime(2026, 9, 1), authorised: true);
        database.AddSale(database.MerchantId, database.OperatorId, 25m, new DateTime(2026, 9, 1), authorised: false);
        await database.SaveAsync();

        var result = await database.Manager.GetTransactionMixSummary(
            new TransactionQueries.TransactionMixSummaryQuery(
                EstateId,
                new TransactionMixSummaryRequest {
                    StartDate = new DateTime(2026, 9, 1),
                    EndDate = new DateTime(2026, 9, 1),
                    Breakdown = TransactionMixBreakdown.Status,
                    Measure = TransactionMixMeasure.Value,
                    TopN = 1
                }),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Data.TotalCount.ShouldBe(2);
        result.Data.TotalValue.ShouldBe(35m);
        result.Data.Groups.Count.ShouldBe(1);
        result.Data.Groups.Single().GroupName.ShouldBe("Declined");
        result.Data.Groups.Single().TransactionValue.ShouldBe(25m);
        result.Data.Transactions.Count.ShouldBe(2);
        result.Data.Transactions.First().Value.ShouldBe(25m);
    }

    [Fact]
    public async Task GetRecentActivityReceiptReport_AppliesMerchantAndSearchFilters()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        database.AddSale(database.MerchantId, database.OperatorId, 10m, new DateTime(2026, 9, 1, 9, 0, 0));
        database.AddSale(database.SecondMerchantId, database.OperatorId, 20m, new DateTime(2026, 9, 1, 10, 0, 0));
        await database.SaveAsync();

        var result = await database.Manager.GetRecentActivityReceiptReport(
            new TransactionQueries.GetRecentActivityReceiptReportQuery(
                EstateId,
                new GetRecentActivityReceiptReportRequest {
                    ReportDate = new DateTime(2026, 9, 1),
                    MerchantReportingId = 20,
                    SearchText = "Product 1"
                }),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Data.TotalCount.ShouldBe(1);
        result.Data.Items.Single().Amount.ShouldBe(10m);
    }

    [Fact]
    public async Task GetProductPerformanceReport_WhenNoSales_ReturnsZeroSummary()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();

        var result = await database.Manager.GetProductPerformanceReport(
            new TransactionQueries.ProductPerformanceQuery(EstateId, new DateTime(2026, 9, 1), new DateTime(2026, 9, 1)),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Data.ProductDetails.ShouldBeEmpty();
        result.Data.Summary.TotalCount.ShouldBe(0);
        result.Data.Summary.TotalValue.ShouldBe(0m);
    }

    [Fact]
    public async Task GetMerchants_WhenBalanceIsMissing_ReturnsFailureResult()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();

        var result = await database.Manager.GetMerchants(
            new MerchantQueries.GetMerchantsQuery(EstateId, new MerchantQueries.MerchantQueryOptions("", "", null, "", "")),
            CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task GetMerchants_WhenBalanceIsDuplicated_ReturnsFailureResult()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        database.AddBalance(database.MerchantId, 10m);
        database.AddBalance(database.MerchantId, 20m);
        await database.SaveAsync();

        var result = await database.Manager.GetMerchants(
            new MerchantQueries.GetMerchantsQuery(EstateId, new MerchantQueries.MerchantQueryOptions("", "", null, "", "")),
            CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly ServiceProvider services;
        private readonly EstateManagementContext seedContext;

        private TestDatabase(ServiceProvider services, EstateManagementContext seedContext, ReportingManager manager,
                             Guid merchantId, Guid secondMerchantId, Guid operatorId, Guid secondOperatorId, Guid contractId, Guid productId)
        {
            this.services = services;
            this.seedContext = seedContext;
            Manager = manager;
            MerchantId = merchantId;
            SecondMerchantId = secondMerchantId;
            OperatorId = operatorId;
            SecondOperatorId = secondOperatorId;
            ContractId = contractId;
            ProductId = productId;
        }

        public ReportingManager Manager { get; }
        public Guid MerchantId { get; }
        public Guid SecondMerchantId { get; }
        public Guid OperatorId { get; }
        public Guid SecondOperatorId { get; }
        public Guid ContractId { get; }
        public Guid ProductId { get; }

        public static async Task<TestDatabase> CreateAsync()
        {
            string databaseName = $"reporting-tests-{Guid.NewGuid()}";
            var options = new DbContextOptionsBuilder<EstateManagementContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;
            var seedContext = new EstateManagementContext(options);

            Guid estateId = EstateId;
            Guid merchantId = Guid.NewGuid();
            Guid secondMerchantId = Guid.NewGuid();
            Guid operatorId = Guid.NewGuid();
            Guid secondOperatorId = Guid.NewGuid();
            Guid contractId = Guid.NewGuid();
            Guid productId = Guid.NewGuid();

            seedContext.Estates.Add(new Db.Estate { EstateId = estateId, EstateReportingId = 1, Name = "Test Estate" });
            seedContext.Operators.AddRange(
                new Db.Operator { OperatorId = operatorId, EstateId = estateId, OperatorReportingId = 10, Name = "Operator 1" },
                new Db.Operator { OperatorId = secondOperatorId, EstateId = estateId, OperatorReportingId = 11, Name = "Operator 2" });
            seedContext.Merchants.Add(new Db.Merchant { MerchantId = merchantId, EstateId = estateId, MerchantReportingId = 20, Name = "Merchant 1", Reference = "M1", SettlementSchedule = 2 });
            seedContext.Merchants.Add(new Db.Merchant { MerchantId = secondMerchantId, EstateId = estateId, MerchantReportingId = 21, Name = "Merchant 2", Reference = "M2", SettlementSchedule = 3 });
            seedContext.MerchantAddresses.AddRange(
                new Db.MerchantAddress { AddressId = Guid.NewGuid(), MerchantId = merchantId, AddressLine1 = "Address 1", Region = "Region", PostalCode = "P1" },
                new Db.MerchantAddress { AddressId = Guid.NewGuid(), MerchantId = secondMerchantId, AddressLine1 = "Address 2", Region = "Region", PostalCode = "P2" });
            seedContext.MerchantContacts.AddRange(
                new Db.MerchantContact { ContactId = Guid.NewGuid(), MerchantId = merchantId, Name = "Contact 1", EmailAddress = "one@example.test" },
                new Db.MerchantContact { ContactId = Guid.NewGuid(), MerchantId = secondMerchantId, Name = "Contact 2", EmailAddress = "two@example.test" });
            seedContext.Contracts.Add(new Db.Contract { ContractId = contractId, EstateId = estateId, OperatorId = operatorId, ContractReportingId = 30, Description = "Contract 1" });
            seedContext.ContractProducts.Add(new Db.ContractProduct {
                ContractProductId = productId,
                ContractId = contractId,
                ContractProductReportingId = 40,
                ProductName = "Product 1",
                DisplayText = "Product 1",
                ProductType = 1,
                Value = 1m
            });
            await seedContext.SaveChangesAsync();

            var services = new ServiceCollection()
                .AddDbContext<EstateManagementContext>(builder => builder.UseInMemoryDatabase(databaseName))
                .BuildServiceProvider();
            var resolver = new IDbContextResolverImposter<EstateManagementContext>();
            resolver.Resolve(Arg<string>.Any(), Arg<string>.Any())
                .Returns(new ResolvedDbContext<EstateManagementContext>(services.CreateScope()));

            return new TestDatabase(services, seedContext, new ReportingManager(resolver.Instance()), merchantId, secondMerchantId, operatorId,
                secondOperatorId, contractId, productId);
        }

        public Guid AddSale(Guid merchantId, Guid operatorId, decimal amount, DateTime date, bool authorised = true)
        {
            Guid transactionId = Guid.NewGuid();
            seedContext.Transactions.Add(new Db.Transaction {
                TransactionId = transactionId,
                MerchantId = merchantId,
                OperatorId = operatorId,
                ContractId = ContractId,
                ContractProductId = ProductId,
                TransactionType = "Sale",
                TransactionDate = date.Date,
                TransactionDateTime = date,
                TransactionTime = date.TimeOfDay,
                TransactionAmount = amount,
                IsAuthorised = authorised,
                IsCompleted = true,
                ResponseCode = authorised ? "0000" : "1000",
                TransactionNumber = "0001"
            });
            return transactionId;
        }

        public void AddContractFees()
        {
            seedContext.ContractProductTransactionFees.AddRange(
                new Db.ContractProductTransactionFee {
                    ContractProductTransactionFeeId = Guid.NewGuid(),
                    ContractProductTransactionFeeReportingId = 50,
                    ContractProductId = ProductId,
                    Description = "Enabled fee",
                    Value = 0.5m,
                    CalculationType = 1,
                    FeeType = 1,
                    IsEnabled = true
                },
                new Db.ContractProductTransactionFee {
                    ContractProductTransactionFeeId = Guid.NewGuid(),
                    ContractProductTransactionFeeReportingId = 51,
                    ContractProductId = ProductId,
                    Description = "Disabled fee",
                    Value = 0.25m,
                    CalculationType = 1,
                    FeeType = 1,
                    IsEnabled = false
                });
        }

        public void AddMerchantRelatedData()
        {
            seedContext.MerchantOperators.Add(new Db.MerchantOperator {
                MerchantId = MerchantId,
                OperatorId = OperatorId,
                MerchantNumber = "merchant-number",
                Name = "Merchant Operator",
                TerminalNumber = "terminal-1",
                IsDeleted = false
            });
            seedContext.MerchantContracts.Add(new Db.MerchantContract {
                MerchantId = MerchantId,
                ContractId = ContractId,
                IsDeleted = false
            });
            seedContext.MerchantDevices.Add(new Db.MerchantDevice {
                MerchantId = MerchantId,
                DeviceId = Guid.NewGuid(),
                DeviceIdentifier = "device-1",
                CreatedDateTime = new DateTime(2026, 9, 1),
                IsEnabled = true
            });
            seedContext.MerchantOpeningHours.Add(new Db.MerchantOpeningHours {
                MerchantId = MerchantId,
                MondayOpening = "08:00",
                MondayClosing = "17:00",
                TuesdayOpening = "08:00",
                TuesdayClosing = "17:00",
                WednesdayOpening = "08:00",
                WednesdayClosing = "17:00",
                ThursdayOpening = "08:00",
                ThursdayClosing = "17:00",
                FridayOpening = "08:00",
                FridayClosing = "17:00",
                SaturdayOpening = "09:00",
                SaturdayClosing = "13:00",
                SundayOpening = "Closed",
                SundayClosing = "Closed"
            });
            Guid scheduleId = Guid.NewGuid();
            seedContext.MerchantSchedules.Add(new Db.MerchantSchedule {
                MerchantScheduleId = scheduleId,
                EstateId = EstateId,
                MerchantId = MerchantId,
                Year = 2026
            });
            seedContext.MerchantScheduleMonths.Add(new Db.MerchantScheduleMonth {
                MerchantScheduleId = scheduleId,
                Month = 9,
                ClosedDays = "1,15,invalid"
            });
        }

        public void AddBalance(Guid merchantId, decimal balance)
        {
            seedContext.MerchantBalanceProjectionState.Add(new BalanceState {
                EstateId = Guid.NewGuid(),
                MerchantId = merchantId,
                MerchantName = merchantId == MerchantId ? "Merchant 1" : "Merchant 2",
                Balance = balance,
                LastSale = new DateTime(2026, 9, 1),
                Timestamp = Array.Empty<byte>()
            });
        }

        public void AddCalendar(DateTime date)
        {
            seedContext.Calendar.Add(new Db.Calendar {
                Date = date,
                DayOfWeek = date.DayOfWeek.ToString(),
                DayOfWeekShort = date.DayOfWeek.ToString()[..3],
                MonthNameLong = date.ToString("MMMM"),
                MonthNameShort = date.ToString("MMM"),
                MonthNumber = date.Month,
                WeekNumberString = "35",
                YearWeekNumber = $"{date.Year}35",
                Year = date.Year
            });
        }

        public void AddEstateOperator()
        {
            seedContext.EstateOperators.Add(new Db.EstateOperator {
                EstateId = EstateId,
                OperatorId = OperatorId
            });
        }

        public void AddEstateUser()
        {
            seedContext.EstateSecurityUsers.Add(new Db.EstateSecurityUser {
                EstateId = EstateId,
                SecurityUserId = Guid.NewGuid(),
                EmailAddress = "user@example.test"
            });
        }

        public Guid AddFileImportData()
        {
            Guid logId = Guid.NewGuid();
            Guid fileId = Guid.NewGuid();
            Guid userId = Guid.NewGuid();
            Guid profileId = Guid.NewGuid();

            seedContext.FileImportLogs.Add(new Db.FileImportLog {
                FileImportLogId = logId,
                EstateId = EstateId,
                ImportLogDate = DateTime.Today,
                ImportLogDateTime = DateTime.Today.AddHours(1)
            });
            seedContext.EstateSecurityUsers.Add(new Db.EstateSecurityUser {
                EstateId = EstateId,
                SecurityUserId = userId,
                EmailAddress = "uploader@example.test"
            });
            seedContext.Files.Add(new Db.File {
                FileId = fileId,
                FileImportLogId = logId,
                FileLocation = "import.csv",
                FileProfileId = profileId,
                FileReceivedDateTime = DateTime.Today,
                UserId = userId,
                MerchantId = MerchantId
            });
            seedContext.FileLines.Add(new Db.FileLine {
                FileId = fileId,
                LineNumber = 1,
                FileLineData = "line-data",
                Status = "OK"
            });

            return logId;
        }

        public void AddSettlementFee(Guid transactionId, decimal feeValue)
        {
            Guid settlementId = Guid.NewGuid();
            seedContext.Settlements.Add(new Db.Settlement {
                SettlementId = settlementId,
                EstateId = EstateId,
                MerchantId = MerchantId,
                SettlementDate = new DateTime(2026, 9, 1),
                ProcessingStarted = true,
                IsCompleted = true,
                ProcessingStartedDateTIme = new DateTime(2026, 9, 1)
            });
            seedContext.MerchantSettlementFees.Add(new Db.MerchantSettlementFee {
                SettlementId = settlementId,
                TransactionId = transactionId,
                MerchantId = MerchantId,
                FeeValue = feeValue,
                CalculatedValue = feeValue,
                IsSettled = true,
                FeeCalculatedDateTime = new DateTime(2026, 9, 1)
            });
        }

        public Task SaveAsync() => seedContext.SaveChangesAsync();

        public async ValueTask DisposeAsync()
        {
            await seedContext.DisposeAsync();
            await services.DisposeAsync();
        }
    }
}
