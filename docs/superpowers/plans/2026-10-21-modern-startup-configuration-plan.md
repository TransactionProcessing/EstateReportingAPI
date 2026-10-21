# Modern Startup Configuration Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Move EstateReportingAPI to minimal hosting and eliminate API-owned static startup/configuration state while preserving deployed behavior.

**Architecture:** `Program.cs` becomes the composition root. Private composition methods and the existing Lamar registries handle host setup, middleware, and endpoint mapping; configuration is injected through `IConfiguration` or options. Lamar remains the container. Integration tests configure each `WebApplicationFactory<Program>` instance instead of changing process-wide environment variables.

**Tech Stack:** .NET 10, ASP.NET Core minimal hosting, Lamar, NLog, Sentry, OpenIddict, EF Core.

**Spec:** `docs/superpowers/specs/2026-10-21-modern-startup-configuration-design.md`

## Global Constraints

- Preserve public routes, response behavior, configuration key names, and external config file locations.
- Preserve the effective configuration provider precedence and reload behavior.
- Preserve existing middleware order, service selection, authentication, logging, health checks, Windows Service, and Sentry behavior.
- Do not add or run tests unless requested by the user.
- Do not write to `.git`; repository metadata is read-only in this workspace.

## Review Focus

- Configuration provider precedence for appsettings, external files, environment variables, command-line arguments, and the in-memory version value.
- DI lifetime/availability for configuration needed by endpoints, business logic, serializers, and Lamar registrations.
- Auth and database behavior under IntegrationTest and test factory overrides.
- Middleware ordering and endpoint mappings.
- Static API dependencies from shared packages that may constrain complete static-state removal.

---

### Task 1: Minimal host and configuration

**Files:**
- Modify: `EstateReportingAPI/Program.cs`
- Delete: `EstateReportingAPI/Startup.cs`

**Interfaces:**
- Produces public partial `Program` for `WebApplicationFactory<Program>`.
- Produces one `WebApplication` configured with all preserved configuration sources and host integrations.

- [x] Create the `WebApplicationBuilder`, explicitly add the existing configuration files in their existing order and base paths, and preserve environment-variable and in-memory version precedence.
- [x] Configure NLog, Windows Service, Lamar, logging, Kestrel, and Sentry on the builder.
- [x] Add middleware and endpoint wiring through focused extension methods introduced in later tasks.
- [x] Remove the `Startup` class and its static fields.

### Task 2: DI registration and configuration consumers

**Files:**
- Modify: `EstateReportingAPI/Bootstrapper/MiddlewareRegistry.cs`
- Modify: `EstateReportingAPI/Bootstrapper/RepositoryRegistry.cs`
- Modify: `EstateReportingAPI/Bootstrapper/MediatorRegistry.cs`
- Modify: `EstateReportingAPI/Endpoints/*.cs`
- Modify: `EstateReportingAPI.BusinessLogic/QueryTimingInterceptor.cs`\n- Modify: `EstateReportingAPI.Tests/BusinessLogic/DbContextResolverXTests.cs`
- Add focused options/registration files under `EstateReportingAPI/Bootstrapper/` as needed.

**Interfaces:**
- Consumes `IConfiguration` and `IWebHostEnvironment` from the host.
- Removes production `ConfigurationReader` calls and global `StringSerialiser` initialization from API-controlled code.

- [x] Replace static configuration/environment reads in registries with explicit composition-time configuration or DI-resolved options.
- [x] Replace endpoint authorization static reads with injected `IConfiguration`/options as endpoint mapping parameters.
- [x] Replace business logic static reads with injected options/configuration dependencies.
- [x] Keep database, health-check, authentication, serialization, mediator, and middleware registrations behaviorally equivalent.

### Task 3: Middleware and endpoint composition

**Files:**
- Modify: `EstateReportingAPI/Program.cs`
- Modify: `EstateReportingAPI/Program.cs`

- [x] Move current middleware and endpoint mapping order into application extensions, retaining all existing endpoint and health-check routes.
- [x] Register logger and serializer dependencies through DI, removing static initializers where API code owns the call site.
- [x] Ensure no `Startup`, `ConfigurationReader`, or mutable startup state remains in production API paths.

### Task 4: Integration test host

**Files:**
- Modify: `EstateReportingAPI.IntegrationTests/CustomWebApplicationFactory.cs`
- Modify: `EstateReportingAPI.IntegrationTests/ControllerTestsBase.cs`

- [x] Retarget factories to `Program`.
- [x] Configure integration environment and test flags per factory, without process-wide environment mutation.
- [x] Preserve SQL database replacement and test authentication setup.

### Task 5: Review and compile

**Files:**
- Review all changed production and integration-test files against the spec.

- [x] Search for stale `Startup`/`ConfigurationReader`/global initializer usage.
- [x] Review service lifetimes, configuration precedence, middleware order, and test overrides.
- [x] Do not run tests or builds unless the user explicitly requests verification.



