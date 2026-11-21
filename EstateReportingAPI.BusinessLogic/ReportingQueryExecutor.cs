using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.DynamicLinq;
using TransactionProcessing.UnionResults;

namespace EstateReportingAPI.BusinessLogic;

public static class ReportingQueryExecutor
{
    public static async Task<Result<T>> SumAsync<T>(IQueryable<T> query,
                                                     CancellationToken cancellationToken,
                                                     string? contextMessage = null)
    {
        try
        {
            T item = await query.SumAsync(cancellationToken);
            return Results.Ok(item);
        }
        catch (Exception ex)
        {
            return new Error(ErrorType.Failure, BuildErrorMessage(ex, contextMessage));
        }
    }

    public static async Task<Result<List<T>>> ToListAsync<T>(IQueryable<T> query,
                                                              CancellationToken cancellationToken,
                                                              string? contextMessage = null)
    {
        try
        {
            List<T> items = await query.ToListAsync(cancellationToken);
            return Results.Ok(items);
        }
        catch (Exception ex)
        {
            return new Error(ErrorType.Failure, BuildErrorMessage(ex, contextMessage));
        }
    }

    public static async Task<Result<int>> CountAsync<T>(IQueryable<T> query,
                                                      CancellationToken cancellationToken,
                                                      string? contextMessage = null)
    {
        try
        {
            int count = await query.CountAsync(cancellationToken);
            return Results.Ok(count);
        }
        catch (Exception ex)
        {
            return new Error(ErrorType.Failure, BuildErrorMessage(ex, contextMessage));
        }
    }

    public static async Task<Result<T>> SingleOrDefaultAsync<T>(IQueryable<T> query,
                                                                 CancellationToken cancellationToken,
                                                                 string? contextMessage = null)
    {
        try
        {
            T item = await query.SingleOrDefaultAsync(cancellationToken);

            if (EqualityComparer<T>.Default.Equals(item, default))
                return new Error(ErrorType.NotFound, contextMessage ?? "No matching record found.");

            return Results.Ok(item);
        }
        catch (Exception ex)
        {
            return new Error(ErrorType.Failure, BuildErrorMessage(ex, contextMessage));
        }
    }

    private static string BuildErrorMessage(Exception exception, string? contextMessage)
    {
        return contextMessage == null
            ? $"Error executing query: {exception.Message}"
            : $"{contextMessage}: {exception.Message}";
    }
}
