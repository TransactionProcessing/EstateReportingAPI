# Recent Activity Receipt Report Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a new transaction report endpoint that returns a date-scoped, merchant-filterable, paged list of recent activity receipts for the mobile app.

**Architecture:** Keep the endpoint thin and follow the existing `api/transactions` flow: Minimal API route -> handler -> MediatR query -> `ReportingManager` projection. Reuse the repository's DTO split between `Models` and `DataTrasferObjects`, and keep the new report isolated from the existing transaction detail report so the mobile contract stays narrow.

**Tech Stack:** ASP.NET Core Minimal APIs, MediatR, Entity Framework Core, xUnit integration tests.

---

### Task 1: Add the request/response contract and wire the endpoint

**Files:**
- Create: `EstateReportingAPI.DataTrasferObjects/GetRecentActivityReceiptReportRequest.cs`
- Create: `EstateReportingAPI.DataTrasferObjects/GetRecentActivityReceiptReportResponse.cs`
- Modify: `EstateReportingAPI.BusinessLogic/Queries/TransactionQueries.cs`
- Modify: `EstateReportingAPI.BusinessLogic/RequestHandlers/TransactionRequestHandler.cs`
- Modify: `EstateReportingAPI/Endpoints/TransactionEndpoints.cs`
- Modify: `EstateReportingAPI/Handlers/TransactionHandler.cs`

**Interfaces:**
- Consumes: the existing `api/transactions` Minimal API group and the MediatR transaction query pattern.
- Produces: `TransactionQueries.GetRecentActivityReceiptReportQuery`, handler forwarding, and a new `POST /api/transactions/recentactivityreceiptreport` route.

- [ ] **Step 1: Write the failing endpoint test**

```csharp
var payload = """
{
  "reportDate": "2026-07-07",
  "merchantReportingId": 1,
  "searchText": "coffee",
  "pageNumber": 1,
  "pageSize": 10
}
""";

var result = await this.CreateAndSendHttpRequestMessage<GetRecentActivityReceiptReportResponse>($"{BaseRoute}/recentactivityreceiptreport", payload, CancellationToken.None);
```

- [ ] **Step 2: Run the test and confirm it fails because the route is missing**

Run: `dotnet test EstateReportingAPI.IntegrationTests/EstateReportingAPI.IntegrationTests.csproj --filter RecentActivityReceipt`
Expected: 404 or unhandled route failure before implementation.

- [ ] **Step 3: Add the DTOs, query record, handler forwarding method, and endpoint mapping**

```csharp
group.MapPost("recentactivityreceiptreport", TransactionHandler.RecentActivityReceiptReport)
    .WithStandardProduces<GetRecentActivityReceiptReportResponse>();

public record GetRecentActivityReceiptReportQuery(Guid EstateId, GetRecentActivityReceiptReportRequest Request)
    : IRequest<Result<GetRecentActivityReceiptReportResponse>>;
```

- [ ] **Step 4: Re-run the test and confirm the route is now wired**

Run: `dotnet test EstateReportingAPI.IntegrationTests/EstateReportingAPI.IntegrationTests.csproj --filter RecentActivityReceipt`
Expected: request reaches the handler path and fails only until the manager implementation is added.

### Task 2: Implement the reporting query and verify behavior

**Files:**
- Modify: `EstateReportingAPI.BusinessLogic/ReportingManager.cs`
- Modify: `EstateReportingAPI.IntegrationTests/TransactionsEndpointTests.cs`

**Interfaces:**
- Consumes: `TransactionQueries.GetRecentActivityReceiptReportQuery` and the new DTOs.
- Produces: ordered, paged receipt rows filtered by report date, merchant reporting id, and optional search text.

- [ ] **Step 1: Add a failing integration test for filtering and paging**

Seed two merchants worth of transactions on the same day and assert the merchant filter, search filter, descending order, and paging metadata behave as expected.

- [ ] **Step 2: Implement the query projection**

Filter by `ReportDate`, optionally restrict by `MerchantReportingId`, apply `SearchText` before paging, sort by transaction datetime descending, and populate `TotalCount` from the pre-paged result set.

- [ ] **Step 3: Map the response**

Populate `Reference`, `TransactionType`, `Product`, `Operator`, `Status`, `Amount`, `TransactionDateTime`, and `ReceiptReference` with the minimum data required by the mobile UI.

- [ ] **Step 4: Run the targeted transaction integration tests**

Run: `dotnet test EstateReportingAPI.IntegrationTests/EstateReportingAPI.IntegrationTests.csproj --filter TransactionsEndpoint`
Expected: the new recent-activity report tests pass alongside the existing transaction endpoint coverage.
