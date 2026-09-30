namespace EstateReportingAPI.BusinessLogic;

public static class ReportQueryLimits
{
    public static readonly int MaxDateRangeDays = 30;
    public static readonly int MaxPageSize = 50;
    public static readonly int DefaultPageSize = 50;
    public static readonly int RecentActivityDefaultPageSize = 10;
    public static readonly int MaxTopN = 20;
    public static readonly int MaxFilterListSize = 50;
}
