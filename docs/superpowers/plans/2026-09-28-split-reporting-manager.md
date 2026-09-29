# Split ReportingManager into Focused Reporting Services Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the 2,339-line `ReportingManager` boundary with focused reporting services injected directly into MediatR handlers, without changing API or MediatR request/response contracts.

**Architecture:** Each reporting concern gets an interface and EF-backed implementation. Existing handlers retain their request types and `Handle` signatures but inject the relevant service directly. The old `IReportingManager` and `ReportingManager` are removed after all behavior and tests have moved to the services.

**Tech Stack:** ASP.NET Core, Lamar DI, MediatR, Entity Framework Core, SimpleResults, xUnit, Shouldly, Imposter.

**Spec:** `docs/superpowers/specs/2026-09-28-split-reporting-manager-design.md`

## Global Constraints

- Preserve every existing MediatR request type, handler response type, endpoint route, serialized response, result status, error message, query filter, ordering, date boundary, and cancellation-token use.
- Do not introduce a general-purpose repository abstraction or redesign database queries for performance.
- Keep focused services behind interfaces and register them in `EstateReportingAPI/Bootstrapper/RepositoryRegistry.cs`.
- Remove `IReportingManager` and `ReportingManager` only after all production and test consumers have migrated.
- Keep the existing integration and report coverage; add service-focused coverage without requiring the old manager.

## Review Focus

- Handler wiring: every request is sent to the same service method with the same request and cancellation token.
- Result compatibility: EF failures, not-found results, empty lists, and error messages remain unchanged.
- Calculation reports: transaction summaries, averages, totals, and grouping preserve existing behavior.
- Cross-cutting lookups: estate IDs, date filters, ordering, and related merchant/operator/contract joins remain unchanged.
- Test composition: integration fixtures and generated imposters no longer depend on the removed manager interface.

---

### Task 1: Add shared query execution helpers and service contracts

**Files:**
- Create: `EstateReportingAPI.BusinessLogic/ReportingQueryExecutor.cs`
- Create: `EstateReportingAPI.BusinessLogic/ITransactionReportingService.cs`
- Create: `EstateReportingAPI.BusinessLogic/IMerchantReportingService.cs`
- Create: `EstateReportingAPI.BusinessLogic/IEstateReportingService.cs`
- Create: `EstateReportingAPI.BusinessLogic/ISettlementReportingService.cs`
- Create: `EstateReportingAPI.BusinessLogic/IFileImportReportingService.cs`
- Test: `EstateReportingAPI.BusinessLogic.UnitTests/ReportingQueryExecutorTests.cs`

**Interfaces:**
- Produces service method signatures equivalent to the current `IReportingManager` methods, grouped by reporting concern.
- `ReportingQueryExecutor` preserves the current safe `SumAsync`, `ToListAsync`, `CountAsync`, and `SingleOrDefaultAsync` result/error behavior.

- [ ] **Step 1: Write failing helper tests**

Test successful list/count/sum/single results, not-found for a missing single result, and exception-to-failure conversion with and without a context message.

- [ ] **Step 2: Run the focused tests and verify they fail**

Run: `dotnet test EstateReportingAPI.BusinessLogic.UnitTests/EstateReportingAPI.BusinessLogic.UnitTests.csproj --filter FullyQualifiedName~ReportingQueryExecutorTests`

Expected: FAIL because the helper does not exist.

- [ ] **Step 3: Implement the helper and service interfaces**

Copy the existing semantics from `ReportingManager.cs` without changing messages or result status. Define asynchronous methods accepting the existing query type and `CancellationToken`, returning the existing `Result<T>` types.

- [ ] **Step 4: Run the focused tests**

