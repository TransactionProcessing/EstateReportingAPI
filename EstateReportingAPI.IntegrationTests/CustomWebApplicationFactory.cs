namespace EstateReportingAPI.IntegrationTests;

using BusinessLogic;
using EstateManagement.Database.Contexts;
using EstateReportingAPI.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shared.EntityFramework;
using Shouldly;

public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    private readonly string DatabaseConnectionString;

    public CustomWebApplicationFactory(string databaseConnectionString)
    {
        DatabaseConnectionString = databaseConnectionString;
        Environment.SetEnvironmentVariable("InTestMode", "true");
    }
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(containerBuilder =>
        {
            var context = new EstateManagementSqlServerContext(DatabaseConnectionString);
            Func<string, EstateManagementGenericContext> f = connectionString => context;

            IDbContextFactory<EstateManagementGenericContext> factory = new DbContextFactory<EstateManagementGenericContext>(new ConfigurationReaderConnectionStringRepository(), f);

            IReportingManager manager = new ReportingManager(factory);

            containerBuilder.AddSingleton(manager);

            bool b = context.Database.EnsureCreated();

            b.ShouldBeTrue();
        });
    }
}