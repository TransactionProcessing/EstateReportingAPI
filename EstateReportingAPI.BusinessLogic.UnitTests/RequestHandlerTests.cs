using EstateReportingAPI.BusinessLogic;
using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.BusinessLogic.RequestHandlers;
using EstateReportingAPI.Models;
using Imposter.Abstractions;
using Shouldly;
using SimpleResults;

[assembly: GenerateImposter(typeof(IReportingManager))]
[assembly: GenerateImposter(typeof(ITransactionReportingService))]
[assembly: GenerateImposter(typeof(IEstateReportingService))]
[assembly: GenerateImposter(typeof(ISettlementReportingService))]

namespace EstateReportingAPI.BusinessLogic.UnitTests;

public sealed class RequestHandlerTests
{
    [Fact]
    public async Task SettlementRequestHandler_ForwardsRequestAndCancellationToken()
    {
        var manager = new ISettlementReportingServiceImposter();
        var expected = Result.Success(new TodaysSettlement());
        var request = new SettlementQueries.TodaysSettlementQuery(Guid.NewGuid(), DateTime.Today);
        var cancellationToken = new CancellationTokenSource().Token;
        manager.GetTodaysSettlement(Arg<SettlementQueries.TodaysSettlementQuery>.Any(), Arg<CancellationToken>.Any())
            .Returns(Task.FromResult(expected));

        var actual = await new SettlementRequestHandler(manager.Instance()).Handle(request, cancellationToken);

        actual.ShouldBe(expected);
    }

