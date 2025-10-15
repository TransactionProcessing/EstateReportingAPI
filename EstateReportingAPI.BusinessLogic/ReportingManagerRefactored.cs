using EstateReportingAPI.Models;
using Microsoft.EntityFrameworkCore;
using Shared.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransactionProcessor.Database.Contexts;
using TransactionProcessor.Database.Entities;
using Merchant = TransactionProcessor.Database.Entities.Merchant;
using Operator = TransactionProcessor.Database.Entities.Operator;

namespace EstateReportingAPI.BusinessLogic
{
    public partial class ReportingManager : IReportingManager {

        private IQueryable<DatabaseProjections.FeeTransactionProjection> BuildUnsettledFeesQuery(EstateManagementContext context,
                                                                                                 DateTime startDate,
                                                                                                 DateTime endDate)
        {
            return from merchantSettlementFee in context.MerchantSettlementFees
                join transaction in context.Transactions
                    on merchantSettlementFee.TransactionId equals transaction.TransactionId
                where merchantSettlementFee.FeeCalculatedDateTime.Date >= startDate &&
                      merchantSettlementFee.FeeCalculatedDateTime.Date <= endDate
                select new DatabaseProjections.FeeTransactionProjection { Fee = merchantSettlementFee, Txn = transaction };
        }

        private IQueryable<DatabaseProjections.TransactionSearchProjection> BuildTransactionSearchQuery(EstateManagementContext context,
                                                                                                     DateTime queryDate) {
            return from txn in context.Transactions 
                join merchant in context.Merchants on txn.MerchantId equals merchant.MerchantId 
                join @operator in context.Operators on txn.OperatorId equals @operator.OperatorId 
                join product in context.ContractProducts on txn.ContractProductId equals product.ContractProductId 
                where txn.TransactionDate == queryDate 
                select new DatabaseProjections.TransactionSearchProjection {
                    Transaction = txn, 
                    Merchant = merchant, 
                    Operator = @operator, 
                    Product = product
                };
        }

        public async Task<List<UnsettledFee>> GetUnsettledFees(Guid estateId, DateTime startDate, DateTime endDate, List<Int32> merchantIds, List<Int32> operatorIds, List<Int32> productIds, GroupByOption? groupByOption, CancellationToken cancellationToken)
        {

            using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, estateId.ToString());
            await using EstateManagementContext context = resolvedContext.Context;

            IQueryable<DatabaseProjections.FeeTransactionProjection> query = BuildUnsettledFeesQuery(context, startDate, endDate)
                .ApplyMerchantFilter(context, merchantIds)
                .ApplyOperatorFilter(context, operatorIds)
                .ApplyProductFilter(context, productIds);

            // Perform grouping
            IQueryable<UnsettledFee> groupedQuery = groupByOption switch
            {
                GroupByOption.Merchant => query.ApplyMerchantGrouping(context),
                GroupByOption.Operator => query.ApplyOperatorGrouping(context),
                GroupByOption.Product => query.ApplyProductGrouping(context),
            };
            return await groupedQuery.ToListAsync(cancellationToken);

        }

        public async Task<List<TransactionResult>> TransactionSearch(Guid estateId, TransactionSearchRequest searchRequest, PagingRequest pagingRequest, SortingRequest sortingRequest, CancellationToken cancellationToken)
        {
            // Base query before any filtering is added
            using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, estateId.ToString());
            await using EstateManagementContext context = resolvedContext.Context;
            
            IQueryable<DatabaseProjections.TransactionSearchProjection> mainQuery = BuildTransactionSearchQuery(context, searchRequest.QueryDate);

            mainQuery = mainQuery.ApplyFilters(searchRequest);
            mainQuery = mainQuery.ApplySorting(sortingRequest);
            mainQuery = mainQuery.ApplyPagination(pagingRequest);

            // Now build the results
            List<DatabaseProjections.TransactionSearchProjection> queryResults = await mainQuery.ToListAsync(cancellationToken);

            List<TransactionResult> results = new List<TransactionResult>();

            queryResults.ForEach(qr =>
            {
                results.Add(new TransactionResult
                {
                    MerchantReportingId = qr.Merchant.MerchantReportingId,
                    ResponseCode = qr.Transaction.ResponseCode,
                    IsAuthorised = qr.Transaction.IsAuthorised,
                    MerchantName = qr.Merchant.Name,
                    OperatorName = qr.Operator.Name,
                    OperatorReportingId = qr.Operator.OperatorReportingId,
                    Product = qr.Product.ProductName,
                    ProductReportingId = qr.Product.ContractProductReportingId,
                    ResponseMessage = qr.Transaction.ResponseMessage,
                    TransactionDateTime = qr.Transaction.TransactionDateTime,
                    TransactionId = qr.Transaction.TransactionId,
                    TransactionReportingId = qr.Transaction.TransactionReportingId,
                    TransactionSource = qr.Transaction.TransactionSource.ToString(), // TODO: Name for this
                    TransactionAmount = qr.Transaction.TransactionAmount
                });
            });

            return results;
        }

    }

    public class DatabaseProjections {
        public class FeeTransactionProjection
        {
            public MerchantSettlementFee Fee { get; set; }
            public Transaction Txn { get; set; }
        }

        public class TransactionSearchProjection {
            public Transaction Transaction { get; set; }
            public Operator Operator { get; set; }
            public Merchant Merchant { get; set; }
            public ContractProduct Product { get; set; }
        }
    }
}
