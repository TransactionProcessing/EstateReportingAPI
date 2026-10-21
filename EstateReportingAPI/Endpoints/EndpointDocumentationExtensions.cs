using Shared.Extensions;
using Shared.Middleware;

namespace EstateReportingAPI.Endpoints;

internal static class EndpointDocumentationExtensions
{
    public static RouteHandlerBuilder WithApiResponse<TSuccess>(this RouteHandlerBuilder builder)
    {
        return builder.WithStandardProduces<TSuccess, ErrorResponse>();
    }
}
