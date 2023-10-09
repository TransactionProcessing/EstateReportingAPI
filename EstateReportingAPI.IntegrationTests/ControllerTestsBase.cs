namespace EstateReportingAPI.IntegrationTests;

using System.Net.Http.Headers;
using Azure;
using Client;
using Common;
using Ductus.FluentDocker.Services;
using Ductus.FluentDocker.Services.Extensions;
using EstateManagement.Database.Contexts;
using NLog;
using Shared.IntegrationTesting;
using Shared.Logger;
using Shouldly;

public abstract class ControllerTestsBase : IDisposable{

    protected readonly HttpClient Client;
    protected readonly CustomWebApplicationFactory<Startup> factory;

    protected readonly Guid TestId;

    public ControllerTestsBase(){
       this.StartSqlContainer();

        this.TestId = Guid.NewGuid();
        String dbConnString = GetLocalConnectionString($"EstateReportingReadModel{this.TestId}");

        this.factory = new CustomWebApplicationFactory<Startup>(dbConnString);
        this.Client = this.factory.CreateClient();
    }

    internal async Task<HttpResponseMessage> CreateAndSendHttpRequestMessage(String url)
    {
        HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
        requestMessage.Headers.Add("estateId", this.TestId.ToString());
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Test");

        HttpResponseMessage result = await this.Client.SendAsync(requestMessage, CancellationToken.None);
        result.IsSuccessStatusCode.ShouldBeTrue(result.StatusCode.ToString());
        return result;
    }
    public static IContainerService DatabaseServerContainer;
    public static INetworkService DatabaseServerNetwork;
    public static (String usename, String password) SqlCredentials = ("sa", "thisisalongpassword123!");

    public static String GetLocalConnectionString(String databaseName)
    {
        Int32 databaseHostPort = DatabaseServerContainer.ToHostExposedEndpoint("1433/tcp").Port;

        return $"server=localhost,{databaseHostPort};database={databaseName};user id={SqlCredentials.usename};password={SqlCredentials.password};Encrypt=false";
    }

    internal void StartSqlContainer(){
        DockerHelper dockerHelper = new DockerHelper();

        NlogLogger logger = new NlogLogger();
        logger.Initialise(LogManager.GetLogger("Specflow"), "Specflow");
        LogManager.AddHiddenAssembly(typeof(NlogLogger).Assembly);
        dockerHelper.Logger = logger;
        dockerHelper.SqlCredentials = SqlCredentials;
        dockerHelper.SqlServerContainerName = "sharedsqlserver";

        DatabaseServerNetwork = dockerHelper.SetupTestNetwork("sharednetwork", true);
        Retry.For(async () => {
                      DatabaseServerContainer = dockerHelper.SetupSqlServerContainer(DatabaseServerNetwork);
                  });
    }

    public void Dispose()
    {
        EstateManagementGenericContext context = new EstateManagementSqlServerContext(ControllerTestsBase.GetLocalConnectionString($"EstateReportingReadModel{this.TestId.ToString()}"));

        Console.WriteLine($"About to delete database EstateReportingReadModel{this.TestId.ToString()}");
        Boolean result = context.Database.EnsureDeleted();
        Console.WriteLine($"Delete result is {result}");
        result.ShouldBeTrue();
    }
}