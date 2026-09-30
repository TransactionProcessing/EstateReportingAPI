using System;
using Shouldly;
using Xunit;

namespace EstateReportingAPI.BusinessLogic.UnitTests;

public sealed class ReportQueryValidationTests
{
    [Fact]
    public void ValidateDateRange_AllowsThirtyInclusiveDays()
    {
        ReportQueryValidation.ValidateDateRange(
                new DateTime(2026, 9, 1),
                new DateTime(2026, 9, 30))
            .ShouldBeNull();
    }

    [Fact]
    public void ValidateDateRange_RejectsMoreThanThirtyInclusiveDays()
    {
        ReportQueryValidation.ValidateDateRange(
                new DateTime(2026, 9, 1),
                new DateTime(2026, 10, 1))
            .ShouldBe("date range must not exceed 30 inclusive calendar days.");
    }

    [Fact]
    public void ValidateDateRange_RejectsReversedDates()
    {
        ReportQueryValidation.ValidateDateRange(
                new DateTime(2026, 9, 2),
                new DateTime(2026, 9, 1))
            .ShouldBe("startDate must be less than or equal to endDate.");
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 50)]
    public void ValidatePaging_AllowsPageNumbersAndSizesWithinBounds(int pageNumber, int pageSize)
    {
        ReportQueryValidation.ValidatePaging(pageNumber, pageSize).ShouldBeNull();
    }

    [Theory]
    [InlineData(0, 1, "pageNumber must be greater than or equal to 1.")]
    [InlineData(1, 0, "pageSize must be between 1 and 50.")]
    [InlineData(1, 51, "pageSize must be between 1 and 50.")]
    [InlineData(int.MaxValue, 50, "pageNumber is too large for this report.")]
    public void ValidatePaging_RejectsInvalidValues(int pageNumber, int pageSize, string expected)
    {
        ReportQueryValidation.ValidatePaging(pageNumber, pageSize).ShouldBe(expected);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(20)]
    public void ValidateTopN_AllowsValuesWithinBounds(int topN)
    {
        ReportQueryValidation.ValidateTopN(topN).ShouldBeNull();
    }

    [Theory]
    [InlineData(0, "topN must be between 1 and 20.")]
    [InlineData(21, "topN must be between 1 and 20.")]
    public void ValidateTopN_RejectsValuesOutsideBounds(int topN, string expected)
    {
        ReportQueryValidation.ValidateTopN(topN).ShouldBe(expected);
    }

    [Fact]
    public void ValidateFilterList_AllowsFiftyIds()
    {
        ReportQueryValidation.ValidateFilterList("merchants", new int[50]).ShouldBeNull();
    }

    [Fact]
    public void ValidateFilterList_RejectsMoreThanFiftyIds()
    {
        ReportQueryValidation.ValidateFilterList("merchants", new int[51])
            .ShouldBe("merchants must contain no more than 50 values.");
    }
}
