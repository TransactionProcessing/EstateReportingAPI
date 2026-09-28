using EstateReportingAPI.BusinessLogic;
using EstateReportingAPI.Models;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using TransactionProcessor.Database.Contexts;
using TransactionProcessor.Database.Entities;
using TransactionProcessor.Database.Entities.Summary;
using Db = TransactionProcessor.Database.Entities;

namespace EstateReportingAPI.BusinessLogic.UnitTests;

public sealed class ReportingManagerExtensionsTests
{
    [Fact]
    public void TodayTransactionFilters_HandleEmptyAndSelectedValues()
    {
        IQueryable<TodayTransaction> query = new[] {
            new TodayTransaction { MerchantReportingId = 20, OperatorReportingId = 10, ContractProductReportingId = 40 },
            new TodayTransaction { MerchantReportingId = 21, OperatorReportingId = 11, ContractProductReportingId = 41 }
        }.AsQueryable();

        query.ApplyMerchantFilter((List<int>)null!).Count().ShouldBe(2);
        query.ApplyMerchantFilter([]).Count().ShouldBe(2);
        query.ApplyMerchantFilter(20).Single().MerchantReportingId.ShouldBe(20);
        query.ApplyProductFilter([40]).Single().ContractProductReportingId.ShouldBe(40);
        query.ApplyProductFilter([]).Count().ShouldBe(2);
        query.ApplyOperatorFilter([10]).Single().OperatorReportingId.ShouldBe(10);
        query.ApplyOperatorFilter((List<int>)null!).Count().ShouldBe(2);
        query.ApplyOperatorFilter(11).Single().OperatorReportingId.ShouldBe(11);
        query.ApplyOperatorFilter(0).Count().ShouldBe(2);
    }

    [Fact]
    public void TransactionHistoryFilters_HandleEmptyAndSelectedValues()
    {
        IQueryable<TransactionHistory> query = new[] {
            new TransactionHistory { MerchantReportingId = 20, OperatorReportingId = 10, ContractProductReportingId = 40 },
            new TransactionHistory { MerchantReportingId = 21, OperatorReportingId = 11, ContractProductReportingId = 41 }
        }.AsQueryable();

        query.ApplyMerchantFilter((List<int>)null!).Count().ShouldBe(2);
        query.ApplyMerchantFilter([]).Count().ShouldBe(2);
        query.ApplyMerchantFilter(20).Single().MerchantReportingId.ShouldBe(20);
        query.ApplyProductFilter([40]).Single().ContractProductReportingId.ShouldBe(40);
        query.ApplyProductFilter([]).Count().ShouldBe(2);
        query.ApplyOperatorFilter([10]).Single().OperatorReportingId.ShouldBe(10);
        query.ApplyOperatorFilter((List<int>)null!).Count().ShouldBe(2);
        query.ApplyOperatorFilter(11).Single().OperatorReportingId.ShouldBe(11);
        query.ApplyOperatorFilter(0).Count().ShouldBe(2);
    }

    [Fact]
    public void SettlementFilters_HandleZeroAndSelectedValues()
    {
        var todayRows = new[] {
            new DatabaseProjections.TodaySettlementTransactionProjection { Txn = new TodayTransaction { MerchantReportingId = 20, OperatorReportingId = 10 } },
            new DatabaseProjections.TodaySettlementTransactionProjection { Txn = new TodayTransaction { MerchantReportingId = 21, OperatorReportingId = 11 } }
        }.AsQueryable();
        var comparisonRows = new[] {
            new DatabaseProjections.ComparisonSettlementTransactionProjection { Txn = new TransactionHistory { MerchantReportingId = 20, OperatorReportingId = 10 } },
            new DatabaseProjections.ComparisonSettlementTransactionProjection { Txn = new TransactionHistory { MerchantReportingId = 21, OperatorReportingId = 11 } }
        }.AsQueryable();

        todayRows.ApplyMerchantFilter(0).Count().ShouldBe(2);
        todayRows.ApplyMerchantFilter(20).Single().Txn.MerchantReportingId.ShouldBe(20);
        todayRows.ApplyOperatorFilter(0).Count().ShouldBe(2);
        todayRows.ApplyOperatorFilter(10).Single().Txn.OperatorReportingId.ShouldBe(10);
        comparisonRows.ApplyMerchantFilter(0).Count().ShouldBe(2);
        comparisonRows.ApplyMerchantFilter(20).Single().Txn.MerchantReportingId.ShouldBe(20);
        comparisonRows.ApplyOperatorFilter(0).Count().ShouldBe(2);
        comparisonRows.ApplyOperatorFilter(10).Single().Txn.OperatorReportingId.ShouldBe(10);

        new[] { new TodayTransaction { MerchantReportingId = 20 } }.AsQueryable()
            .ApplyMerchantFilter(0).Count().ShouldBe(1);
        new[] { new TransactionHistory { MerchantReportingId = 20 } }.AsQueryable()
            .ApplyMerchantFilter(0).Count().ShouldBe(1);
    }

