using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.Models;
using Microsoft.EntityFrameworkCore;
using SimpleResults;
using TransactionProcessor.Database.Contexts;
using Shared.EntityFramework;

namespace EstateReportingAPI.BusinessLogic.Services;

public interface ISettlementReportingService
{
    Task<Result<TodaysSettlement>> GetTodaysSettlement(SettlementQueries.TodaysSettlementQuery request, CancellationToken cancellationToken);
}

public sealed class SettlementReportingService : ISettlementReportingService
{
    private readonly IDbContextResolver<EstateManagementContext> Resolver;
    private const string EstateManagementDatabaseName = "TransactionProcessorReadModel";
    public SettlementReportingService(IDbContextResolver<EstateManagementContext> resolver) => Resolver = resolver;

    public async Task<Result<TodaysSettlement>> GetTodaysSettlement(SettlementQueries.TodaysSettlementQuery request,
                                                                    CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        IQueryable<DatabaseProjections.TodaySettlementTransactionProjection> todaySettlementData = this.BuildTodaySettlementQuery(context, DateTime.Now);
        IQueryable<DatabaseProjections.ComparisonSettlementTransactionProjection> comparisonSettlementData = this.BuildComparisonSettlementQuery(context, request.ComparisonDate);
        
        DatabaseProjections.SettlementGroupProjection todaySettlement = await this.GetSettlementSummary(todaySettlementData, cancellationToken);
        DatabaseProjections.SettlementGroupProjection comparisonSettlement = await this.GetSettlementSummary(comparisonSettlementData, cancellationToken);

        TodaysSettlement response = new()
        {
            ComparisonSettlementCount = comparisonSettlement.SettledCount,
            ComparisonSettlementValue = comparisonSettlement.SettledValue,
            ComparisonPendingSettlementCount = comparisonSettlement.UnSettledCount,
            ComparisonPendingSettlementValue = comparisonSettlement.UnSettledValue,
            TodaysSettlementCount = todaySettlement.SettledCount,
            TodaysSettlementValue = todaySettlement.SettledValue,
            TodaysPendingSettlementCount = todaySettlement.UnSettledCount,
            TodaysPendingSettlementValue = todaySettlement.UnSettledValue
        };

        return response;
    }
    private async Task<DatabaseProjections.SettlementGroupProjection> GetSettlementSummary(IQueryable<DatabaseProjections.ComparisonSettlementTransactionProjection> query,
                                                                                           CancellationToken cancellationToken) {
            // Get the settleed fees summary
            DatabaseProjections.SettlementGroupProjection summary = await BuildSettlementSummaryQuery(query).SingleOrDefaultAsync(cancellationToken);

            if (summary == null)
                return new DatabaseProjections.SettlementGroupProjection();

        return new DatabaseProjections.SettlementGroupProjection { SettledCount = summary.SettledCount, SettledValue = summary.SettledValue, UnSettledCount = summary.UnSettledCount, UnSettledValue = summary.UnSettledValue };
        }

        private async Task<DatabaseProjections.SettlementGroupProjection> GetSettlementSummary(
            IQueryable<DatabaseProjections.TodaySettlementTransactionProjection> query,
            CancellationToken cancellationToken)
        {
            // Get the settleed fees summary
            DatabaseProjections.SettlementGroupProjection summary = await BuildSettlementSummaryQuery(query).SingleOrDefaultAsync(cancellationToken);

            if (summary == null)
                return new DatabaseProjections.SettlementGroupProjection();

            return new DatabaseProjections.SettlementGroupProjection
            {
                SettledCount = summary.SettledCount,
                SettledValue = summary.SettledValue,
                UnSettledCount = summary.UnSettledCount,
                UnSettledValue = summary.UnSettledValue
            };
        }

    private static IQueryable<DatabaseProjections.SettlementGroupProjection> BuildSettlementSummaryQuery(IQueryable<DatabaseProjections.ComparisonSettlementTransactionProjection> query) {
            return query.GroupBy(_ => 1).Select(g => new DatabaseProjections.SettlementGroupProjection { SettledCount = g.Count(x => x.Fee.IsSettled), SettledValue = g.Where(x => x.Fee.IsSettled).Sum(x => x.Fee.CalculatedValue), UnSettledCount = g.Count(x => !x.Fee.IsSettled), UnSettledValue = g.Where(x => !x.Fee.IsSettled).Sum(x => x.Fee.CalculatedValue) });
        }

        private static IQueryable<DatabaseProjections.SettlementGroupProjection> BuildSettlementSummaryQuery(IQueryable<DatabaseProjections.TodaySettlementTransactionProjection> query) {
            return query.GroupBy(_ => 1).Select(g => new DatabaseProjections.SettlementGroupProjection { SettledCount = g.Count(x => x.Fee.IsSettled), SettledValue = g.Where(x => x.Fee.IsSettled).Sum(x => x.Fee.CalculatedValue), UnSettledCount = g.Count(x => !x.Fee.IsSettled), UnSettledValue = g.Where(x => !x.Fee.IsSettled).Sum(x => x.Fee.CalculatedValue) });
        }

        private IQueryable<DatabaseProjections.TodaySettlementTransactionProjection> BuildTodaySettlementQuery(EstateManagementContext context,
                                                                                                               DateTime settlementDate) {
            IQueryable<DatabaseProjections.TodaySettlementTransactionProjection> settlementData = from s in context.Settlements join f in context.MerchantSettlementFees on s.SettlementId equals f.SettlementId join t in context.TodayTransactions on f.TransactionId equals t.TransactionId where s.SettlementDate == settlementDate select new DatabaseProjections.TodaySettlementTransactionProjection { Fee = f, Txn = t };
            return settlementData;
        }

        private IQueryable<DatabaseProjections.ComparisonSettlementTransactionProjection> BuildComparisonSettlementQuery(EstateManagementContext context,
                                                                                                                         DateTime settlementDate) {
            IQueryable<DatabaseProjections.ComparisonSettlementTransactionProjection> settlementData = from s in context.Settlements join f in context.MerchantSettlementFees on s.SettlementId equals f.SettlementId join t in context.TransactionHistory on f.TransactionId equals t.TransactionId where s.SettlementDate == settlementDate.Date select new DatabaseProjections.ComparisonSettlementTransactionProjection { Fee = f, Txn = t };
            return settlementData;
        }
}