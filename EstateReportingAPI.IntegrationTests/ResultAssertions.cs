using Shouldly;
using TransactionProcessing.UnionResults;

namespace EstateReportingAPI.IntegrationTests;

internal static class ResultAssertions
{
    public static T ShouldBeSuccess<T>(this Result<T> result)
    {
        return result.Value.ShouldBeOfType<Success<T>>().Value;
    }
}