Run the same focused test command. Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add EstateReportingAPI.BusinessLogic EstateReportingAPI.BusinessLogic.UnitTests/ReportingQueryExecutorTests.cs
git commit -m "refactor: add reporting service contracts"
```

### Task 2: Extract transaction reporting and migrate transaction handlers

**Files:**
- Create: `EstateReportingAPI.BusinessLogic/TransactionReportingService.cs`
- Modify: `EstateReportingAPI.BusinessLogic/RequestHandlers/TransactionRequestHandler.cs`
- Modify: `EstateReportingAPI.BusinessLogic.UnitTests/ReportingManagerReportTests.cs`
- Modify: `EstateReportingAPI.BusinessLogic.UnitTests/RequestHandlerTests.cs`
- Create or modify: `EstateReportingAPI.BusinessLogic.UnitTests/TransactionReportingServiceTests.cs`

**Interfaces:**
- Consumes: `ITransactionReportingService`, `ReportingQueryExecutor`, `IDbContextResolver<EstateManagementContext>`.
- Produces: all ten transaction methods currently beginning at `ReportingManager.cs:419`, `:447`, `:760`, `:848`, `:933`, `:1285`, `:1308`, `:1358`, `:1578`, and `:1853`.

- [ ] **Step 1: Add focused failing service tests**

Move or duplicate calculation-focused cases for transaction detail, merchant/operator summaries, product performance, transaction mix, and daily performance so they instantiate `TransactionReportingService` directly and assert the existing response values.

- [ ] **Step 2: Run focused tests and verify the new service tests fail**

Run: `dotnet test EstateReportingAPI.BusinessLogic.UnitTests/EstateReportingAPI.BusinessLogic.UnitTests.csproj --filter FullyQualifiedName~TransactionReportingServiceTests`

- [ ] **Step 3: Extract the transaction query implementations**

Move the method bodies unchanged into `TransactionReportingService`; replace private helper calls with `ReportingQueryExecutor` and retain all projection/calculation logic.

- [ ] **Step 4: Change `TransactionRequestHandler` to inject the transaction service**

Each `Handle` method calls the corresponding service method directly and returns its result unchanged. Keep all `IRequestHandler` declarations and method signatures unchanged.

- [ ] **Step 5: Run transaction and handler tests**

Run the focused service tests and `dotnet test EstateReportingAPI.BusinessLogic.UnitTests/EstateReportingAPI.BusinessLogic.UnitTests.csproj --filter FullyQualifiedName~RequestHandlerTests`.

Expected: PASS with no `IReportingManager` dependency for transaction handlers.

- [ ] **Step 6: Commit**

```bash
git add EstateReportingAPI.BusinessLogic/TransactionReportingService.cs EstateReportingAPI.BusinessLogic/RequestHandlers/TransactionRequestHandler.cs EstateReportingAPI.BusinessLogic.UnitTests
git commit -m "refactor: extract transaction reporting service"
```

### Task 3: Extract estate, calendar, merchant, operator, and contract reporting

**Files:**
- Create: `EstateReportingAPI.BusinessLogic/EstateReportingService.cs`
- Create: `EstateReportingAPI.BusinessLogic/MerchantReportingService.cs`
- Modify: `EstateReportingAPI.BusinessLogic/RequestHandlers/CalendarRequestHandler.cs`
- Modify: `EstateReportingAPI.BusinessLogic/RequestHandlers/EstateRequestHandler.cs`
- Modify: `EstateReportingAPI.BusinessLogic/RequestHandlers/MerchantRequestHandler.cs`
- Modify: `EstateReportingAPI.BusinessLogic/RequestHandlers/OperatorRequestHandler.cs`
- Modify: `EstateReportingAPI.BusinessLogic/RequestHandlers/ContractRequestHandler.cs`
- Modify: `EstateReportingAPI.BusinessLogic.UnitTests/ReportingManagerReportTests.cs`
- Modify: `EstateReportingAPI.BusinessLogic.UnitTests/UnitTest1.cs`
- Modify: `EstateReportingAPI.BusinessLogic.UnitTests/RequestHandlerTests.cs`
- Create or modify: `EstateReportingAPI.BusinessLogic.UnitTests/EstateReportingServiceTests.cs`
- Create or modify: `EstateReportingAPI.BusinessLogic.UnitTests/MerchantReportingServiceTests.cs`

**Interfaces:**
- `IEstateReportingService` owns calendar, estate, and estate-operator methods.
- `IMerchantReportingService` owns merchant, operator, contract, relationship, schedule, opening-hour, device, and KPI methods.

- [ ] **Step 1: Add focused failing service tests**

Move existing calendar, estate, contract, merchant, and operator assertions from manager-based setup to the appropriate service. Preserve date filters, ordering, related data, and empty/not-found expectations.

- [ ] **Step 2: Run focused tests and verify they fail**

Run both new service-test filters. Expected: FAIL until implementations exist.

- [ ] **Step 3: Extract estate and merchant query implementations**

Move the method bodies from `ReportingManager.cs:144-758` and `:1035-1284` into the two services, preserving joins, projections, helper calls, and result propagation.

- [ ] **Step 4: Migrate the five lookup handlers**

Inject `IEstateReportingService` into calendar/estate handlers and `IMerchantReportingService` into merchant/operator/contract handlers. Preserve each existing handler interface declaration and `Handle` signature.

- [ ] **Step 5: Run focused service and handler tests**

Run the new service filters and the full `RequestHandlerTests` filter. Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add EstateReportingAPI.BusinessLogic EstateReportingAPI.BusinessLogic.UnitTests
git commit -m "refactor: extract estate and merchant reporting services"
```

