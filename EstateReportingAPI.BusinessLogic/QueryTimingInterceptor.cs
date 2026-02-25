using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.EntityFramework;
using Shared.Logger;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Text;
using Shared.General;

namespace EstateReportingAPI.BusinessLogic;

public class QueryTimingInterceptor : DbCommandInterceptor {

    internal void LogIfRequired(DbCommand command,
                                CommandExecutedEventData eventData) {

        Int32 threshold = ConfigurationReader.GetValueOrDefault<int>("AppSettings", "EFQueryPerformanceThresholdMs", 500);

        if (eventData.Duration.TotalMilliseconds < threshold)
            return;
        Logger.LogWarning($"PERFORMANCE - EF Query took {eventData.Duration.TotalMilliseconds} ms\n{command.CommandText}\n");
    }

    public override DbDataReader ReaderExecuted(DbCommand command,
                                                CommandExecutedEventData eventData,
                                                DbDataReader result) {
        LogIfRequired(command, eventData);
        return result;
    }

    public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command,
                                                                      CommandExecutedEventData eventData,
                                                                      DbDataReader result,
                                                                      CancellationToken cancellationToken = new CancellationToken()) {
        LogIfRequired(command, eventData);

        return result;
    }
}


public class DbContextResolverX<TContext> : IDbContextResolver<TContext> where TContext : DbContext
{
    private readonly IServiceProvider _rootProvider;
    private readonly IConfiguration _config;
    private readonly DbCommandInterceptor Interceptor;

    public DbContextResolverX(IServiceProvider rootProvider,
                              IConfiguration config,
                              DbCommandInterceptor interceptor)
    {
        _rootProvider = rootProvider;
        _config = config;
        this.Interceptor = interceptor;
    }

    public ResolvedDbContext<TContext> Resolve(String connectionStringKey)
    {
        return this.Resolve(connectionStringKey, String.Empty);
    }

    public ResolvedDbContext<TContext> Resolve(String connectionStringKey,
                                               String databaseNameSuffix)
    {
        IServiceScope scope = _rootProvider.CreateScope();
        String connectionString = _config.GetConnectionString(connectionStringKey);
        if (String.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException($"Connection string for '{connectionStringKey}' not found.");

        // Update the connection string with the identifier if needed
        if (!String.IsNullOrWhiteSpace(databaseNameSuffix))
        {
            SqlConnectionStringBuilder builder = new(connectionString);
            builder.InitialCatalog = $"{builder.InitialCatalog}-{databaseNameSuffix}";
            connectionString = builder.ConnectionString;


            // Create an isolated service collection and provider
            ServiceCollection services = new();
            services.AddDbContext<TContext>(options => {
                options.UseSqlServer(connectionString);
                options.AddInterceptors(Interceptor); // attach here
            });

            ServiceProvider provider = services.BuildServiceProvider();
            scope = provider.CreateScope();
            // Standard resolution using DI container
        }

        return new ResolvedDbContext<TContext>(scope);
    }
}
