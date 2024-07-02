﻿using Shared.Middleware;

namespace EstateReportingAPI.Bootstrapper{
    using System.Diagnostics.CodeAnalysis;
    using System.Net.Security;
    using System.Reflection;
    using Common;
    using Lamar;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using Microsoft.IdentityModel.Tokens;
    using Microsoft.OpenApi.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using Shared.General;
    using Swashbuckle.AspNetCore.Filters;

    [ExcludeFromCodeCoverage]
    public class MiddlewareRegistry : ServiceRegistry{
        public MiddlewareRegistry(){


            this.AddHealthChecks()
                .AddSqlServer(connectionString:ConfigurationReader.GetConnectionString("HealthCheck"),
                              healthQuery:"SELECT 1;",
                              name:"Read Model Server",
                              failureStatus:HealthStatus.Degraded,
                              tags:new[]{ "db", "sql", "sqlserver" });
            this.AddSwaggerGen(c => {
                                   c.SwaggerDoc("v1",
                                                new OpenApiInfo{
                                                                   Title = "Estate Reporting API",
                                                                   Version = "1.0",
                                                                   Description = "A REST Api to manage all aspects of reporting for an estate (merchants, operators and contracts).",
                                                                   Contact = new OpenApiContact{
                                                                                                   Name = "Stuart Ferguson",
                                                                                                   Email = "golfhandicapping@btinternet.com"
                                                                                               }
                                                               });
                                   // add a custom operation filter which sets default values
                                   c.OperationFilter<SwaggerDefaultValues>();
                                   c.ExampleFilters();

                                   //Locate the XML files being generated by ASP.NET...
                                   var directory = new DirectoryInfo(AppContext.BaseDirectory);
                                   var xmlFiles = directory.GetFiles("*.xml");

                                   //... and tell Swagger to use those XML comments.
                                   foreach (FileInfo fileInfo in xmlFiles){
                                       c.IncludeXmlComments(fileInfo.FullName);
                                   }
                               });

            this.AddSwaggerExamplesFromAssemblyOf<SwaggerJsonConverter>();

            String? inTestMode = Environment.GetEnvironmentVariable("InTestMode");
            if (String.Compare(inTestMode, Boolean.TrueString, StringComparison.InvariantCultureIgnoreCase) != 0){
                this.AddAuthentication(options => {
                                           options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                                           options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                                           options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                                       }).AddJwtBearer(options => {
                                                           options.BackchannelHttpHandler = new HttpClientHandler{
                                                                                                                     ServerCertificateCustomValidationCallback = (message,
                                                                                                                                                                  certificate,
                                                                                                                                                                  chain,
                                                                                                                                                                  sslPolicyErrors) => true
                                                                                                                 };
                                                           options.Authority = ConfigurationReader.GetValue("SecurityConfiguration", "Authority");
                                                           options.Audience = ConfigurationReader.GetValue("SecurityConfiguration", "ApiName");

                                                           options.TokenValidationParameters = new TokenValidationParameters{
                                                                                                                                ValidateAudience = false,
                                                                                                                                ValidAudience =
                                                                                                                                    ConfigurationReader.GetValue("SecurityConfiguration", "ApiName"),
                                                                                                                                ValidIssuer =
                                                                                                                                    ConfigurationReader.GetValue("SecurityConfiguration", "Authority"),
                                                                                                                            };
                                                           options.IncludeErrorDetails = true;
                                                       });
            }

            this.AddControllers().AddNewtonsoftJson(options => {
                                                        options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                                                        options.SerializerSettings.TypeNameHandling = TypeNameHandling.None;
                                                        options.SerializerSettings.Formatting = Formatting.Indented;
                                                        options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Unspecified;
                                                        options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                                                    });

            Assembly assembly = this.GetType().GetTypeInfo().Assembly;
            this.AddMvcCore().AddApplicationPart(assembly).AddControllersAsServices();

            bool logRequests = ConfigurationReaderExtensions.GetValueOrDefault<Boolean>("MiddlewareLogging", "LogRequests", true);
            bool logResponses = ConfigurationReaderExtensions.GetValueOrDefault<Boolean>("MiddlewareLogging", "LogResponses", true);
            LogLevel middlewareLogLevel = ConfigurationReaderExtensions.GetValueOrDefault<LogLevel>("MiddlewareLogging", "MiddlewareLogLevel", LogLevel.Warning);

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

    public static class ConfigurationReaderExtensions
    {
        public static T GetValueOrDefault<T>(String sectionName, String keyName, T defaultValue)
        {
            try
            {
                var value = ConfigurationReader.GetValue(sectionName, keyName);

                if (String.IsNullOrEmpty(value))
                {
                    return defaultValue;
                }

                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch (KeyNotFoundException kex)
            {
                return defaultValue;
            }
        }
    }
}