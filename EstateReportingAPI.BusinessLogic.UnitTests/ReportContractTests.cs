using EstateReportingAPI.Models;
using Shouldly;
using Xunit;

namespace EstateReportingAPI.BusinessLogic.UnitTests;

public sealed class ReportContractTests
{
    [Fact]
    public void PagedRequestDefaultsUseApprovedPageSizes()
    {
        new TransactionDetailReportRequest().PageSize.ShouldBe(ReportQueryLimits.DefaultPageSize);
        new TransactionMixSummaryRequest().PageSize.ShouldBe(ReportQueryLimits.DefaultPageSize);
        new TransactionSummaryByMerchantRequest().PageSize.ShouldBe(ReportQueryLimits.DefaultPageSize);
        new TransactionSummaryByOperatorRequest().PageSize.ShouldBe(ReportQueryLimits.DefaultPageSize);
        new GetRecentActivityReceiptReportRequest().PageSize.ShouldBe(ReportQueryLimits.RecentActivityDefaultPageSize);
    }

    [Fact]
    public void PagedResponsesExposeSharedPaginationMetadata()
    {
        new TransactionDetailReportResponse().Pagination.ShouldNotBeNull();
        new TransactionMixSummaryResponse().Pagination.ShouldNotBeNull();
        new TransactionSummaryByMerchantResponse().Pagination.ShouldNotBeNull();
        new TransactionSummaryByOperatorResponse().Pagination.ShouldNotBeNull();
        new ProductPerformanceResponse().Pagination.ShouldNotBeNull();
        new GetRecentActivityReceiptReportResponse().Pagination.ShouldNotBeNull();
    }
}
