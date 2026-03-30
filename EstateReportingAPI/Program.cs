using Lamar.Microsoft.DependencyInjection;
using NLog;
using NLog.Extensions.Logging;
using Sentry.Extensibility;
using Shared.General;
using Shared.Logger;
using Shared.Middleware;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace EstateReportingAPI;

[ExcludeFromCodeCoverage]
public class Program{

    public static void Main(string[] args)
    {
        Program.CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args)
    {


        //At this stage, we only need our hosting file for ip and ports
        FileInfo fi = new(Assembly.GetExecutingAssembly().Location);

        ConfigureLogging();

        IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args);
        hostBuilder.UseWindowsService();
        hostBuilder.UseLamar();
        hostBuilder.ConfigureLogging(logging =>
                                     {
                                         logging.AddConsole();
                                         logging.AddNLog();
                                     });
       return ConfigureWebHost(hostBuilder, fi.Directory.FullName);
    }

    private static void ConfigureLogging() {
        String contentRoot = Directory.GetCurrentDirectory();
        String nlogConfigPath = Path.Combine(contentRoot, "nlog.config");

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

    private static IHostBuilder ConfigureWebHost(IHostBuilder hostBuilder, String basePath) {
        hostBuilder.ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.ConfigureAppConfiguration((context, configBuilder) =>
            {
                IWebHostEnvironment env = context.HostingEnvironment;

                configBuilder.SetBasePath(basePath)
                    .AddJsonFile("hosting.json", optional: true)
                    .AddJsonFile($"hosting.{env.EnvironmentName}.json", optional: true)
                    .AddJsonFile("/home/txnproc/config/appsettings.json", optional: true, reloadOnChange: true)
                    .AddJsonFile($"/home/txnproc/config/appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables();

                // Keep existing static usage (if you must), and initialise the ConfigurationReader now.
                Startup.Configuration = configBuilder.Build();
                ConfigurationReader.Initialise(Startup.Configuration);

                ConfigureSentry(env, webBuilder);
            });

            webBuilder.UseStartup<Startup>();
            webBuilder.UseKestrel();
        });
        return hostBuilder;
    }

    private static void ConfigureSentry(IWebHostEnvironment env,
                                        IWebHostBuilder webBuilder) {
        // Configure Sentry on the webBuilder using the config snapshot.
        IConfigurationSection sentrySection = Startup.Configuration.GetSection("SentryConfiguration");
        if (sentrySection.Exists())
        {
            // Replace the condition below if you intended to only enable Sentry in certain environments.
            if (env.IsDevelopment() == false)
            {
                webBuilder.UseSentry(o =>
                {
                    o.Dsn = Startup.Configuration["SentryConfiguration:Dsn"];
                    o.SendDefaultPii = true;
                    o.MaxRequestBodySize = RequestSize.Always;
                    o.CaptureBlockingCalls = ConfigurationReader.GetValueOrDefault("SentryConfiguration", "CaptureBlockingCalls", false);
                    o.IncludeActivityData = ConfigurationReader.GetValueOrDefault("SentryConfiguration", "IncludeActivityData", false);
                    o.Release = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
                });
            }
        }
    }
}
