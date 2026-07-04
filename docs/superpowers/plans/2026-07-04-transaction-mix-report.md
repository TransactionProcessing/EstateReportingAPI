# Transaction Mix Report Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a merchant-scoped transaction mix reporting endpoint that can break down activity by product, transaction type, operator, and status over a date range.

**Architecture:** Extend the existing `api/transactions` reporting surface so the new endpoint follows the same Minimal API, MediatR, and `ReportingManager` flow as the current transaction reports. Keep the request/response shapes mirrored in `Models` and `DataTrasferObjects`, and implement the aggregation in `ReportingManager` so the endpoint stays thin.

**Tech Stack:** ASP.NET Core Minimal APIs, MediatR, Entity Framework Core, xUnit integration tests.

---

### Task 1: Add the new transaction mix contract

**Files:**
- Create: `EstateReportingAPI.Models/TransactionMixSummaryRequest.cs`
- Create: `EstateReportingAPI.Models/TransactionMixSummaryResponse.cs`
- Create: `EstateReportingAPI.DataTrasferObjects/TransactionMixSummaryRequest.cs`
- Create: `EstateReportingAPI.DataTrasferObjects/TransactionMixSummaryResponse.cs`
- Create: `EstateReportingAPI.DataTrasferObjects/TransactionMixSummaryGroup.cs`
- Create: `EstateReportingAPI.DataTrasferObjects/TransactionMixSummaryTransaction.cs`

- [ ] **Step 1: Add the failing model references**

```csharp
public enum TransactionMixBreakdown
{
    Product,
    TransactionType,
    Operator,
    Status
}

public enum TransactionMixMeasure
{
    Count,
    Value
}
```

- [ ] **Step 2: Run a build that fails until the new types exist**

Run: `dotnet build`
Expected: compile errors for the missing transaction mix types

- [ ] **Step 3: Add the minimal DTO/model implementations**

```csharp
public sealed class TransactionMixSummaryRequest
{
    public int? MerchantReportingId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public TransactionMixBreakdown Breakdown { get; set; }
    public TransactionMixMeasure Measure { get; set; }
    public int TopN { get; set; }
}
```

- [ ] **Step 4: Rebuild to verify the contract compiles**

Run: `dotnet build`
Expected: success

### Task 2: Wire the endpoint and query handler

**Files:**
- Modify: `EstateReportingAPI/Endpoints/TransactionEndpoints.cs`
- Modify: `EstateReportingAPI/Handlers/TransactionHandler.cs`
- Modify: `EstateReportingAPI.BusinessLogic/Queries/TransactionQueries.cs`
- Modify: `EstateReportingAPI.BusinessLogic/RequestHandlers/TransactionRequestHandler.cs`

- [ ] **Step 1: Add a failing endpoint test case**

Use the integration test suite to call `POST /api/transactions/transactionmixsummary` and assert the route is missing or unhandled before implementation.

- [ ] **Step 2: Add the endpoint and MediatR query**

```csharp
group.MapPost("transactionmixsummary", TransactionHandler.TransactionMixSummary)
    .WithStandardProduces<TransactionMixSummaryResponse>();

public record TransactionMixSummaryQuery(Guid EstateId, TransactionMixSummaryRequest Request)
    : IRequest<Result<TransactionMixSummaryResponse>>;
```

- [ ] **Step 3: Add the request handler forwarding method**

```csharp
public async Task<Result<TransactionMixSummaryResponse>> Handle(TransactionMixSummaryQuery request, CancellationToken cancellationToken)
{
    return await this.Manager.GetTransactionMixSummary(request, cancellationToken);
}
```

- [ ] **Step 4: Rebuild and confirm the new route compiles**

Run: `dotnet build`
Expected: success

### Task 3: Implement the aggregation in `ReportingManager`

**Files:**
- Modify: `EstateReportingAPI.BusinessLogic/ReportingManager.cs`

- [ ] **Step 1: Add a failing integration path**

Add a test that seeds transactions and expects grouped counts/values by at least one breakdown dimension.

- [ ] **Step 2: Implement the report query and projection**

Use the existing `Transactions` set, filter by date range and merchant, then group by the selected breakdown with count/value totals and drill-down rows.

- [ ] **Step 3: Return empty collections and validation failures cleanly**

Reject invalid date ranges and unsupported measure/breakdown combinations; return empty collections when the query yields no rows.

- [ ] **Step 4: Rebuild and run the targeted reporting tests**

Run: `dotnet test EstateReportingAPI.IntegrationTests/EstateReportingAPI.IntegrationTests.csproj --filter TransactionsEndpoint`
Expected: pass

### Task 4: Cover the endpoint with integration tests

**Files:**
- Modify: `EstateReportingAPI.IntegrationTests/TransactionsEndpointTests.cs`

- [ ] **Step 1: Add success coverage**

Verify product, operator, transaction type, and status breakdowns return the expected totals.

- [ ] **Step 2: Add top-N and empty-result coverage**

Verify the response respects `TopN` and returns empty collections when there is no matching data.

- [ ] **Step 3: Add validation coverage**

Verify invalid date ranges are rejected.

- [ ] **Step 4: Run the full integration test slice**

Run: `dotnet test EstateReportingAPI.IntegrationTests/EstateReportingAPI.IntegrationTests.csproj --filter TransactionsEndpoint`
Expected: pass
