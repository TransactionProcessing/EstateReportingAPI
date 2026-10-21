using Microsoft.EntityFrameworkCore.Diagnostics;
using TransactionProcessor.Database.Contexts;

namespace EstateReportingAPI.Bootstrapper;

using BusinessLogic;
using EstateReportingAPI.BusinessLogic.Services;
using Lamar;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Shared.EntityFramework;
using Shared.General;
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class RepositoryRegistry : ServiceRegistry{
    public RepositoryRegistry(IConfiguration configuration, IHostEnvironment environment) {
        this.AddSingleton<ReportingDatePolicy>(_ => new ReportingDatePolicy(TimeProvider.System));
        this.AddSingleton<ITransactionReportingService, TransactionReportingService>();
        this.AddSingleton<IEstateReportingService, EstateReportingService>();
        this.AddSingleton<ISettlementReportingService, SettlementReportingService>();
        this.AddSingleton<IFileImportReportingService, FileImportReportingService>();
        this.AddSingleton<IMerchantReportingService, MerchantReportingService>();

        this.AddSingleton<DbCommandInterceptor, QueryTimingInterceptor>();
        this.AddSingleton(typeof(IDbContextResolver<>), typeof(DbContextResolver<>));

        if (environment.IsEnvironment("IntegrationTest") || configuration.GetValue<Boolean>("ServiceOptions:UseInMemoryDatabase")) {
            this.AddDbContext<EstateManagementContext>(builder => { builder.UseInMemoryDatabase("TransactionProcessorReadModel"); });
        }
        else {
            SqlServerRetryOptions? retryOptions = configuration.GetSection("AppSettings:SqlServerRetry").Get<SqlServerRetryOptions>();

            if (retryOptions != null) {
                this.AddDbContext<EstateManagementContext>(options => {
                    options.UseSharedSqlServer<EstateManagementContext>(configuration.GetConnectionString("TransactionProcessorReadModel")!, retry => {
                        retry.AdditionalTransientErrorNumbers = retryOptions.AdditionalTransientErrorNumbers;
                        retry.MaxRetryCount = retryOptions.MaxRetryCount;
                        retry.MaxRetryDelay = retryOptions.MaxRetryDelay;
                    });
                });
            }
            else {
                this.AddDbContext<EstateManagementContext>(options => { options.UseSqlServer(configuration.GetConnectionString("TransactionProcessorReadModel")!, retry => { retry.EnableRetryOnFailure(); }); });

            }
        }
    }
}
