namespace EstateReportingAPI.BusinessLogic;

public readonly record struct ReportingDateRange(DateTime StartInclusive, DateTime EndExclusive);

public sealed class ReportingDatePolicy
{
    private const string IanaTimeZoneId = "Europe/London";
    private const string WindowsTimeZoneId = "GMT Standard Time";

    private readonly TimeProvider Clock;
    private readonly TimeZoneInfo TimeZone;

    public ReportingDatePolicy(TimeProvider clock)
    {
        Clock = clock ?? throw new ArgumentNullException(nameof(clock));
        TimeZone = ResolveTimeZone();
    }

    public DateOnly Today => DateOnly.FromDateTime(LocalNow);

    public DateTime LocalNow => TimeZoneInfo.ConvertTime(Clock.GetUtcNow(), TimeZone).DateTime;

    public DateTimeOffset ToDateTimeOffset(DateTime localDateTime)
    {
        DateTime unspecified = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);
        return new DateTimeOffset(unspecified, TimeZone.GetUtcOffset(unspecified));
    }

    public ReportingDateRange GetRange(DateOnly startDate, DateOnly endDate)
    {
        if (startDate > endDate)
            throw new ArgumentException("The start date must be less than or equal to the end date.", nameof(startDate));

        DateTime startInclusive = startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        DateTime endExclusive = endDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);

        return new ReportingDateRange(startInclusive, endExclusive);
    }

    private static TimeZoneInfo ResolveTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(IanaTimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById(WindowsTimeZoneId);
        }
    }
}
