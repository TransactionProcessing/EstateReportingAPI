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

        public async Task InvokeAsync(
            HttpContext httpContext,
            EstateContext estateContext,
            IConfiguration configuration)
        {
            if (IsAnonymousEndpoint(httpContext))
            {
                await _next(httpContext);
                return;
            }

            bool disableAuthorisation = configuration.GetValue<Boolean>("AppSettings:DisableAuthorisation");
            bool isAuthenticated = httpContext.User.Identity?.IsAuthenticated == true;

            if (isAuthenticated == false && disableAuthorisation == false)
            {
                httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            if (TryGetHeaderEstateId(httpContext, out Guid? headerEstateId) == false)
            {
                httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            if (isAuthenticated)
            {
                await HandleAuthenticatedRequestAsync(httpContext, estateContext, headerEstateId);
                return;
            }

            await HandleUnauthenticatedRequestAsync(httpContext, estateContext, headerEstateId);
        }

        private static bool IsAnonymousEndpoint(HttpContext httpContext)
        {
            return httpContext.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() != null;
        }

        private static bool TryGetHeaderEstateId(HttpContext httpContext, out Guid? estateId)
        {
            estateId = null;

            if (httpContext.Request.Headers.TryGetValue("estateId", out var headerValues) == false)
            {
                return true;
            }

            if (headerValues.Count != 1)
            {
                return false;
            }

            if (Guid.TryParse(headerValues[0], out Guid parsedEstateId) == false)
            {
                return false;
            }

            if (parsedEstateId == Guid.Empty)
            {
                return false;
            }

            estateId = parsedEstateId;
            return true;
        }

        private async Task HandleUnauthenticatedRequestAsync(
            HttpContext httpContext,
            EstateContext estateContext,
            Guid? headerEstateId)
        {
            if (headerEstateId.HasValue == false)
            {
                httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            estateContext.SetEstate(headerEstateId.Value);
            await _next(httpContext);
        }

        private async Task HandleAuthenticatedRequestAsync(
            HttpContext httpContext,
            EstateContext estateContext,
            Guid? headerEstateId)
        {
            string? estateClaim = httpContext.User.FindFirst("estateId")?.Value;

            if (Guid.TryParse(estateClaim, out Guid authenticatedEstateId) == false ||
                authenticatedEstateId == Guid.Empty)
            {
                httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            if (headerEstateId.HasValue && headerEstateId.Value != authenticatedEstateId)
            {
                httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            estateContext.SetEstate(authenticatedEstateId);

            await _next(httpContext);
        }
    }
}
