using System.Diagnostics.CodeAnalysis;
using TransactionProcessing.UnionResults;

namespace EstateReportingAPI.BusinessLogic;

internal static class ReportingResultExtensions
{
    public static bool TryGetSuccess<T>(this Result<T> result, [MaybeNullWhen(false)] out T value)
    {
        if (result.Value is Success<T> success)
        {
            value = success.Value;
            return true;
        }

        value = default;
        return false;
    }
}
