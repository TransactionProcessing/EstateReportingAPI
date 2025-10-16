using EstateReportingAPI.Models;
using TransactionProcessor.Database.Contexts;
using TransactionProcessor.Database.Entities.Summary;

namespace EstateReportingAPI.BusinessLogic;

public static class ReportingManagerExtensions{
    public static IQueryable<TodayTransaction> ApplyMerchantFilter(this IQueryable<TodayTransaction> query, List<int> merchantReportingIds)
    {
        if (merchantReportingIds == null || merchantReportingIds.Count == 0)
            return query;

        return query.Where(t => merchantReportingIds.Contains(t.MerchantReportingId)).AsQueryable();
    }

    public static IQueryable<TransactionHistory> ApplyMerchantFilter(this IQueryable<TransactionHistory> query, List<int> merchantReportingIds)
    {
        if (merchantReportingIds == null || merchantReportingIds.Count == 0)
            return query;

        return query.Where(t => merchantReportingIds.Contains(t.MerchantReportingId)).AsQueryable();
    }


    public static IQueryable<DatabaseProjections.FeeTransactionProjection> ApplyMerchantFilter(this IQueryable<DatabaseProjections.FeeTransactionProjection> query,
                                                                                               EstateManagementContext context,
                                                                                               List<int> merchantIds)
    {
        if (merchantIds == null || merchantIds.Count == 0)
            return query;

        return from q in query
            join m in context.Merchants
                on q.Txn.MerchantId equals m.MerchantId
            where merchantIds.Contains(m.MerchantReportingId)
            select q;
    }

    public static IQueryable<DatabaseProjections.FeeTransactionProjection> ApplyOperatorFilter(this IQueryable<DatabaseProjections.FeeTransactionProjection> query,
                                                                                               EstateManagementContext context,
                                                                                               List<int> operatorIds)
    {
        if (operatorIds == null || operatorIds.Count == 0)
            return query;

        return from q in query
            join o in context.Operators
                on q.Txn.OperatorId equals o.OperatorId
            where operatorIds.Contains(o.OperatorReportingId)
            select q;
    }

    public static IQueryable<DatabaseProjections.FeeTransactionProjection> ApplyProductFilter(this IQueryable<DatabaseProjections.FeeTransactionProjection> query,
                                                                                              EstateManagementContext context,
                                                                                              List<int> productIds)
    {
        if (productIds == null || productIds.Count == 0)
            return query;

        return from q in query
            join cp in context.ContractProducts
                on q.Txn.ContractProductId equals cp.ContractProductId
            where productIds.Contains(cp.ContractProductReportingId)
            select q;
    }

    public static IQueryable<UnsettledFee> ApplyProductGrouping(this IQueryable<DatabaseProjections.FeeTransactionProjection> fees,
                                                                EstateManagementContext context)
    {
        return from f in fees
            join cp in context.ContractProducts on f.Txn.ContractProductId equals cp.ContractProductId
            join c in context.Contracts on cp.ContractId equals c.ContractId
            join op in context.Operators on c.OperatorId equals op.OperatorId
            group f by new { op.Name, cp.ProductName } into g
            select new UnsettledFee
            {
                DimensionName = $"{g.Key.Name} - {g.Key.ProductName}",
                FeesValue = g.Sum(x => x.Fee.CalculatedValue),
                FeesCount = g.Count()
            };
    }

    public static IQueryable<UnsettledFee> ApplyMerchantGrouping(this IQueryable<DatabaseProjections.FeeTransactionProjection> fees,
                                                                 EstateManagementContext context)
    {
        return from f in fees
            join merchant in context.Merchants on f.Fee.MerchantId equals merchant.MerchantId
            group f by merchant.Name into g
            select new UnsettledFee
            {
                DimensionName = g.Key,
                FeesValue = g.Sum(x => x.Fee.CalculatedValue),
                FeesCount = g.Count()
            };
    }

    public static IQueryable<UnsettledFee> ApplyOperatorGrouping(this IQueryable<DatabaseProjections.FeeTransactionProjection> fees,
                                                                 EstateManagementContext context)
    {
        return from f in fees
            join op in context.Operators on f.Txn.OperatorId equals op.OperatorId
            group f by op.Name into g
            select new UnsettledFee
            {
                DimensionName = g.Key,
                FeesValue = g.Sum(x => x.Fee.CalculatedValue),
                FeesCount = g.Count()
            };
    }

    public static IQueryable<DatabaseProjections.TransactionSearchProjection> ApplyOperatorFilters(this IQueryable<DatabaseProjections.TransactionSearchProjection> transactions,
                                                                                                   List<Int32> operators) {

        if (operators != null && operators.Any()) {
            return transactions.Where(m => operators.Contains(m.Operator.OperatorReportingId));
        }
        return transactions;
    }

