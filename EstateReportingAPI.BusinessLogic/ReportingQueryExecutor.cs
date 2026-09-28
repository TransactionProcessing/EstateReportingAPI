using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.DynamicLinq;
using SimpleResults;

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
            return Result.Success(item);
        }
        catch (Exception ex)
        {
            return Result.Failure(BuildErrorMessage(ex, contextMessage));
        }
    }

    public static async Task<Result<List<T>>> ToListAsync<T>(IQueryable<T> query,
                                                              CancellationToken cancellationToken,
                                                              string? contextMessage = null)
    {
        try
        {
            List<T> items = await query.ToListAsync(cancellationToken);
            return Result.Success(items);
        }
        catch (Exception ex)
        {
            return Result.Failure(BuildErrorMessage(ex, contextMessage));
        }
    }

    public static async Task<Result<int>> CountAsync<T>(IQueryable<T> query,
                                                      CancellationToken cancellationToken,
                                                      string? contextMessage = null)
    {
        try
        {
            int count = await query.CountAsync(cancellationToken);
            return Result.Success(count);
        }
        catch (Exception ex)
        {
            return Result.Failure(BuildErrorMessage(ex, contextMessage));
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
                return Result.NotFound(contextMessage);

            return Result.Success(item);
        }
        catch (Exception ex)
        {
            return Result.Failure(BuildErrorMessage(ex, contextMessage));
        }
    }

    private static string BuildErrorMessage(Exception exception, string? contextMessage)
    {
        return contextMessage == null
            ? $"Error executing query: {exception.Message}"
            : $"{contextMessage}: {exception.Message}";
    }
}
