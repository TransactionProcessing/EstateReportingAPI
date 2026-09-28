using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using TransactionProcessor.Database.Contexts;

namespace EstateReportingAPI.IntegrationTests;

using BusinessLogic;
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
        Environment.SetEnvironmentVariable("AppSettings__DisableAuthorisation", "true");
    }

    public string DefaultUserId { get; set; } = "1";
    public Guid DefaultEstateId { get; set; }

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
            containerBuilder.AddSingleton(resolver);

            containerBuilder.Configure<TestAuthHandlerOptions>(options =>
            {
                options.DefaultUserId = DefaultUserId;
                options.DefaultEstateId = DefaultEstateId;
            });

            containerBuilder.AddAuthentication(TestAuthHandler.AuthenticationScheme)
                            .AddScheme<TestAuthHandlerOptions, TestAuthHandler>(TestAuthHandler.AuthenticationScheme, options => { });


        });

    }

}

public class TestAuthHandlerOptions : AuthenticationSchemeOptions
{
    public string DefaultUserId { get; set; } = null!;
    public Guid DefaultEstateId { get; set; }
}

public class TestAuthHandler : AuthenticationHandler<TestAuthHandlerOptions>
{
    public const string UserId = "UserId";
    public const string OmitEstateClaim = "Test-Omit-Estate-Claim";
    public const string EstateClaim = "Test-Estate-Claim";
    public const string EstateClaimType = "Test-Estate-Claim-Type";

    public const string AuthenticationScheme = "Test";
    private readonly string _defaultUserId;
    private readonly Guid _defaultEstateId;

    public TestAuthHandler(
        IOptionsMonitor<TestAuthHandlerOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock) : base(options, logger, encoder, clock)
    {
        _defaultUserId = options.CurrentValue.DefaultUserId;
        _defaultEstateId = options.CurrentValue.DefaultEstateId;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Context.Request.Headers.TryGetValue("Authorization", out var authorization) == false ||
            AuthenticationHeaderValue.TryParse(authorization.ToString(), out AuthenticationHeaderValue? authenticationHeader) == false ||
            String.Equals(authenticationHeader.Scheme, AuthenticationScheme, StringComparison.OrdinalIgnoreCase) == false)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

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

        if (Context.Request.Headers.ContainsKey(OmitEstateClaim) == false)
        {
            string estateClaim = Context.Request.Headers.TryGetValue(EstateClaim, out var requestedEstateClaim)
                ? requestedEstateClaim.ToString()
                : _defaultEstateId.ToString();

            string estateClaimType = Context.Request.Headers.TryGetValue(EstateClaimType, out var requestedEstateClaimType)
                ? requestedEstateClaimType.ToString()
                : "estateId";

            claims.Add(new Claim(estateClaimType, estateClaim));
        }

        // TODO: Add as many claims as you need here

        var identity = new ClaimsIdentity(claims, AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

        var result = AuthenticateResult.Success(ticket);

        return Task.FromResult(result);
    }
}
