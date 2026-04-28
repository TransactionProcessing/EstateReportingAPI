using EstateReportingAPI.Bootstrapper;
using HealthChecks.UI.Client;
using Lamar;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Shared.Extensions;
using Shared.General;
using Shared.Logger;
using System.Diagnostics.CodeAnalysis;
using EstateReportingAPI.Endpoints;

namespace EstateReportingAPI
{
    using Shared.Middleware;

    [ExcludeFromCodeCoverage]
    public class Startup
    {
        public static IConfigurationRoot Configuration { get; set; }
        public static IWebHostEnvironment WebHostEnvironment { get; set; }
        public static IServiceProvider ServiceProvider { get; set; }
        public static Container Container;

        public Startup(IWebHostEnvironment webHostEnvironment)
        {
            WebHostEnvironment = webHostEnvironment;
        }

        public void ConfigureContainer(ServiceRegistry services)
        {
            ConfigurationReader.Initialise(Configuration);

            services.IncludeRegistry<MiddlewareRegistry>();
            services.IncludeRegistry<RepositoryRegistry>();
            services.IncludeRegistry<MediatorRegistry>();
            
            Container = new Container(services);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }

            Microsoft.Extensions.Logging.ILogger logger = loggerFactory.CreateLogger("EstateManagement");

            Logger.Initialise(logger);
            Configuration.LogConfiguration(Logger.LogWarning);

            ConfigurationReader.Initialise(Configuration);
            app.UseMiddleware<TenantMiddleware>();
            app.AddRequestResponseLogging();
            app.AddExceptionHandler();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
                             {
                                 endpoints.MapCalendarEndpoints();
                                 endpoints.MapEstateEndpoints();
                                 endpoints.MapOperatorEndpoints();
                                 endpoints.MapMerchantEndpoints();
                                 endpoints.MapContractEndpoints();
                                 endpoints.MapTransactionEndpoints();
                                 endpoints.MapSettlementEndpoints();
                                 
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