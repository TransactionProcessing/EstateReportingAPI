using HealthMonitoring.Client;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Shared.Middleware;
using TransactionProcessing.ApiDefaults;

namespace EstateReportingAPI.Bootstrapper{
    using EstateReportingAPI.Common;
    using Lamar;
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using OpenIddict.Validation.AspNetCore;
    using Shared.Serialisation;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;
    using System.Text.Json;

    [ExcludeFromCodeCoverage]
    public class MiddlewareRegistry : ServiceRegistry{
        public MiddlewareRegistry(IConfiguration configuration)
        {
            this.ConfigureHealthChecks(configuration);
            this.ConfigureAuthentication(configuration);
            this.ConfigureControllers();
            this.ConfigureMiddlewareLogging(configuration);
            this.ConfigureJsonOptions();
            this.AddHealthMonitoringRegistration(configuration);
            this.AddScoped<EstateContext>();
            this.AddScoped<IEstateContext>(sp => sp.GetRequiredService<EstateContext>());
            this.Configure<ApiDefaultsOptions>(options =>
            {
                options.Title = "Estate Reporting";
                options.Description = "API providing a variety of reporting facilities consumed by various reporting applications";
            });
            this.AddApiVersioning(options =>
            {
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
                options.DefaultApiVersion = ApiVersions.V1;
                options.AssumeDefaultVersionWhenUnspecified = false;
                options.ReportApiVersions = true;
            }).AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            }).AddOpenApi(options =>
            {
                options.Document.AddSchemaTransformer((schema, context, _) =>
                {
                    XmlDocumentationSchemaTransformer.Apply(schema, context);
                    return Task.CompletedTask;
                });
                options.Document.AddOperationTransformer((operation, context, _) =>
                {
                    OpenApiExamplesTransformer.Apply(operation, context);
                    return Task.CompletedTask;
                });
                options.Document.AddDocumentTransformer((document, context, _) =>
                {
                    ApiDefaultsOptions metadata = context.ApplicationServices
                        .GetRequiredService<IOptions<ApiDefaultsOptions>>()
                        .Value;
                    document.Info.Title = metadata.Title;
                    document.Info.Description = metadata.Description;
                    return Task.CompletedTask;
                });
            });

        }

        private void ConfigureJsonOptions()
        {
            this.ConfigureHttpJsonOptions(options => {
                JsonSerializerConfiguration.ConfigureMinimalApi(options.SerializerOptions);
            });
        }

        private void ConfigureHealthChecks(IConfiguration configuration){
            var connectionStringSection = configuration.GetSection("ConnectionStrings");
            if (connectionStringSection.Exists() == false)
            {
                this.AddHealthChecks();
            }
            else
            {
                this.AddHealthChecks().AddSqlServer(
                    connectionString: configuration.GetConnectionString("HealthCheck"),
                    healthQuery: "SELECT 1;",
                    name: "Read Model Server",
                    failureStatus: HealthStatus.Degraded,
                    tags: new[] { "db", "sql", "sqlserver" });
            }
        }
        
        private void ConfigureAuthentication(IConfiguration configuration){
            bool inTestMode = configuration.GetValue("InTestMode", false);
            if (!inTestMode){
                this.AddAuthentication(options =>
                {
                    options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
                });
                this.AddOpenIddict()
                    .AddValidation(options =>
                    {
                        // Same as your Authority
                        options.SetIssuer(new Uri(configuration["SecurityConfiguration:Authority"]!));

                        // Enables discovery and HTTP backchannel support
                        options.UseSystemNetHttp()
                            .ConfigureHttpClientHandler(handler =>
                            {
                                // DEV ONLY: bypass all certificate errors
                                handler.ServerCertificateCustomValidationCallback =
                                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                            });

                        // Register the ASP.NET Core integration
                        options.UseAspNetCore();

                        // Optionally set expected audience(s):
                        options.AddAudiences(configuration["SecurityConfiguration:ApiName"]!);

                    });

                this.AddAuthorization();
            }
        }

        private void ConfigureControllers() {
            this.AddControllers();

            Assembly assembly = this.GetType().GetTypeInfo().Assembly;
            this.AddMvcCore().AddApplicationPart(assembly).AddControllersAsServices();
        }

        private void ConfigureMiddlewareLogging(IConfiguration configuration){
            bool logRequests = configuration.GetValue("MiddlewareLogging:LogRequests", true);
            bool logResponses = configuration.GetValue("MiddlewareLogging:LogResponses", true);
            LogLevel middlewareLogLevel = configuration.GetValue("MiddlewareLogging:MiddlewareLogLevel", LogLevel.Warning);

            RequestResponseMiddlewareLoggingConfig config =
                new RequestResponseMiddlewareLoggingConfig(middlewareLogLevel, logRequests, logResponses);

            this.AddSingleton(config);
        }

        private HttpClientHandler ApiEndpointHttpHandler(IServiceProvider serviceProvider){
            return new HttpClientHandler{
                                            ServerCertificateCustomValidationCallback = (message,
                                                                                         cert,
                                                                                         chain,
                                                                                         errors) => {
                                                                                            return true;
                                                                                        }
                                        };
        }
    }

    [ExcludeFromCodeCoverage]
    public class SerialiserRegistry : ServiceRegistry
    {
        public SerialiserRegistry()
        {
            this.AddSingleton<IStringSerialiser, SystemTextJsonSerializer>();
            this.AddSingleton<Func<Object, String>>(container => obj => container.GetRequiredService<IStringSerialiser>().Serialize(obj));
            this.AddSingleton<Func<String, Type, Object>>(container => (json, type) => container.GetRequiredService<IStringSerialiser>().DeserializeObject<Object>(json, type));

            var serialiserSettings = SystemTextJsonSerializer.GetDefaultJsonSerializerOptions();

            this.AddSingleton(serialiserSettings);
        }
    }

    public static class JsonSerializerConfiguration
    {
        public static void ConfigureMinimalApi(JsonSerializerOptions serializerOptions)
        {
            var defaultOptions = SystemTextJsonSerializer.GetDefaultJsonSerializerOptions();
            serializerOptions.PropertyNamingPolicy = defaultOptions.PropertyNamingPolicy;
            serializerOptions.DictionaryKeyPolicy = defaultOptions.DictionaryKeyPolicy;
            serializerOptions.ReferenceHandler = defaultOptions.ReferenceHandler;
            serializerOptions.WriteIndented = defaultOptions.WriteIndented;
            serializerOptions.Converters.Add(new DateTimeSpaceConverter());
        }
    }
}
