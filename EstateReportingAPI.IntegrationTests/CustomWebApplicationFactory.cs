using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
    private readonly string DatabaseConnectionString;

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
            var context = new EstateManagementSqlServerContext(DatabaseConnectionString);
            Func<string, EstateManagementGenericContext> f = connectionString => context;

            IDbContextFactory<EstateManagementGenericContext> factory = new DbContextFactory<EstateManagementGenericContext>(new TestConnectionStringConfigurationRepository(DatabaseConnectionString), f);

            IReportingManager manager = new ReportingManager(factory);

            containerBuilder.AddSingleton(manager);

            containerBuilder.Configure<TestAuthHandlerOptions>(options => options.DefaultUserId = DefaultUserId);

            containerBuilder.AddAuthentication(TestAuthHandler.AuthenticationScheme)
                            .AddScheme<TestAuthHandlerOptions, TestAuthHandler>(TestAuthHandler.AuthenticationScheme, options => { });
            
            bool b = context.Database.EnsureCreated();
            
            b.ShouldBeTrue();
        });

    }

}

public class TestConnectionStringConfigurationRepository : IConnectionStringConfigurationRepository
{
    private readonly string DbConnectionString;

    public TestConnectionStringConfigurationRepository(String dbConnectionString)
    {
        DbConnectionString = dbConnectionString;
    }
    public Task DeleteConnectionStringConfiguration(string externalIdentifier, string connectionStringIdentifier,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<string> GetConnectionString(string externalIdentifier, string connectionStringIdentifier,
        CancellationToken cancellationToken)
    {
        return DbConnectionString;
    }

    public Task CreateConnectionString(string externalIdentifier, string connectionStringIdentifier, string connectionString,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
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