    [Fact]
    public async Task CalendarRequestHandler_ForwardsRequest()
    {
        var manager = new IEstateReportingServiceImposter();
        var request = new CalendarQueries.GetYearsQuery(Guid.NewGuid());
        manager.GetCalendarYears(Arg<CalendarQueries.GetYearsQuery>.Any(), Arg<CancellationToken>.Any())
            .Returns(Task.FromResult(Result.Success(new List<int>())));

        var actual = await new CalendarRequestHandler(manager.Instance()).Handle(request, CancellationToken.None);

        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task CalendarRequestHandler_ForwardsAllDatesRequest()
    {
        var manager = new IEstateReportingServiceImposter();
        var request = new CalendarQueries.GetAllDatesQuery(Guid.NewGuid());
        manager.GetCalendarDates(Arg<CalendarQueries.GetAllDatesQuery>.Any(), Arg<CancellationToken>.Any())
            .Returns(Task.FromResult(Result.Success(new List<Calendar>())));

        var actual = await new CalendarRequestHandler(manager.Instance()).Handle(request, CancellationToken.None);

        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task CalendarRequestHandler_ForwardsComparisonDatesRequest()
    {
        var manager = new IEstateReportingServiceImposter();
        var request = new CalendarQueries.GetComparisonDatesQuery(Guid.NewGuid());
        manager.GetCalendarComparisonDates(Arg<CalendarQueries.GetComparisonDatesQuery>.Any(), Arg<CancellationToken>.Any())
            .Returns(Task.FromResult(Result.Success(new List<Calendar>())));

        var actual = await new CalendarRequestHandler(manager.Instance()).Handle(request, CancellationToken.None);

        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task CalendarRequestHandler_WhenComparisonDatesFails_ReturnsFailure()
    {
        var manager = new IEstateReportingServiceImposter();
        var request = new CalendarQueries.GetComparisonDatesQuery(Guid.NewGuid());
        manager.GetCalendarComparisonDates(Arg<CalendarQueries.GetComparisonDatesQuery>.Any(), Arg<CancellationToken>.Any())
            .Returns(Task.FromResult<Result<List<Calendar>>>(Result.Failure("Calendar lookup failed.")));

        var actual = await new CalendarRequestHandler(manager.Instance()).Handle(request, CancellationToken.None);

        actual.IsFailed.ShouldBeTrue();
    }

    [Fact]
    public async Task ContractRequestHandler_ForwardsRequest()
    {
        var manager = new IReportingManagerImposter();
        var request = new ContractQueries.GetContractsQuery(Guid.NewGuid());
        manager.GetContracts(Arg<ContractQueries.GetContractsQuery>.Any(), Arg<CancellationToken>.Any())
            .Returns(Task.FromResult(Result.Success(new List<Contract>())));

        var actual = await new ContractRequestHandler(manager.Instance()).Handle(request, CancellationToken.None);

        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task ContractRequestHandler_ForwardsRecentContractsRequest()
    {
        var manager = new IReportingManagerImposter();
        var request = new ContractQueries.GetRecentContractsQuery(Guid.NewGuid());
        manager.GetRecentContracts(Arg<ContractQueries.GetRecentContractsQuery>.Any(), Arg<CancellationToken>.Any())
            .Returns(Task.FromResult(Result.Success(new List<Contract>())));

        var actual = await new ContractRequestHandler(manager.Instance()).Handle(request, CancellationToken.None);

        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task ContractRequestHandler_ForwardsContractRequest()
    {
        var manager = new IReportingManagerImposter();
        var request = new ContractQueries.GetContractQuery(Guid.NewGuid(), Guid.NewGuid());
        manager.GetContract(Arg<ContractQueries.GetContractQuery>.Any(), Arg<CancellationToken>.Any())
            .Returns(Task.FromResult(Result.Success(new Contract())));

        var actual = await new ContractRequestHandler(manager.Instance()).Handle(request, CancellationToken.None);

        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task EstateRequestHandler_ForwardsRequest()
    {
        var manager = new IEstateReportingServiceImposter();
        var request = new EstateQueries.GetEstateQuery(Guid.NewGuid());
        manager.GetEstate(Arg<EstateQueries.GetEstateQuery>.Any(), Arg<CancellationToken>.Any())
            .Returns(Task.FromResult(Result.Success(new Estate())));

        var actual = await new EstateRequestHandler(manager.Instance()).Handle(request, CancellationToken.None);

        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task EstateRequestHandler_ForwardsEstateOperatorsRequest()
    {
        var manager = new IEstateReportingServiceImposter();
        var request = new EstateQueries.GetEstateOperatorsQuery(Guid.NewGuid());
        manager.GetEstateOperators(Arg<EstateQueries.GetEstateOperatorsQuery>.Any(), Arg<CancellationToken>.Any())
            .Returns(Task.FromResult(Result.Success(new List<EstateOperator>())));

        var actual = await new EstateRequestHandler(manager.Instance()).Handle(request, CancellationToken.None);

        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task FileImportLogRequestHandler_ForwardsRequest()
    {
        var manager = new IReportingManagerImposter();
        var request = new FileImportLogQueries.GetFileImportLogListQuery(Guid.NewGuid(), null, DateTime.Today, DateTime.Today);
        manager.GetFileImportLogList(Arg<FileImportLogQueries.GetFileImportLogListQuery>.Any(), Arg<CancellationToken>.Any())
            .Returns(Task.FromResult(Result.Success(new List<FileImportLog>())));

        var actual = await new FileImportLogRequestHandler(manager.Instance()).Handle(request, CancellationToken.None);

        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task FileImportLogRequestHandler_ForwardsSingleLogRequest()
    {
        var manager = new IReportingManagerImposter();
        var request = new FileImportLogQueries.GetFileImportLogQuery(Guid.NewGuid(), null, Guid.NewGuid());
        manager.GetFileImportLog(Arg<FileImportLogQueries.GetFileImportLogQuery>.Any(), Arg<CancellationToken>.Any())
            .Returns(Task.FromResult(Result.Success(new FileImportLog())));

        var actual = await new FileImportLogRequestHandler(manager.Instance()).Handle(request, CancellationToken.None);

        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task FileProfileConfigurationRequestHandler_ForwardsRequest()
    {
        var manager = new IReportingManagerImposter();
        var request = new FileProfileConfigurationQueries.GetFileProfileConfigurationListQuery(Guid.NewGuid());
        manager.GetFileProfileConfigurationList(Arg<FileProfileConfigurationQueries.GetFileProfileConfigurationListQuery>.Any(), Arg<CancellationToken>.Any())
            .Returns(Task.FromResult(Result.Success(new List<FileProfileConfiguration>())));

        var actual = await new FileProfileConfigurationRequestHandler(manager.Instance()).Handle(request, CancellationToken.None);

        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task MerchantRequestHandler_ForwardsRequest()
    {
        var manager = new IReportingManagerImposter();
        var request = new MerchantQueries.GetMerchantQuery(Guid.NewGuid(), Guid.NewGuid());
        manager.GetMerchant(Arg<MerchantQueries.GetMerchantQuery>.Any(), Arg<CancellationToken>.Any())
            .Returns(Task.FromResult(Result.Success(new Merchant())));

        var actual = await new MerchantRequestHandler(manager.Instance()).Handle(request, CancellationToken.None);

        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task MerchantRequestHandler_ForwardsRecentMerchantsRequest()
    {
        var manager = new IReportingManagerImposter();
        var request = new MerchantQueries.GetRecentMerchantsQuery(Guid.NewGuid());
        manager.GetRecentMerchants(Arg<MerchantQueries.GetRecentMerchantsQuery>.Any(), Arg<CancellationToken>.Any())
            .Returns(Task.FromResult(Result.Success(new List<Merchant>())));

        var actual = await new MerchantRequestHandler(manager.Instance()).Handle(request, CancellationToken.None);

        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task MerchantRequestHandler_ForwardsTransactionKpisRequest()
    {
        var manager = new IReportingManagerImposter();
        var request = new MerchantQueries.GetTransactionKpisQuery(Guid.NewGuid());
        manager.GetMerchantsTransactionKpis(Arg<MerchantQueries.GetTransactionKpisQuery>.Any(), Arg<CancellationToken>.Any())
            .Returns(Task.FromResult(Result.Success(new MerchantKpi())));

        var actual = await new MerchantRequestHandler(manager.Instance()).Handle(request, CancellationToken.None);

        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task MerchantRequestHandler_ForwardsMerchantsRequest()
    {
        var manager = new IReportingManagerImposter();
        var request = new MerchantQueries.GetMerchantsQuery(Guid.NewGuid(), new MerchantQueries.MerchantQueryOptions("", "", null, "", ""));
        manager.GetMerchants(Arg<MerchantQueries.GetMerchantsQuery>.Any(), Arg<CancellationToken>.Any())
            .Returns(Task.FromResult(Result.Success(new List<Merchant>())));

        var actual = await new MerchantRequestHandler(manager.Instance()).Handle(request, CancellationToken.None);

        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task MerchantRequestHandler_ForwardsMerchantOperatorsRequest()
    {
        var manager = new IReportingManagerImposter();
        var request = new MerchantQueries.GetMerchantOperatorsQuery(Guid.NewGuid(), Guid.NewGuid());
        manager.GetMerchantOperators(Arg<MerchantQueries.GetMerchantOperatorsQuery>.Any(), Arg<CancellationToken>.Any())
            .Returns(Task.FromResult(Result.Success(new List<MerchantOperator>())));

        var actual = await new MerchantRequestHandler(manager.Instance()).Handle(request, CancellationToken.None);

        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task MerchantRequestHandler_ForwardsMerchantContractsRequest()
    {
        var manager = new IReportingManagerImposter();
        var request = new MerchantQueries.GetMerchantContractsQuery(Guid.NewGuid(), Guid.NewGuid());
        manager.GetMerchantContracts(Arg<MerchantQueries.GetMerchantContractsQuery>.Any(), Arg<CancellationToken>.Any())
            .Returns(Task.FromResult(Result.Success(new List<MerchantContract>())));

        var actual = await new MerchantRequestHandler(manager.Instance()).Handle(request, CancellationToken.None);

        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task MerchantRequestHandler_ForwardsMerchantDevicesRequest()
    {
        var manager = new IReportingManagerImposter();
        var request = new MerchantQueries.GetMerchantDevicesQuery(Guid.NewGuid(), Guid.NewGuid());
        manager.GetMerchantDevices(Arg<MerchantQueries.GetMerchantDevicesQuery>.Any(), Arg<CancellationToken>.Any())
            .Returns(Task.FromResult(Result.Success(new List<MerchantDevice>())));

        var actual = await new MerchantRequestHandler(manager.Instance()).Handle(request, CancellationToken.None);

        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task MerchantRequestHandler_ForwardsMerchantOpeningHoursRequest()
    {
        var manager = new IReportingManagerImposter();
        var request = new MerchantQueries.GetMerchantOpeningHoursQuery(Guid.NewGuid(), Guid.NewGuid());
        manager.GetMerchantOpeningHours(Arg<MerchantQueries.GetMerchantOpeningHoursQuery>.Any(), Arg<CancellationToken>.Any())
            .Returns(Task.FromResult(Result.Success(new List<MerchantOpeningHour>())));

        var actual = await new MerchantRequestHandler(manager.Instance()).Handle(request, CancellationToken.None);

        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task MerchantRequestHandler_ForwardsMerchantScheduleRequest()
    {
        var manager = new IReportingManagerImposter();
        var request = new MerchantQueries.GetMerchantScheduleQuery(Guid.NewGuid(), Guid.NewGuid(), 2026);
        manager.GetMerchantSchedule(Arg<MerchantQueries.GetMerchantScheduleQuery>.Any(), Arg<CancellationToken>.Any())
            .Returns(Task.FromResult(Result.Success(new MerchantScheduleResponse())));

        var actual = await new MerchantRequestHandler(manager.Instance()).Handle(request, CancellationToken.None);

        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task OperatorRequestHandler_ForwardsRequest()
    {
        var manager = new IReportingManagerImposter();
        var request = new OperatorQueries.GetOperatorsQuery(Guid.NewGuid());
        manager.GetOperators(Arg<OperatorQueries.GetOperatorsQuery>.Any(), Arg<CancellationToken>.Any())
            .Returns(Task.FromResult(Result.Success(new List<Operator>())));

        var actual = await new OperatorRequestHandler(manager.Instance()).Handle(request, CancellationToken.None);

        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task OperatorRequestHandler_ForwardsOperatorRequest()
    {
        var manager = new IReportingManagerImposter();
        var request = new OperatorQueries.GetOperatorQuery(Guid.NewGuid(), Guid.NewGuid());
        manager.GetOperator(Arg<OperatorQueries.GetOperatorQuery>.Any(), Arg<CancellationToken>.Any())
            .Returns(Task.FromResult(Result.Success(new Operator())));

        var actual = await new OperatorRequestHandler(manager.Instance()).Handle(request, CancellationToken.None);

        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task TransactionRequestHandler_ForwardsRequest()
    {
        var manager = new ITransactionReportingServiceImposter();
        var request = new TransactionQueries.ProductPerformanceQuery(Guid.NewGuid(), DateTime.Today, DateTime.Today);
        manager.GetProductPerformanceReport(Arg<TransactionQueries.ProductPerformanceQuery>.Any(), Arg<CancellationToken>.Any())
            .Returns(Task.FromResult(Result.Success(new ProductPerformanceResponse())));

        var actual = await new TransactionRequestHandler(manager.Instance()).Handle(request, CancellationToken.None);

        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task TransactionRequestHandler_ForwardsFailedSalesRequest()
    {
        var manager = new ITransactionReportingServiceImposter();
        var request = new TransactionQueries.TodaysFailedSales(Guid.NewGuid(), DateTime.Today, "1000");
        manager.GetTodaysFailedSales(Arg<TransactionQueries.TodaysFailedSales>.Any(), Arg<CancellationToken>.Any())
            .Returns(Task.FromResult(Result.Success(new TodaysSales())));

        var actual = await new TransactionRequestHandler(manager.Instance()).Handle(request, CancellationToken.None);

        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task TransactionRequestHandler_ForwardsTodaysSalesRequest()
    {
        var manager = new ITransactionReportingServiceImposter();
        var request = new TransactionQueries.TodaysSalesQuery(Guid.NewGuid(), 1, 2, DateTime.Today);
        manager.GetTodaysSales(Arg<TransactionQueries.TodaysSalesQuery>.Any(), Arg<CancellationToken>.Any())
            .Returns(Task.FromResult(Result.Success(new TodaysSales())));

        var actual = await new TransactionRequestHandler(manager.Instance()).Handle(request, CancellationToken.None);

        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task TransactionRequestHandler_ForwardsTransactionDetailRequest()
    {
        var manager = new ITransactionReportingServiceImposter();
        var request = new TransactionQueries.TransactionDetailReportQuery(Guid.NewGuid(), new TransactionDetailReportRequest());
        manager.GetTransactionDetailReport(Arg<TransactionQueries.TransactionDetailReportQuery>.Any(), Arg<CancellationToken>.Any())
            .Returns(Task.FromResult(Result.Success(new TransactionDetailReportResponse())));

        var actual = await new TransactionRequestHandler(manager.Instance()).Handle(request, CancellationToken.None);

        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task TransactionRequestHandler_ForwardsSummaryByMerchantRequest()
    {
        var manager = new ITransactionReportingServiceImposter();
        var request = new TransactionQueries.TransactionSummaryByMerchantQuery(Guid.NewGuid(), new TransactionSummaryByMerchantRequest());
        manager.GetTransactionSummaryByMerchantReport(Arg<TransactionQueries.TransactionSummaryByMerchantQuery>.Any(), Arg<CancellationToken>.Any())
            .Returns(Task.FromResult(Result.Success(new TransactionSummaryByMerchantResponse())));

        var actual = await new TransactionRequestHandler(manager.Instance()).Handle(request, CancellationToken.None);

        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task TransactionRequestHandler_ForwardsSummaryByOperatorRequest()
    {
        var manager = new ITransactionReportingServiceImposter();
        var request = new TransactionQueries.TransactionSummaryByOperatorQuery(Guid.NewGuid(), new TransactionSummaryByOperatorRequest());
        manager.GetTransactionSummaryByOperatorReport(Arg<TransactionQueries.TransactionSummaryByOperatorQuery>.Any(), Arg<CancellationToken>.Any())
            .Returns(Task.FromResult(Result.Success(new TransactionSummaryByOperatorResponse())));

        var actual = await new TransactionRequestHandler(manager.Instance()).Handle(request, CancellationToken.None);

        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task TransactionRequestHandler_ForwardsTransactionMixRequest()
    {
        var manager = new ITransactionReportingServiceImposter();
        var request = new TransactionQueries.TransactionMixSummaryQuery(Guid.NewGuid(), new TransactionMixSummaryRequest());
        manager.GetTransactionMixSummary(Arg<TransactionQueries.TransactionMixSummaryQuery>.Any(), Arg<CancellationToken>.Any())
            .Returns(Task.FromResult(Result.Success(new TransactionMixSummaryResponse())));

        var actual = await new TransactionRequestHandler(manager.Instance()).Handle(request, CancellationToken.None);

        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task TransactionRequestHandler_ForwardsRecentActivityRequest()
    {
        var manager = new ITransactionReportingServiceImposter();
        var request = new TransactionQueries.GetRecentActivityReceiptReportQuery(Guid.NewGuid(), new GetRecentActivityReceiptReportRequest());
        manager.GetRecentActivityReceiptReport(Arg<TransactionQueries.GetRecentActivityReceiptReportQuery>.Any(), Arg<CancellationToken>.Any())
            .Returns(Task.FromResult(Result.Success(new GetRecentActivityReceiptReportResponse())));

        var actual = await new TransactionRequestHandler(manager.Instance()).Handle(request, CancellationToken.None);

        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task TransactionRequestHandler_ForwardsSalesByHourRequest()
    {
        var manager = new ITransactionReportingServiceImposter();
        var request = new TransactionQueries.TodaysSalesByHour(Guid.NewGuid(), DateTime.Today);
        manager.GetTodaysSalesByHour(Arg<TransactionQueries.TodaysSalesByHour>.Any(), Arg<CancellationToken>.Any())
            .Returns(Task.FromResult(Result.Success(new List<TodaysSalesByHour>())));

        var actual = await new TransactionRequestHandler(manager.Instance()).Handle(request, CancellationToken.None);

        actual.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task TransactionRequestHandler_ForwardsMerchantDailyPerformanceRequest()
    {
        var manager = new ITransactionReportingServiceImposter();
        var request = new TransactionQueries.MerchantDailyPerformanceSummaryQuery(Guid.NewGuid(), new MerchantDailyPerformanceSummaryRequest());
        manager.GetMerchantDailyPerformanceSummary(Arg<TransactionQueries.MerchantDailyPerformanceSummaryQuery>.Any(), Arg<CancellationToken>.Any())
            .Returns(Task.FromResult(Result.Success(new MerchantDailyPerformanceSummaryResponse())));

        var actual = await new TransactionRequestHandler(manager.Instance()).Handle(request, CancellationToken.None);

        actual.IsSuccess.ShouldBeTrue();
    }
}