    [Fact]
    public void TransactionSearchFiltersApplyAllPredicates()
    {
        var matching = new DatabaseProjections.TransactionSearchProjection {
            Transaction = new Transaction {
                TransactionAmount = 10m,
                AuthorisationCode = "AUTH",
                ResponseCode = "0000",
                TransactionNumber = "TX-1"
            },
            Merchant = new Db.Merchant { MerchantReportingId = 20 },
            Operator = new Db.Operator { OperatorReportingId = 10 }
        };
        var other = new DatabaseProjections.TransactionSearchProjection {
            Transaction = new Transaction {
                TransactionAmount = 90m,
                AuthorisationCode = "OTHER",
                ResponseCode = "1000",
                TransactionNumber = "TX-2"
            },
            Merchant = new Db.Merchant { MerchantReportingId = 21 },
            Operator = new Db.Operator { OperatorReportingId = 11 }
        };

        var request = new TransactionSearchRequest {
            Operators = [10],
            Merchants = [20],
            ValueRange = new ValueRange { StartValue = 5m, EndValue = 15m },
            AuthCode = "AUTH",
            ResponseCode = "0000",
            TransactionNumber = "TX-1"
        };

        new[] { matching, other }.AsQueryable().ApplyFilters(request).Single().ShouldBe(matching);
        new[] { matching, other }.AsQueryable().ApplyFilters(new TransactionSearchRequest()).Count().ShouldBe(2);
    }

    [Fact]
    public void PaginationAndSortingCoverAllDirections()
    {
        var records = new[] {
            SearchProjection("Merchant B", "Operator A", 20m),
            SearchProjection("Merchant A", "Operator B", 10m),
            SearchProjection("Merchant C", "Operator C", 30m)
        }.AsQueryable();

        records.ApplyPagination(new PagingRequest(2, 1)).Single().Transaction.TransactionAmount.ShouldBe(10m);
        records.ApplyPagination(new PagingRequest(1, 2)).Count().ShouldBe(2);

        records.ApplySorting(null!).Count().ShouldBe(3);
        records.ApplySorting(new SortingRequest(SortField.MerchantName, EstateReportingAPI.Models.SortDirection.Ascending)).First().Merchant.Name.ShouldBe("Merchant A");
        records.ApplySorting(new SortingRequest(SortField.MerchantName, EstateReportingAPI.Models.SortDirection.Descending)).First().Merchant.Name.ShouldBe("Merchant C");
        records.ApplySorting(new SortingRequest(SortField.OperatorName, EstateReportingAPI.Models.SortDirection.Ascending)).First().Operator.Name.ShouldBe("Operator A");
        records.ApplySorting(new SortingRequest(SortField.OperatorName, EstateReportingAPI.Models.SortDirection.Descending)).First().Operator.Name.ShouldBe("Operator C");
        records.ApplySorting(new SortingRequest(SortField.TransactionAmount, EstateReportingAPI.Models.SortDirection.Ascending)).First().Transaction.TransactionAmount.ShouldBe(10m);
        records.ApplySorting(new SortingRequest(SortField.TransactionAmount, EstateReportingAPI.Models.SortDirection.Descending)).First().Transaction.TransactionAmount.ShouldBe(30m);
        records.ApplySorting(new SortingRequest((SortField)999, (EstateReportingAPI.Models.SortDirection)999)).ShouldBe(records);
    }

