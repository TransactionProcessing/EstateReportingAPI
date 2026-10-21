using EstateReportingAPI.BusinessLogic;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Generic;
using Shared.EntityFramework;
using Shouldly;
using TransactionProcessor.Database.Contexts;
using Xunit;

namespace EstateReportingAPI.Tests.BusinessLogic;

public class DbContextResolverXTests
{
    [Fact]
    public void Resolve_WhenEstateSuffixIsProvided_UsesTheSuffixedCatalog()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["ConnectionStrings:TransactionProcessorReadModel"] = "Server=localhost;Initial Catalog=TransactionProcessorReadModel;Integrated Security=True;TrustServerCertificate=True"
            })
            .Build();

        using ServiceProvider rootProvider = new ServiceCollection().BuildServiceProvider();
        DbContextResolver<EstateManagementContext> resolver = new(rootProvider, configuration);

        using ResolvedDbContext<EstateManagementContext> resolvedContext =
            resolver.Resolve("TransactionProcessorReadModel", "estate-123");

        SqlConnectionStringBuilder connectionStringBuilder =
            new(resolvedContext.Context.Database.GetDbConnection().ConnectionString);

        connectionStringBuilder.InitialCatalog.ShouldBe("TransactionProcessorReadModel-estate-123");
    }
}
