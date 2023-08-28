using HealthChecks.UI.Client;
using Lamar;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using NLog.Extensions.Logging;
using Shared.Extensions;
using Shared.General;
using Shared.Logger;
using System.Diagnostics.CodeAnalysis;
using Lamar.Microsoft.DependencyInjection;
using EstateReportingAPI.Bootstrapper;

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

        IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args);
        hostBuilder.UseWindowsService();
        hostBuilder.UseLamar();
        hostBuilder.ConfigureLogging(logging =>
                                     {
                                         logging.AddConsole();
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

public class Startup{
    public static IConfigurationRoot Configuration { get; set; }
    public static IWebHostEnvironment WebHostEnvironment { get; set; }

    public static IServiceProvider ServiceProvider { get; set; }

    public static Container Container;
    public Startup(IWebHostEnvironment webHostEnvironment){
        IConfigurationBuilder builder = new ConfigurationBuilder().SetBasePath(webHostEnvironment.ContentRootPath)
                                                                  .AddJsonFile("/home/txnproc/config/appsettings.json", true, true)
                                                                  .AddJsonFile($"/home/txnproc/config/appsettings.{webHostEnvironment.EnvironmentName}.json", optional:true)
                                                                  .AddJsonFile("appsettings.json", optional:true, reloadOnChange:true)
                                                                  .AddJsonFile($"appsettings.{webHostEnvironment.EnvironmentName}.json", optional:true, reloadOnChange:true)
                                                                  .AddEnvironmentVariables();

        Startup.Configuration = builder.Build();
        Startup.WebHostEnvironment = webHostEnvironment;
    }

    public void ConfigureContainer(ServiceRegistry services){

        ConfigurationReader.Initialise(Startup.Configuration);

        services.IncludeRegistry<MiddlewareRegistry>();

        Startup.Container = new Container(services);
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory){
        String nlogConfigFilename = "nlog.config";

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        loggerFactory.ConfigureNLog(Path.Combine(env.ContentRootPath, nlogConfigFilename));
        loggerFactory.AddNLog();

        Microsoft.Extensions.Logging.ILogger logger = loggerFactory.CreateLogger("EstateManagement");

        Logger.Initialise(logger);

        Action<String> loggerAction = message =>
                                      {
                                          Logger.LogInformation(message);
                                      };
        Startup.Configuration.LogConfiguration(loggerAction);

        ConfigurationReader.Initialise(Startup.Configuration);

        app.AddRequestLogging();
        app.AddResponseLogging();
        app.AddExceptionHandler();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
                         {
                             endpoints.MapControllers();
                             endpoints.MapHealthChecks("health", new HealthCheckOptions()
                                                                 {
                                                                     Predicate = _ => true,
                                                                     ResponseWriter = Shared.HealthChecks.HealthCheckMiddleware.WriteResponse
                                                                 });
                             endpoints.MapHealthChecks("healthui", new HealthCheckOptions()
                                                                   {
                                                                       Predicate = _ => true,
                                                                       ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                                                                   });
                         });
        app.UseSwagger();

        app.UseSwaggerUI();
    }
}