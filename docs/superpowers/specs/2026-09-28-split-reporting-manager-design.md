# Split ReportingManager into Focused Reporting Services

## Status

Approved conversational design for issue #598.

## Goal

Remove the reporting god object by making MediatR request handlers depend directly on focused reporting services while preserving all existing request behavior, public API contracts, response shapes, result semantics, and endpoint wiring.

## Current Context

`ReportingManager` is a 2,339-line class implementing 29 reporting operations across calendar, estate, merchant, operator, contract, transaction, settlement, and file-import concerns. All request handlers depend on `IReportingManager`, and integration tests construct the concrete manager directly. Existing report tests also use the manager as their entry point.

The refactor may change internal handler constructor dependencies, but must preserve every MediatR request type, handler response type, endpoint route, serialized response, and result behavior. `IReportingManager` and `ReportingManager` are implementation details that can be removed once all consumers and tests have moved to focused services.

## Design

### Handler boundaries

Change each MediatR request handler to inject only the focused service or services required by the requests it handles. Keep request types and handler `Handle` signatures unchanged. Handlers remain thin adapters: they pass the request and cancellation token to the service and return the service result unchanged.

Remove `IReportingManager` and `ReportingManager` after all production consumers, test fixtures, and generated test doubles have been migrated. No compatibility façade is required because the interface is internal application wiring rather than an HTTP/API contract.

### Focused services

Introduce focused interfaces and implementations in `EstateReportingAPI.BusinessLogic`:

1. `ITransactionReportingService` / `TransactionReportingService`
   - Today's sales and failed sales.
   - Transaction detail, summary-by-merchant, summary-by-operator, product performance, transaction mix, recent activity receipt, sales-by-hour, and merchant daily performance reports.
2. `IMerchantReportingService` / `MerchantReportingService`
   - Merchant and operator lookups and merchant-related relationships, schedules, opening hours, devices, contracts, and KPIs.
   - Contract operations may be kept in this service because they are consumed as merchant/operator reporting lookups and share the same query dependencies; if code inspection shows a clean independent boundary, they may be split into `IContractReportingService`.
3. `IEstateReportingService` / `EstateReportingService`
   - Estate, estate operators, and calendar comparison/date/year lookups.
   - File-profile configuration lookup is included here only if its existing query dependencies are configuration-oriented; otherwise it remains with file-import reporting.
4. `ISettlementReportingService` / `SettlementReportingService`
   - Today's settlement reporting.
5. `IFileImportReportingService` / `FileImportReportingService`
   - File-import log list/detail operations and file-profile configuration operations not assigned to estate configuration.

The final assignment of contract and file-profile methods must follow the existing method dependencies and avoid duplicating query logic. The acceptance criterion is cohesive responsibility, not a fixed number of classes.

### Shared query execution

Move the existing safe EF query wrappers (`SumAsync`, `ToListAsync`, `CountAsync`, and `SingleOrDefaultAsync`) into a small internal helper or service-local shared utility. Preserve their current failure conversion and `NotFound` behavior. Do not introduce a new general-purpose repository abstraction as part of this issue.

### Dependency injection and construction

Register each focused interface and implementation in `RepositoryRegistry` with the same lifetime as the current manager unless the implementation requires a different lifetime based on its dependencies. Remove the `IReportingManager`/`ReportingManager` registration after all handlers are migrated.

Update handler tests, generated imposters, direct test construction, and integration fixtures to compose the focused services directly. Do not retain duplicate production implementations or a compatibility façade solely to keep old constructors working.

### Testing strategy

- Preserve and run the existing unit and integration suites to detect behavior and contract regressions.
- Add focused unit coverage for at least one calculation-heavy transaction-report service without constructing `ReportingManager`.
- Add focused coverage for handler-to-service wiring where practical.
- Move existing manager-based report setup to the extracted service under test without reducing endpoint or query coverage.

## Data flow

```text
HTTP endpoint -> MediatR request -> existing request handler
             -> focused reporting service
             -> EstateManagementContext via IDbContextResolver
             -> existing Models + SimpleResults response contract
```

## Error handling and compatibility

The extracted methods must preserve current result propagation, including failed query messages, not-found responses, empty-list behavior, ordering, date boundaries, and cancellation-token use. Handlers must return the delegated result unchanged. No endpoint route, MediatR query, DTO, model, or serialized response is changed.

## Scope boundaries

In scope: replacing the manager with focused interfaces/implementations, updating handler dependencies and DI wiring, moving or adapting tests, and adding focused service tests.

Out of scope: changing API contracts, redesigning database queries for performance, changing result/error semantics, replacing MediatR, or broad repository abstractions.

## Acceptance criteria mapping

- Each extracted service has one cohesive reporting responsibility and an interface.
- Each MediatR handler depends directly on the focused service(s) required by its requests.
- Existing handlers, endpoints, response contracts, and result semantics remain unchanged.
- Existing integration tests continue to pass.
- At least one calculation-focused unit test exercises an extracted service without the entire manager.
