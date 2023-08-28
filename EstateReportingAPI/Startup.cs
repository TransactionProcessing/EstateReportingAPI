using EstateReportingAPI.Bootstrapper;
using HealthChecks.UI.Client;
using Lamar;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using NLog.Extensions.Logging;
using Shared.Extensions;
using Shared.General;
using Shared.Logger;

namespace EstateReportingAPI
{
    public class Startup
    {
        public static IConfigurationRoot Configuration { get; set; }
        public static IWebHostEnvironment WebHostEnvironment { get; set; }

        public static IServiceProvider ServiceProvider { get; set; }

        public static Container Container;
        public Startup(IWebHostEnvironment webHostEnvironment)
        {
            IConfigurationBuilder builder = new ConfigurationBuilder().SetBasePath(webHostEnvironment.ContentRootPath)
                                                                      .AddJsonFile("/home/txnproc/config/appsettings.json", true, true)
                                                                      .AddJsonFile($"/home/txnproc/config/appsettings.{webHostEnvironment.EnvironmentName}.json", optional: true)
                                                                      .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                                                                      .AddJsonFile($"appsettings.{webHostEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                                                                      .AddEnvironmentVariables();

            Configuration = builder.Build();
            WebHostEnvironment = webHostEnvironment;
        }

        public void ConfigureContainer(ServiceRegistry services)
        {

            ConfigurationReader.Initialise(Configuration);

            services.IncludeRegistry<MiddlewareRegistry>();

            Container = new Container(services);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            string nlogConfigFilename = "nlog.config";

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            loggerFactory.ConfigureNLog(Path.Combine(env.ContentRootPath, nlogConfigFilename));
            loggerFactory.AddNLog();

            Microsoft.Extensions.Logging.ILogger logger = loggerFactory.CreateLogger("EstateManagement");

            Logger.Initialise(logger);

            Action<string> loggerAction = message =>
                                          {
                                              Logger.LogInformation(message);
                                          };
            Configuration.LogConfiguration(loggerAction);

            ConfigurationReader.Initialise(Configuration);

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
}