    public static IQueryable<DatabaseProjections.TransactionSearchProjection> ApplyMerchantFilters(this IQueryable<DatabaseProjections.TransactionSearchProjection> transactions,
                                                                                                   List<Int32> merchants)
    {

        if (merchants != null && merchants.Any())
        {
            return transactions.Where(m => merchants.Contains(m.Merchant.MerchantReportingId));
        }
        return transactions;
    }

    public static IQueryable<DatabaseProjections.TransactionSearchProjection> ApplyValueRangeFilters(this IQueryable<DatabaseProjections.TransactionSearchProjection> transactions,
                                                                                                   ValueRange valueRange)
    {

        if (valueRange != null) {
            return transactions.Where(m => m.Transaction.TransactionAmount >= valueRange.StartValue && m.Transaction.TransactionAmount <= valueRange.EndValue);
        }

        return transactions;
    }

    public static IQueryable<DatabaseProjections.TransactionSearchProjection> ApplyAuthCodeFilters(this IQueryable<DatabaseProjections.TransactionSearchProjection> transactions,
                                                                                                     String authCode)
    {

        if (String.IsNullOrEmpty(authCode) == false)
        {
            transactions = transactions.Where(m => m.Transaction.AuthorisationCode == authCode);
        }

        return transactions;
    }

    public static IQueryable<DatabaseProjections.TransactionSearchProjection> ApplyResponseCodeFilters(this IQueryable<DatabaseProjections.TransactionSearchProjection> transactions,
                                                                                                   String responseCode)
    {
        if (String.IsNullOrEmpty(responseCode) == false) {
            return transactions.Where(m => m.Transaction.ResponseCode == responseCode);
        }

        return transactions;
    }

    public static IQueryable<DatabaseProjections.TransactionSearchProjection> ApplyTransactionNumberFilters(this IQueryable<DatabaseProjections.TransactionSearchProjection> transactions,
                                                                                                       String transactionNumber)
    {
        if (String.IsNullOrEmpty(transactionNumber) == false) {
            return transactions.Where(m => m.Transaction.TransactionNumber == transactionNumber);
        }

        return transactions;
    }


    public static IQueryable<DatabaseProjections.TransactionSearchProjection> ApplyFilters(this IQueryable<DatabaseProjections.TransactionSearchProjection> transactions,
                                                                                           TransactionSearchRequest searchRequest) {

        transactions = transactions.ApplyOperatorFilters(searchRequest.Operators);
        transactions = transactions.ApplyMerchantFilters(searchRequest.Merchants);
        transactions = transactions.ApplyValueRangeFilters(searchRequest.ValueRange);
        transactions = transactions.ApplyAuthCodeFilters(searchRequest.AuthCode);
        transactions = transactions.ApplyResponseCodeFilters(searchRequest.ResponseCode);
        transactions = transactions.ApplyTransactionNumberFilters(searchRequest.TransactionNumber);

        return transactions;
    }

    public static IQueryable<T> ApplyPagination<T>(this IQueryable<T> records,
                                                                                              PagingRequest pagingRequest) {
        Int32 skipCount = 0;
        if (pagingRequest.Page > 1)
        {
            skipCount = (pagingRequest.Page - 1) * pagingRequest.PageSize;
        }
        return records.Skip(skipCount).Take(pagingRequest.PageSize);
    }

    public static IQueryable<DatabaseProjections.TransactionSearchProjection> ApplySorting(this IQueryable<DatabaseProjections.TransactionSearchProjection> records,
                                                                                              SortingRequest sortingRequest) {
        if (sortingRequest == null)
            return records;
        
        // Handle order by here, cant think of a better way of achieving this
        return (sortingRequest.SortDirection, sortingRequest.SortField) switch
        {
            (SortDirection.Ascending, SortField.MerchantName) => records.OrderBy(m => m.Merchant.Name),
            (SortDirection.Ascending, SortField.OperatorName) => records.OrderBy(m => m.Operator.Name),
            (SortDirection.Ascending, SortField.TransactionAmount) => records.OrderBy(m => m.Transaction.TransactionAmount),
            (SortDirection.Descending, SortField.MerchantName) => records.OrderByDescending(m => m.Merchant.Name),
            (SortDirection.Descending, SortField.OperatorName) => records.OrderByDescending(m => m.Operator.Name),
            (SortDirection.Descending, SortField.TransactionAmount) => records.OrderByDescending(m => m.Transaction.TransactionAmount),
            _ => records
        };
    }
}