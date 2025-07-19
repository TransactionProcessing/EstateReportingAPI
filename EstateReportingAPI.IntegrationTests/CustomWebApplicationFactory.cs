using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Shared.Repositories;
using TransactionProcessor.Database.Contexts;

namespace EstateReportingAPI.IntegrationTests;

using BusinessLogic;
using EstateReportingAPI.Common;
using k8s.KubeConfigModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.EntityFramework;
using Shouldly;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.TestHost;

public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    private string DatabaseConnectionString;

    public CustomWebApplicationFactory(string databaseConnectionString)
    {
        DatabaseConnectionString = databaseConnectionString;
        Environment.SetEnvironmentVariable("InTestMode", "true");
    }

    public string DefaultUserId { get; set; } = "1";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(containerBuilder =>
        {
            var createcontext = new EstateManagementContext(DatabaseConnectionString);
            bool b = createcontext.Database.EnsureCreated();
            b.ShouldBeTrue();

            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(DatabaseConnectionString)
            {
                InitialCatalog = "TransactionProcessorReadModel",
            };
            this.DatabaseConnectionString = builder.ToString();

            var context = new EstateManagementContext(DatabaseConnectionString);
            Func<string, EstateManagementContext> f = connectionString => context;

            containerBuilder.AddTransient<EstateManagementContext>(_ => context);
            var serviceProvider = containerBuilder.BuildServiceProvider();
            
            var inMemorySettings = new Dictionary<string, string>
            {
                { "ConnectionStrings:TransactionProcessorReadModel", DatabaseConnectionString }
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            IDbContextResolver<EstateManagementContext> resolver = new DbContextResolver<EstateManagementContext>(serviceProvider, configuration);
            IReportingManager manager = new ReportingManager(resolver);

            containerBuilder.AddSingleton(manager);

            containerBuilder.Configure<TestAuthHandlerOptions>(options => options.DefaultUserId = DefaultUserId);

            containerBuilder.AddAuthentication(TestAuthHandler.AuthenticationScheme)
                            .AddScheme<TestAuthHandlerOptions, TestAuthHandler>(TestAuthHandler.AuthenticationScheme, options => { });
            
            
        });

    }

}

public class TestAuthHandlerOptions : AuthenticationSchemeOptions
{
    public string DefaultUserId { get; set; } = null!;
}

public class TestAuthHandler : AuthenticationHandler<TestAuthHandlerOptions>
{
    public const string UserId = "UserId";

    public const string AuthenticationScheme = "Test";
    private readonly string _defaultUserId;

    public TestAuthHandler(
        IOptionsMonitor<TestAuthHandlerOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock) : base(options, logger, encoder, clock)
    {
        _defaultUserId = options.CurrentValue.DefaultUserId;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new List<Claim> { new Claim(ClaimTypes.Name, "Test user") };

        // Extract User ID from the request headers if it exists,
        // otherwise use the default User ID from the options.
        if (Context.Request.Headers.TryGetValue(UserId, out var userId))
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId[0]));
        }
        else
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, _defaultUserId));
        }

        // TODO: Add as many claims as you need here

        var identity = new ClaimsIdentity(claims, AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

        var result = AuthenticateResult.Success(ticket);

        return Task.FromResult(result);
    }
}