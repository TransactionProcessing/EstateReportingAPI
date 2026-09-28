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
            // Health endpoints or anonymous endpoints do not need estate context.
            if (httpContext.GetEndpoint()?.Metadata
                    .GetMetadata<IAllowAnonymous>() != null)
            {
                await _next(httpContext);
                return;
            }

            bool disableAuthorisation = configuration.GetValue<Boolean>(
                "AppSettings:DisableAuthorisation");
            bool isAuthenticated = httpContext.User.Identity?.IsAuthenticated == true;

            if (isAuthenticated == false && disableAuthorisation == false)
            {
                httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            Guid? headerEstateId = null;
            if (httpContext.Request.Headers.TryGetValue("estateId", out var headerValues))
            {
                if (headerValues.Count != 1 ||
                    !Guid.TryParse(headerValues[0], out Guid parsedHeaderEstateId) ||
                    parsedHeaderEstateId == Guid.Empty)
                {
                    httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return;
                }

                headerEstateId = parsedHeaderEstateId;
            }

            if (isAuthenticated == false)
            {
                if (headerEstateId.HasValue == false)
                {
                    httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return;
                }

                estateContext.SetEstate(headerEstateId.Value);
                await _next(httpContext);
                return;
            }

            string? estateClaim = httpContext.User.FindFirst("estateId")?.Value;

            if (!Guid.TryParse(estateClaim, out Guid authenticatedEstateId) ||
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
