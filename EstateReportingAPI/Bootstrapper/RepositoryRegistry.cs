using Microsoft.EntityFrameworkCore.Diagnostics;
using TransactionProcessor.Database.Contexts;

namespace EstateReportingAPI.Bootstrapper;

using BusinessLogic;
using Common;
using Lamar;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
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
            this.AddDbContext<EstateManagementContext>(builder => builder.UseInMemoryDatabase("TransactionProcessorReadModel"));
        }
        else {
            this.AddSingleton<QueryTimingInterceptor>();
            this.AddDbContext<EstateManagementContext>((sp, options) =>
            {
                options.UseSqlServer(ConfigurationReader.GetConnectionString("TransactionProcessorReadModel"));
                options.AddInterceptors(sp.GetRequiredService<QueryTimingInterceptor>());
            });
        }
    }
}