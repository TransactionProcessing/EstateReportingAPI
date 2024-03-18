namespace EstateReportingAPI.IntegrationTests;

using System.Net.Http.Headers;
using Azure;
using Client;
using Common;
using Ductus.FluentDocker.Services;
using Ductus.FluentDocker.Services.Extensions;
using EstateManagement.Database.Contexts;
using Newtonsoft.Json;
using NLog;
using Shared.IntegrationTesting;
using Shared.Logger;
using Shouldly;
using Xunit;

public abstract class ControllerTestsBase : IAsyncLifetime
{
    protected List<String> merchantsList;
    protected List<(String contract, String operatorname)> contractList;
    protected Dictionary<String, List<String>> contractProducts;
    protected DatabaseHelper helper;
    public virtual async Task InitializeAsync()
    {
        this.TestId = Guid.NewGuid();

        await this.StartSqlContainer();

        String dbConnString = GetLocalConnectionString($"EstateReportingReadModel{this.TestId}");

        this.factory = new CustomWebApplicationFactory<Startup>(dbConnString);
        this.Client = this.factory.CreateClient();

        this.context = new EstateManagementSqlServerContext(GetLocalConnectionString($"EstateReportingReadModel{this.TestId.ToString()}"));

        this.helper = new DatabaseHelper(context);

        await this.SetupStandingData();
    }

    public virtual async Task DisposeAsync()
    {
    }
    
    protected EstateManagementGenericContext context;

    protected abstract Task ClearStandingData();
    protected abstract Task SetupStandingData();

    protected HttpClient Client;
    protected CustomWebApplicationFactory<Startup> factory;

    protected Guid TestId;
    
    internal async Task<T?> CreateAndSendHttpRequestMessage<T>(String url, CancellationToken cancellationToken)
    {
        HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
        requestMessage.Headers.Add("estateId", this.TestId.ToString());
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Test");

        HttpResponseMessage result = await this.Client.SendAsync(requestMessage, cancellationToken);
        result.IsSuccessStatusCode.ShouldBeTrue(result.StatusCode.ToString());
        String content = await result.Content.ReadAsStringAsync(cancellationToken);
        content.ShouldNotBeNull();
        return JsonConvert.DeserializeObject<T>(content);
    }
    public static IContainerService DatabaseServerContainer;
    public static INetworkService DatabaseServerNetwork;
    public static (String usename, String password) SqlCredentials = ("sa", "thisisalongpassword123!");

    public static String GetLocalConnectionString(String databaseName)
    {
        Int32 databaseHostPort = DatabaseServerContainer.ToHostExposedEndpoint("1433/tcp").Port;

        return $"server=localhost,{databaseHostPort};database={databaseName};user id={SqlCredentials.usename};password={SqlCredentials.password};Encrypt=false";
    }

    internal async Task StartSqlContainer(){
        DockerHelper dockerHelper = new TestDockerHelper();

        NlogLogger logger = new NlogLogger();
        logger.Initialise(LogManager.GetLogger("Specflow"), "Specflow");
        LogManager.AddHiddenAssembly(typeof(NlogLogger).Assembly);
        dockerHelper.Logger = logger;
        dockerHelper.SqlCredentials = SqlCredentials;
        dockerHelper.SqlServerContainerName = "sharedsqlserver";
        dockerHelper.RequiredDockerServices = DockerServices.SqlServer;

        DatabaseServerNetwork = dockerHelper.SetupTestNetwork("sharednetwork", true);
        await Retry.For(async () => {
                      DatabaseServerContainer = await dockerHelper.SetupSqlServerContainer(DatabaseServerNetwork);
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

public class TestDockerHelper : DockerHelper{
    public override async Task CreateSubscriptions(){
        // Nothing here
    }
}