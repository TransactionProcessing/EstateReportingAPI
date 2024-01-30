namespace EstateReportingAPI.Tests.General
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net.Http;
    using System.Threading;
    using Client;
    using EstateReportingAPI;
    using Lamar;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Moq;
    using Shouldly;
    using Xunit;

    [Collection("TestCollection")]
    public class BootstrapperTests
    {
        [Fact]
        public void VerifyBootstrapperIsValid(){
            
            Mock<IWebHostEnvironment> hostingEnvironment = new Mock<IWebHostEnvironment>();
            hostingEnvironment.Setup(he => he.EnvironmentName).Returns("Development");
            hostingEnvironment.Setup(he => he.ContentRootPath).Returns("/home");
            hostingEnvironment.Setup(he => he.ApplicationName).Returns("Test Application");

            ServiceRegistry services = new ServiceRegistry();
            Startup s = new Startup(hostingEnvironment.Object);
            Startup.Configuration = this.SetupMemoryConfiguration();
            
            this.AddTestRegistrations(services, hostingEnvironment.Object);
            s.ConfigureContainer(services);
            Startup.Container.AssertConfigurationIsValid(AssertMode.Full);
        }

        private IConfigurationRoot SetupMemoryConfiguration()
        {
            Dictionary<String, String> configuration = new Dictionary<String, String>();

            IConfigurationBuilder builder = new ConfigurationBuilder();

            configuration.Add("ConnectionStrings:HealthCheck", "HeathCheckConnString");
            configuration.Add("SecurityConfiguration:Authority", "https://127.0.0.1");
            configuration.Add("EventStoreSettings:ConnectionString", "https://127.0.0.1:2113");
            configuration.Add("EventStoreSettings:ConnectionName", "UnitTestConnection");
            configuration.Add("EventStoreSettings:UserName", "admin");
            configuration.Add("EventStoreSettings:Password", "changeit");
            configuration.Add("AppSettings:UseConnectionStringConfig", "false");
            configuration.Add("AppSettings:SecurityService", "http://127.0.0.1");
            configuration.Add("AppSettings:MessagingServiceApi", "http://127.0.0.1");
            configuration.Add("AppSettings:TransactionProcessorApi", "http://127.0.0.1");
            configuration.Add("AppSettings:DatabaseEngine", "SqlServer");
            
            builder.AddInMemoryCollection(configuration);

            return builder.Build();
        }

        private void AddTestRegistrations(ServiceRegistry services,
                                                        IWebHostEnvironment hostingEnvironment)
        {
            services.AddLogging();
            DiagnosticListener diagnosticSource = new DiagnosticListener(hostingEnvironment.ApplicationName);
            services.AddSingleton<DiagnosticSource>(diagnosticSource);
            services.AddSingleton<DiagnosticListener>(diagnosticSource);
            services.AddSingleton<IWebHostEnvironment>(hostingEnvironment);
            services.AddSingleton<IHostEnvironment>(hostingEnvironment);
            services.AddSingleton<IConfiguration>(Startup.Configuration);
        }
    }

    public class QueryStringBuilderTests
    {
        [Theory]
        [InlineData("StringTest", "value",true)]
        [InlineData("IntegerTest", 10, true)]
        [InlineData("DecimalTest1", 1.00, true)]
        [InlineData("DecimalTest2", 0.01, true)]
        [InlineData("GuidTest", "69F35754-EB7D-441B-A62F-F50633AFBE91", true)]
        [InlineData("NullTest", null, false)]
        [InlineData("EmptyStringTest", "", false)]
        [InlineData("ZeroIntegerTest", 0, false)]
        [InlineData("ZeroDecimalTest1", 0, false)]
        [InlineData("ZeroDecimalTest2", 0.00, false)]
        [InlineData("EmptyGuidTest", "00000000-0000-0000-0000-000000000000", false)]
        public void QueryStringBuilderTests_BuildQueryString_ValuesAddedAsExpected(String keyname, Object value, Boolean isAdded){
            if (keyname == "GuidTest" || keyname == "EmptyGuidTest")
                value = Guid.Parse(value.ToString());

            QueryStringBuilder builder = new QueryStringBuilder();
            builder.AddParameter(keyname, value);

            String queryString = builder.BuildQueryString();

            queryString.Contains(keyname).ShouldBe(isAdded);
            
        }
    }
}