### Task 4: Extract settlement and file-import reporting

**Files:**
- Create: `EstateReportingAPI.BusinessLogic/SettlementReportingService.cs`
- Create: `EstateReportingAPI.BusinessLogic/FileImportReportingService.cs`
- Modify: `EstateReportingAPI.BusinessLogic/RequestHandlers/SettlementRequestHandler.cs`
- Modify: `EstateReportingAPI.BusinessLogic/RequestHandlers/FileImportLogRequestHandler.cs`
- Modify: `EstateReportingAPI.BusinessLogic.UnitTests/RequestHandlerTests.cs`
- Modify or create focused service tests for settlement and file-import operations.

**Interfaces:**
- `ISettlementReportingService` owns `GetTodaysSettlement`.
- `IFileImportReportingService` owns file-import log and file-profile configuration methods.

- [ ] **Step 1: Add failing service tests**

Cover settlement result propagation and file-import list/detail/configuration responses, including not-found and empty-list behavior currently provided by the manager.

- [ ] **Step 2: Extract the implementations**

Move the existing bodies from `ReportingManager.cs:1624-1852` into the two focused services and route safe query calls through the shared helper.

- [ ] **Step 3: Migrate settlement and file-import handlers**

Inject the focused interfaces directly and remove their `IReportingManager` dependencies.

- [ ] **Step 4: Run focused tests and the handler tests**

Run the service filters and `RequestHandlerTests`. Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add EstateReportingAPI.BusinessLogic EstateReportingAPI.BusinessLogic.UnitTests
git commit -m "refactor: extract settlement and file import services"
```

### Task 5: Register services and remove the manager boundary

**Files:**
- Modify: `EstateReportingAPI/Bootstrapper/RepositoryRegistry.cs`
- Modify: `EstateReportingAPI.IntegrationTests/CustomWebApplicationFactory.cs`
- Modify: `EstateReportingAPI.BusinessLogic.UnitTests/RequestHandlerTests.cs`
- Modify: `EstateReportingAPI.BusinessLogic.UnitTests/UnitTest1.cs`
- Modify: `EstateReportingAPI.BusinessLogic.UnitTests/ReportingManagerReportTests.cs`
- Delete: `EstateReportingAPI.BusinessLogic/ReportingManager.cs`

- [ ] **Step 1: Update DI and test composition**

Register each focused interface/implementation with the current manager lifetime. Update integration and unit fixtures to resolve or construct focused services directly. Remove generated `IReportingManager` imposter usage and replace it with focused service imposters.

- [ ] **Step 2: Remove the old manager files and references**

Delete `ReportingManager.cs` after `rg -n "ReportingManager|IReportingManager"` shows only intended historical documentation references. Remove obsolete usings and project references if any.

- [ ] **Step 3: Run compile and focused regression tests**

Run: `dotnet build EstateReportingAPI.sln --no-restore`

Run: `dotnet test EstateReportingAPI.BusinessLogic.UnitTests/EstateReportingAPI.BusinessLogic.UnitTests.csproj --no-build`

Expected: build succeeds and all business-logic unit tests pass.

- [ ] **Step 4: Run integration tests**

Run: `dotnet test EstateReportingAPI.IntegrationTests/EstateReportingAPI.IntegrationTests.csproj --no-restore`

Expected: all existing integration tests pass with unchanged API responses.

- [ ] **Step 5: Commit**

```bash
git add EstateReportingAPI EstateReportingAPI.BusinessLogic EstateReportingAPI.BusinessLogic.UnitTests EstateReportingAPI.IntegrationTests
git commit -m "refactor: inject focused reporting services into handlers"
```

### Task 6: Final verification and review

**Files:**
- Modify only if verification exposes a regression.

- [ ] **Step 1: Run the complete solution test suite**

Run: `dotnet test EstateReportingAPI.sln --no-restore`

Expected: exit code 0 with zero failed tests.

- [ ] **Step 2: Verify no old boundary remains**

Run: `rg -n "IReportingManager|ReportingManager" EstateReportingAPI EstateReportingAPI.BusinessLogic EstateReportingAPI.BusinessLogic.UnitTests EstateReportingAPI.IntegrationTests`.

Expected: no production or test references remain.

- [ ] **Step 3: Review the final diff**

Run: `git diff HEAD~5..HEAD --check` and inspect the complete diff for contract, registration, and test-composition changes.

- [ ] **Step 4: Commit any required verification-only fixes**

Use a focused commit message describing the verified regression fix; do not broaden scope.
