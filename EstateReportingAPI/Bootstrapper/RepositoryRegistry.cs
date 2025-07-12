using TransactionProcessor.Database.Contexts;

namespace EstateReportingAPI.Bootstrapper;

using BusinessLogic;
using Common;
using Lamar;
using Shared.EntityFramework;
using Shared.EntityFramework.ConnectionStringConfiguration;
using Shared.General;
using Shared.Repositories;
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class RepositoryRegistry : ServiceRegistry{
    public RepositoryRegistry(){

        Boolean useConnectionStringConfig = bool.Parse(ConfigurationReader.GetValue("AppSettings", "UseConnectionStringConfig"));

        if (useConnectionStringConfig)
        {
            String connectionStringConfigurationConnString = ConfigurationReader.GetConnectionString("ConnectionStringConfiguration");
            this.AddSingleton<IConnectionStringConfigurationRepository, ConnectionStringConfigurationRepository>();
            this.AddTransient(c => { return new ConnectionStringConfigurationContext(connectionStringConfigurationConnString); });

            // TODO: Read this from a the database and set
        }
        else
        {
            this.AddSingleton<IConnectionStringConfigurationRepository, ConfigurationReaderConnectionStringRepository>();
        }

        String? inTestMode = Environment.GetEnvironmentVariable("InTestMode");
        if (String.Compare(inTestMode, Boolean.TrueString, StringComparison.InvariantCultureIgnoreCase) != 0){
            this.AddSingleton<IReportingManager, ReportingManager>();
        }

        this.AddSingleton<IDbContextFactory<EstateManagementContext>, DbContextFactory<EstateManagementContext>>();

        this.AddSingleton<Func<String, EstateManagementContext>>(cont => connectionString => {
            return new EstateManagementContext(connectionString);
        });
    }
}