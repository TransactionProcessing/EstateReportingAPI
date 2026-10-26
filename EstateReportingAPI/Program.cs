using EstateReportingAPI.Bootstrapper;
using EstateReportingAPI.Common;
using EstateReportingAPI.Endpoints;
using HealthChecks.UI.Client;
using Lamar;
using Lamar.Microsoft.DependencyInjection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using NLog;
using NLog.Extensions.Logging;
using Sentry.Extensibility;
using Shared.Extensions;
using Shared.Logger;
using Shared.Middleware;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TransactionProcessing.ApiDefaults;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace EstateReportingAPI;

[ExcludeFromCodeCoverage]
public partial class Program
{
    protected Program()
    {
    }

    public static void Main(string[] args)
    {
        ConfigureLogging();

        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        ConfigureHost(builder);
        WebApplication app = builder.Build();
        ConfigurePipeline(app);
        app.Run();
    }

    private static void ConfigureLogging()
    {
        string nlogConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "nlog.config");
        LogManager.Setup(b =>
        {
            b.SetupLogFactory(setup =>
            {
                setup.AddCallSiteHiddenAssembly(typeof(NlogLogger).Assembly);
                setup.AddCallSiteHiddenAssembly(typeof(Shared.Logger.Logger).Assembly);
                setup.AddCallSiteHiddenAssembly(typeof(TenantMiddleware).Assembly);
            });
            b.LoadConfigurationFromFile(nlogConfigPath);
        });
    }

    private static void ConfigureHost(WebApplicationBuilder builder)
    {
        builder.Host.UseWindowsService();
        builder.Logging.AddConsole();
        builder.Logging.AddNLog();
        builder.WebHost.UseKestrel();

        string basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        builder.Configuration.SetBasePath(basePath)
            .AddJsonFile("hosting.json", optional: true)
            .AddJsonFile($"hosting.{builder.Environment.EnvironmentName}.json", optional: true)
            .AddJsonFile("/home/txnproc/config/appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"/home/txnproc/config/appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddJsonFile("/home/txnproc/config/appsettings.local.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HealthMonitoring:Service:Version"] = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "0.0.0.0"
            });

        Shared.General.ConfigurationReader.Initialise(builder.Configuration);
        if (builder.Configuration.GetSection("SentryConfiguration").Exists() && !builder.Environment.IsDevelopment())
        {
            builder.WebHost.UseSentry(options =>
            {
                options.Dsn = builder.Configuration["SentryConfiguration:Dsn"];
                options.SendDefaultPii = true;
                options.MaxRequestBodySize = RequestSize.Always;
                options.CaptureBlockingCalls = builder.Configuration.GetValue("SentryConfiguration:CaptureBlockingCalls", false);
                options.IncludeActivityData = builder.Configuration.GetValue("SentryConfiguration:IncludeActivityData", false);
                options.Release = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
            });
        }
        builder.Host.UseLamar((context, services) =>
        {
            services.IncludeRegistry(new MiddlewareRegistry(builder.Configuration, context.HostingEnvironment));
            services.IncludeRegistry(new RepositoryRegistry(builder.Configuration, context.HostingEnvironment));
            services.IncludeRegistry(new MediatorRegistry(builder.Configuration));
            services.IncludeRegistry(new SerialiserRegistry());
        });
    }

    private static void ConfigurePipeline(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        LogManager.Setup().LoadConfigurationFromFile("NLog.config");
        NLog.Logger logger = LogManager.GetLogger("EstateReporting");
        Shared.Logger.Logger.Initialise(logger, "EstateReporting");

        IConfigurationRoot appConfiguration = (IConfigurationRoot)app.Configuration;
        appConfiguration.LogConfiguration(Shared.Logger.Logger.LogWarning);

        app.UseMiddleware<TenantMiddleware>();
        app.AddRequestResponseLogging();
        app.AddExceptionHandler();

        app.UseRouting();
        app.UseAuthentication();
        app.UseMiddleware<EstateContextMiddleware>();
        app.UseAuthorization();

        IConfiguration configuration = app.Configuration;
        app.MapCalendarEndpoints(configuration);
        app.MapEstateEndpoints(configuration);
        app.MapOperatorEndpoints(configuration);
        app.MapMerchantEndpoints(configuration);
        app.MapContractEndpoints(configuration);
        app.MapTransactionEndpoints(configuration);
        app.MapSettlementEndpoints(configuration);
        app.MapFileImportLogEndpoints(configuration);
        app.MapFileProfileConfigurationEndpoints(configuration);
        app.MapHealthChecks("health", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = Shared.HealthChecks.HealthCheckMiddleware.WriteResponse
        }).AllowAnonymous();
        app.MapHealthChecks("healthui", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        }).AllowAnonymous();
        app.UseApiDefaults();
    }
}

