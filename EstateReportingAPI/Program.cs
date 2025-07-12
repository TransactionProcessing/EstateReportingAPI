using Lamar.Microsoft.DependencyInjection;
using NLog;
using System.Diagnostics.CodeAnalysis;
using NLog.Extensions.Logging;
using Shared.Logger;
using Shared.Middleware;

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
        FileInfo fi = new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);

        IConfigurationRoot config = new ConfigurationBuilder().SetBasePath(fi.Directory.FullName)
                                                              .AddJsonFile("hosting.json", optional: true)
                                                              .AddJsonFile("hosting.development.json", optional: true)
                                                              .AddEnvironmentVariables().Build();

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

        IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args);
        hostBuilder.UseWindowsService();
        hostBuilder.UseLamar();
        hostBuilder.ConfigureLogging(logging =>
                                     {
                                         logging.AddConsole();
                                         logging.AddNLog();
                                     });
        hostBuilder.ConfigureWebHostDefaults(webBuilder =>
                                             {
                                                 webBuilder.UseStartup<Startup>();
                                                 webBuilder.UseConfiguration(config);
                                                 webBuilder.UseKestrel();
                                             });
        return hostBuilder;
    }
}