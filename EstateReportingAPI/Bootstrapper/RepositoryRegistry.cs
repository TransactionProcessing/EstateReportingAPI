using Microsoft.EntityFrameworkCore.Diagnostics;
using TransactionProcessor.Database.Contexts;

namespace EstateReportingAPI.Bootstrapper;

using BusinessLogic;
using Lamar;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Options;
using Shared.EntityFramework;
using Shared.General;
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class RepositoryRegistry : ServiceRegistry{
    public RepositoryRegistry(){
        String? inTestMode = Environment.GetEnvironmentVariable("InTestMode");
        if (String.Compare(inTestMode, Boolean.TrueString, StringComparison.InvariantCultureIgnoreCase) != 0)
        {
            this.AddSingleton<IReportingManager, ReportingManager>();
        }
        this.AddSingleton<DbCommandInterceptor, QueryTimingInterceptor>();
        this.AddSingleton(typeof(IDbContextResolver<>), typeof(DbContextResolverX<>));
        
        if (Startup.WebHostEnvironment.IsEnvironment("IntegrationTest") || Startup.Configuration.GetValue<Boolean>("ServiceOptions:UseInMemoryDatabase") == true)
        {
            this.AddDbContext<EstateManagementContext>(builder => {
                builder.UseInMemoryDatabase("TransactionProcessorReadModel");
            });
        }
        else
        {
            SqlServerRetryOptions retryOptions;
            try
            {
                retryOptions = ConfigurationReader.GetSection<SqlServerRetryOptions>("AppSettings:SqlServerRetry");
            }
            catch (KeyNotFoundException)
            {
                retryOptions = null;
            }

            if (retryOptions != null)
            {
                this.AddDbContext<EstateManagementContext>(options => {

                    options.UseSharedSqlServer<EstateManagementContext>(ConfigurationReader.GetConnectionString("TransactionProcessorReadModel"), retry => {
                        retry.AdditionalTransientErrorNumbers = retryOptions.AdditionalTransientErrorNumbers;
                        retry.MaxRetryCount = retryOptions.MaxRetryCount;
                        retry.MaxRetryDelay = retryOptions.MaxRetryDelay;
                    });
                });
            }
            else
            {
                this.AddDbContext<EstateManagementContext>(options => {
                    options.UseSqlServer(ConfigurationReader.GetConnectionString("TransactionProcessorReadModel"), retry => {
                        retry.EnableRetryOnFailure();
                    });
                });

            }
        }
}