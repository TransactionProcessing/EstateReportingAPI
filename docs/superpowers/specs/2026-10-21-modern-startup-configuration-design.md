# Modern Startup Configuration Refactor Design

## Goal

Move EstateReportingAPI to ASP.NET Core minimal hosting and remove static startup/configuration state, while preserving its deployed behavior and integration-test customization.

## Current State

The application targets .NET 10 but builds its host through `Host.CreateDefaultBuilder`, configures the web host in `Program.ConfigureWebHost`, then delegates services and middleware to `Startup`. Configuration is manually layered in the web-host callback and copied into `Startup.Configuration`; `Startup.WebHostEnvironment`, `Startup.Container`, and other static initializers provide global state. `ConfigurationReader` is used across registries, endpoints, and business logic. Lamar registries currently register authentication, database access, health checks, middleware options, serialization, repositories, and MediatR. Integration tests use `WebApplicationFactory<Startup>` and process-wide environment variables.

## Proposed Architecture

`Program.cs` will use `WebApplication.CreateBuilder(args)` as the composition root. A small set of focused registration extensions will configure host integrations (Windows Service, Lamar, logging, NLog, Sentry), application services, and the HTTP pipeline. The existing configuration source order and base paths will be represented explicitly on the new builder so precedence, optional files, reload behavior, and the in-memory service version stay compatible.

Configuration will flow through `IConfiguration`, typed options where appropriate, and injected services. Startup decisions that depend on the environment (such as in-memory database selection and Sentry activation) will be made at composition time from builder environment/configuration rather than static `Startup` fields. `ConfigurationReader` calls in this API will be replaced with injected configuration/options or small configuration-bound services; no API-owned static mutable configuration, environment, service provider, or container will remain. Serialization will be registered and consumed through DI. The current Shared package requires a narrowly scoped compatibility initializer: `TenantMiddleware` reads `ConfigurationReader`, and the same middleware logs through the shared static `Logger`. Keep those shared-library initializations isolated in one adapter until Shared exposes instance-based APIs.

Lamar remains the DI container and existing registries may be split or retained as registration modules where they can receive configuration and environment explicitly. Endpoint mappings, middleware ordering, authentication setup, health-check behavior, JSON serialization behavior, NLog call-site filtering, Windows Service support, and Sentry behavior remain functionally equivalent.

Integration tests will use `WebApplicationFactory<Program>` with per-factory configuration and service overrides. Test-specific environment/configuration will not be set as process-global mutable state. Database override behavior and the test authentication scheme will remain available.

## Configuration Contract

Preserve the effective application configuration inputs and ordering from `Program.cs`:

1. Host defaults and command-line arguments supported by `WebApplication.CreateBuilder(args)`.
2. `hosting.json` and `hosting.{Environment}.json` rooted at the executing assembly directory.
3. `/home/txnproc/config/appsettings.json` and its environment-specific companion.
4. `appsettings.json` and `appsettings.{Environment}.json` rooted at the executing assembly directory.
5. `/home/txnproc/config/appsettings.local.json`.
6. Environment variables.
7. In-memory `HealthMonitoring:Service:Version` derived from the entry assembly, with the current fallback.

The implementation must deliberately reconcile default-builder providers with this established ordering so that existing deployment overrides continue to win as they do today. JSON reload-on-change settings must be retained for the same files. The special absolute Linux paths remain optional.

## HTTP and Service Behavior to Preserve

- Windows Service hosting, Kestrel, console logging, NLog, and existing NLog configuration behavior.
- Sentry registration only when its section exists and the environment is not Development, including current options.
- Lamar registrations for middleware, repository, mediator, and serialization services.
- Authentication and authorization behavior, including `InTestMode` and `DisableAuthorisation` behavior unless a safer per-host equivalent is required to eliminate process-wide state.
- Database selection for `IntegrationTest` and `ServiceOptions:UseInMemoryDatabase`, SQL Server retry settings, and health-check configuration.
- JSON options and serializer initialization behavior.
- Middleware ordering and all currently mapped endpoints and health endpoints.

## Out of Scope

- Endpoint, response, route, DTO, database schema, or business-rule changes.
- Changing external configuration key names or deployment file locations.
- Removing Lamar, NLog, Sentry, OpenIddict, or shared packages.
- Broad redesign of the `Shared` package. If a shared static API blocks complete removal of a static bridge, isolate that compatibility boundary and record it for separate follow-up.

## Acceptance Criteria

1. `Program.cs` builds and runs the app using `WebApplication`; `Startup.cs` and `UseStartup` are removed.
2. API-owned static startup state is removed; direct static configuration use is removed except for the documented Shared compatibility adapter.
3. Configuration precedence, file paths, reload behavior, and environment-based decisions match the contract above.
4. The full middleware and endpoint pipeline remains behaviorally equivalent.
5. Integration tests target `Program` and apply configuration/service overrides per factory without mutating process-wide environment variables.
6. The project compiles and existing verification appropriate to the repository passes.

