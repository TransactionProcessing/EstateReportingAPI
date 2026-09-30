using EstateReportingAPI.BusinessLogic;
using Shouldly;

namespace EstateReportingAPI.BusinessLogic.UnitTests;

public sealed class ReportingDatePolicyTests
{
    [Fact]
    public void Today_UsesEuropeLondonAtUtcMidnightBoundary()
    {
        ReportingDatePolicy policy = CreatePolicy(new DateTimeOffset(2026, 10, 25, 0, 30, 0, TimeSpan.Zero));

        policy.Today.ShouldBe(new DateOnly(2026, 10, 25));
    }

    [Fact]
    public void GetRange_UsesInclusiveStartAndExclusiveDayAfterEnd()
    {
        ReportingDatePolicy policy = CreatePolicy(new DateTimeOffset(2026, 9, 30, 12, 0, 0, TimeSpan.Zero));

        ReportingDateRange range = policy.GetRange(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        range.StartInclusive.ShouldBe(new DateTime(2026, 9, 1));
        range.EndExclusive.ShouldBe(new DateTime(2026, 10, 1));
    }

    [Fact]
    public void Today_UsesBritishSummerTimeWhenUtcDateDiffersFromLocalDate()
    {
        ReportingDatePolicy policy = CreatePolicy(new DateTimeOffset(2026, 6, 30, 23, 30, 0, TimeSpan.Zero));

        policy.Today.ShouldBe(new DateOnly(2026, 7, 1));
    }

    [Fact]
    public void ToDateTimeOffset_AppliesBritishSummerTimeOffset()
    {
        ReportingDatePolicy policy = CreatePolicy(new DateTimeOffset(2026, 6, 30, 12, 0, 0, TimeSpan.Zero));

        policy.ToDateTimeOffset(new DateTime(2026, 6, 30, 13, 0, 0)).Offset.ShouldBe(TimeSpan.FromHours(1));
    }

    private static ReportingDatePolicy CreatePolicy(DateTimeOffset utcNow)
    {
        return new ReportingDatePolicy(new FixedTimeProvider(utcNow));
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
