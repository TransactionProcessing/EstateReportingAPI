namespace EstateReportingAPI.Bootstrapper{
    using System.Diagnostics.CodeAnalysis;
    using System.Net.Security;
    using System.Reflection;
    using Common;
    using EstateManagement.Database.Contexts;
    using Lamar;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using Microsoft.IdentityModel.Tokens;
    using Microsoft.OpenApi.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using Shared.EntityFramework;
    using Shared.EntityFramework.ConnectionStringConfiguration;
    using Shared.General;
    using Shared.Repositories;
    using Swashbuckle.AspNetCore.Filters;

    public class RepositoryRegistry : ServiceRegistry{
        public RepositoryRegistry(){

            Boolean useConnectionStringConfig = bool.Parse(ConfigurationReader.GetValue("AppSettings", "UseConnectionStringConfig"));

            if (useConnectionStringConfig)
            {
                String connectionStringConfigurationConnString = ConfigurationReader.GetConnectionString("ConnectionStringConfiguration");
                this.AddSingleton<IConnectionStringConfigurationRepository, ConnectionStringConfigurationRepository>();
                this.AddTransient(c => { return new ConnectionStringConfigurationContext(connectionStringConfigurationConnString); });

                // TODO: Read this from a the database and set
            }
            else
            {
                this.AddSingleton<IConnectionStringConfigurationRepository, ConfigurationReaderConnectionStringRepository>();
            }

            this.AddSingleton<IDbContextFactory<EstateManagementGenericContext>, DbContextFactory<EstateManagementGenericContext>>();

            this.AddSingleton<Func<String, EstateManagementGenericContext>>(cont => connectionString =>
                                                                                    {
                                                                                        String databaseEngine =
                                                                                            ConfigurationReader.GetValue("AppSettings", "DatabaseEngine");

                                                                                        return databaseEngine switch
                                                                                        {
                                                                                            "MySql" => new EstateManagementMySqlContext(connectionString),
                                                                                            "SqlServer" => new EstateManagementSqlServerContext(connectionString),
                                                                                            _ => throw new
                                                                                                NotSupportedException($"Unsupported Database Engine {databaseEngine}")
                                                                                        };
                                                                                    });
        }
    }


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

            this.AddControllers().AddNewtonsoftJson(options => {
                                                        options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                                                        options.SerializerSettings.TypeNameHandling = TypeNameHandling.None;
                                                        options.SerializerSettings.Formatting = Formatting.Indented;
                                                        options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                                                        options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                                                    });

            Assembly assembly = this.GetType().GetTypeInfo().Assembly;
            this.AddMvcCore().AddApplicationPart(assembly).AddControllersAsServices();
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
}