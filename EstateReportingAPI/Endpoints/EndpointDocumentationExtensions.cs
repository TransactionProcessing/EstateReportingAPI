using TransactionProcessing.UnionResults.AspNetCore;

namespace EstateReportingAPI.Endpoints;

internal static class EndpointDocumentationExtensions
{
    public static RouteHandlerBuilder WithApiResponse<TSuccess>(this RouteHandlerBuilder builder)
    {
        return builder.ProducesUnionResult<TSuccess>();
    }
}
