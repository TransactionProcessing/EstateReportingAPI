using TransactionProcessor.Database.Contexts;

namespace EstateReportingAPI.Bootstrapper;

using BusinessLogic;
using Common;
using Lamar;
using Microsoft.EntityFrameworkCore;
using Shared.EntityFramework;
using Shared.EntityFramework.ConnectionStringConfiguration;
using Shared.General;
using Shared.Repositories;
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class RepositoryRegistry : ServiceRegistry{
    public RepositoryRegistry(){
        String? inTestMode = Environment.GetEnvironmentVariable("InTestMode");
        if (String.Compare(inTestMode, Boolean.TrueString, StringComparison.InvariantCultureIgnoreCase) != 0)
        {
            this.AddSingleton<IReportingManager, ReportingManager>();
        }

        this.AddSingleton(typeof(IDbContextResolver<>), typeof(DbContextResolver<>));
        if (Startup.WebHostEnvironment.IsEnvironment("IntegrationTest") || Startup.Configuration.GetValue<Boolean>("ServiceOptions:UseInMemoryDatabase") == true)
        {
            this.AddDbContext<EstateManagementContext>(builder => builder.UseInMemoryDatabase("TransactionProcessorReadModel"));
        }
        else
        {
            this.AddDbContext<EstateManagementContext>(options =>
                options.UseSqlServer(ConfigurationReader.GetConnectionString("TransactionProcessorReadModel")));
        }
    }
}