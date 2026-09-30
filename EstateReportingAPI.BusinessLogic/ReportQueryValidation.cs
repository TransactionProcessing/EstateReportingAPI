using System;
using System.Collections.Generic;

namespace EstateReportingAPI.BusinessLogic;

public static class ReportQueryValidation
{
    public static string? ValidateDateRange(DateTime startDate, DateTime endDate)
    {
        DateTime start = startDate.Date;
        DateTime end = endDate.Date;

        if (start > end)
            return "startDate must be less than or equal to endDate.";

        if ((end - start).TotalDays + 1 > ReportQueryLimits.MaxDateRangeDays)
            return "date range must not exceed 30 inclusive calendar days.";

        return null;
    }

    public static string? ValidatePaging(int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
            return "pageNumber must be greater than or equal to 1.";

        if (pageSize < 1 || pageSize > ReportQueryLimits.MaxPageSize)
            return "pageSize must be between 1 and 50.";

        if ((long)(pageNumber - 1) * pageSize > int.MaxValue)
            return "pageNumber is too large for this report.";

        return null;
    }

    public static string? ValidateTopN(int topN)
    {
        if (topN < 1 || topN > ReportQueryLimits.MaxTopN)
            return "topN must be between 1 and 20.";

        return null;
    }

    public static string? ValidateFilterList<T>(string parameterName, IReadOnlyCollection<T>? values)
    {
        if (values != null && values.Count > ReportQueryLimits.MaxFilterListSize)
            return $"{parameterName} must contain no more than 50 values.";

        return null;
    }
}