    [Fact]
    public void SettlementFiltersAndFeeGroupingsFilterAndAggregateRows()
    {
        using EstateManagementContext context = CreateContext();
        Guid merchantId = Guid.NewGuid();
        Guid operatorId = Guid.NewGuid();
        Guid contractId = Guid.NewGuid();
        Guid productId = Guid.NewGuid();
        context.Merchants.Add(new Db.Merchant { MerchantId = merchantId, MerchantReportingId = 20, Name = "Merchant 1" });
        context.Operators.Add(new Db.Operator { OperatorId = operatorId, OperatorReportingId = 10, Name = "Operator 1" });
        context.Contracts.Add(new Db.Contract { ContractId = contractId, OperatorId = operatorId, Description = "Contract 1" });
        context.ContractProducts.Add(new Db.ContractProduct { ContractProductId = productId, ContractId = contractId, ContractProductReportingId = 40, ProductName = "Product 1", DisplayText = "Product 1" });
        context.SaveChanges();

        Guid transactionId = Guid.NewGuid();
        context.Transactions.Add(new Transaction {
            TransactionId = transactionId,
            MerchantId = merchantId,
            OperatorId = operatorId,
            ContractId = contractId,
            ContractProductId = productId,
            TransactionType = "Sale",
            TransactionDate = new DateTime(2026, 9, 1),
            TransactionDateTime = new DateTime(2026, 9, 1),
            TransactionTime = TimeSpan.Zero,
            TransactionAmount = 10m,
            IsAuthorised = true,
            IsCompleted = true,
            ResponseCode = "0000",
            TransactionNumber = "TX-1"
        });
        context.MerchantSettlementFees.Add(new MerchantSettlementFee {
            MerchantId = merchantId,
            TransactionId = transactionId,
            CalculatedValue = 1.5m,
            FeeValue = 1.5m
        });
        context.SaveChanges();

        var feeRows = from t in context.Transactions
                      join f in context.MerchantSettlementFees on t.TransactionId equals f.TransactionId
                      select new DatabaseProjections.FeeTransactionProjection { Txn = t, Fee = f };

        feeRows.ApplyMerchantFilter(context, [20]).Single().ShouldNotBeNull();
        feeRows.ApplyOperatorFilter(context, [10]).Single().ShouldNotBeNull();
        feeRows.ApplyProductFilter(context, [40]).Single().ShouldNotBeNull();
        feeRows.ApplyMerchantFilter(context, []).Count().ShouldBe(1);
        feeRows.ApplyOperatorFilter(context, []).Count().ShouldBe(1);
        feeRows.ApplyProductFilter(context, []).Count().ShouldBe(1);

        feeRows.ApplyMerchantGrouping(context).Single().ShouldSatisfyAllConditions(
            row => row.DimensionName.ShouldBe("Merchant 1"),
            row => row.FeesValue.ShouldBe(1.5m),
            row => row.FeesCount.ShouldBe(1));
        feeRows.ApplyOperatorGrouping(context).Single().DimensionName.ShouldBe("Operator 1");
        feeRows.ApplyProductGrouping(context).Single().DimensionName.ShouldBe("Operator 1 - Product 1");
    }

    private static EstateManagementContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<EstateManagementContext>()
            .UseInMemoryDatabase($"extension-tests-{Guid.NewGuid()}")
            .Options;
        return new EstateManagementContext(options);
    }

    private static DatabaseProjections.TransactionSearchProjection SearchProjection(string merchantName, string operatorName, decimal amount)
    {
        return new DatabaseProjections.TransactionSearchProjection {
            Transaction = new Transaction { TransactionAmount = amount },
            Merchant = new Db.Merchant { Name = merchantName },
            Operator = new Db.Operator { Name = operatorName }
        };
    }
}
