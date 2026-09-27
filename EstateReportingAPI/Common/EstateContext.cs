using Microsoft.AspNetCore.Authorization;

namespace EstateReportingAPI.Common
{
    public interface IEstateContext
    {
        Guid EstateId { get; }
    }

    public sealed class EstateContext : IEstateContext
    {
        public Guid EstateId { get; private set; }

        internal void SetEstate(Guid estateId)
        {
            if (estateId == Guid.Empty)
                throw new ArgumentException("Estate ID cannot be empty.", nameof(estateId));

            EstateId = estateId;
        }
    }

    public sealed class EstateContextMiddleware
    {
        private readonly RequestDelegate _next;

        public EstateContextMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext,EstateContext estateContext)
        {
            // Health endpoints or anonymous endpoints do not need estate context.
            if (httpContext.GetEndpoint()?.Metadata
                    .GetMetadata<IAllowAnonymous>() != null)
            {
                await _next(httpContext);
                return;
            }

            if (httpContext.User.Identity?.IsAuthenticated != true)
            {
                httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            string? estateClaim = httpContext.User.FindFirst("estateId")?.Value;

            if (!Guid.TryParse(estateClaim, out Guid authenticatedEstateId) ||
                authenticatedEstateId == Guid.Empty)
            {
                httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            // Backwards compatibility: if the header is supplied, it must agree
            // with the authenticated claim.
            if (httpContext.Request.Headers.TryGetValue("estateId", out var headerValue))
            {
                if (!Guid.TryParse(headerValue.SingleOrDefault(), out Guid headerEstateId) ||
                    headerEstateId != authenticatedEstateId)
                {
                    httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return;
                }
            }

            estateContext.SetEstate(authenticatedEstateId);

            await _next(httpContext);
        }
    }
}
