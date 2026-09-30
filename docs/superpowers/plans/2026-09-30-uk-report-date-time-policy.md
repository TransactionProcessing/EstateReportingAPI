# UK Report Date and Time Policy Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Standardize report date handling around `Europe/London`, make clock-dependent behavior injectable, and expose explicit date/time API contracts.

**Architecture:** Add a small reporting date policy around `TimeProvider` and `TimeZoneInfo`. Handlers normalize `DateOnly` request values into domain ranges; services use the normalized boundaries and the policy for current-day calculations. Existing database business-date columns remain UK-local values.

**Tech Stack:** .NET 10, ASP.NET Minimal APIs, MediatR, EF Core, xUnit v3, Shouldly.

**Spec:** `docs/superpowers/specs/2026-09-30-uk-report-date-time-policy-design.md`

## Global Constraints

- Use `Europe/London`; do not introduce estate-specific timezone storage.
- Preserve persisted UK-local reporting-date semantics.
- UI/client updates are out of scope.
- Use injected `TimeProvider`; no direct system-clock calls in reporting production code.

## Review Focus

- Midnight rollover must select the UK business day, not the server day.
- DST spring and autumn transitions must retain the correct UK business date.
- Inclusive end dates must include the entire requested day.
- Date-only report inputs must not depend on an implicit time component.
- Settlement “today” comparisons must use a normalized UK business date.

---

### Task 1: Add and test the UK reporting date policy

**Files:**
- Create: `EstateReportingAPI.BusinessLogic/ReportingDatePolicy.cs`
- Test: `EstateReportingAPI.BusinessLogic.UnitTests/ReportingDatePolicyTests.cs`

- [ ] Write failing tests for `Today`, inclusive range normalization, and GMT/BST boundary behavior using `TimeProvider`.
- [ ] Run the focused tests and verify they fail because the policy does not exist.
- [ ] Implement `ReportingDatePolicy` with `Europe/London`, `Today`, `GetDateRange(DateOnly, DateOnly)`, and `GetCurrentLocalTime()`.
- [ ] Run the focused tests and verify they pass.

### Task 2: Register the policy and migrate API date contracts

**Files:**
- Modify: `EstateReportingAPI/Bootstrapper/RepositoryRegistry.cs`
- Modify: report request DTOs under `EstateReportingAPI.DataTrasferObjects`
- Modify: corresponding models and MediatR queries under `EstateReportingAPI.Models` and `EstateReportingAPI.BusinessLogic/Queries`
- Test: request/handler contract coverage in existing unit and integration test projects

- [ ] Add failing contract tests proving report date inputs are date-only and timestamp outputs are offset-aware.
- [ ] Run the focused tests and verify the expected contract failures.
- [ ] Register `TimeProvider.System` and `ReportingDatePolicy` through DI, then migrate report input types and timestamp output types.
- [ ] Run focused tests and update all compile-time callers to use explicit date values.

### Task 3: Migrate current-day and range-based reporting queries

**Files:**
- Modify: `EstateReportingAPI/Handlers/CalenderHandler.cs`
- Modify: `EstateReportingAPI/Handlers/TransactionHandler.cs`
- Modify: `EstateReportingAPI/Handlers/SettlementHandler.cs`
- Modify: `EstateReportingAPI/Handlers/FileImportHandler.cs`
- Modify: reporting services under `EstateReportingAPI.BusinessLogic/Services`

- [ ] Add failing tests for inclusive range end behavior, UK current-day behavior, and normalized settlement dates.
- [ ] Run the focused tests and verify they fail against the current implementation.
- [ ] Replace direct `DateTime.Now`/`DateTime.Today` calls and `<= EndDate` range predicates with the policy and exclusive end boundaries.
- [ ] Run the focused unit tests and verify they pass.

### Task 4: Update integration coverage and remove direct clock calls

**Files:**
- Modify: `EstateReportingAPI.BusinessLogic.UnitTests/ReportingManagerReportTests.cs`
- Modify: relevant files under `EstateReportingAPI.IntegrationTests`
- Modify: any remaining reporting production files returned by `rg "DateTime\\.(Now|Today)"`

- [ ] Add integration cases for midnight and both UK DST transitions.
- [ ] Run the integration tests and correct any contract or boundary regressions.
- [ ] Run a final search for direct system-clock calls in reporting production code and remove remaining occurrences.

### Task 5: Verify the complete solution

- [ ] Run the focused unit tests.
- [ ] Run the complete solution test suite.
- [ ] Run `dotnet build EstateReportingAPI.sln`.
- [ ] Review the diff for unintended API or persistence changes.
