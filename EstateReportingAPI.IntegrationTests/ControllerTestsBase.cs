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

    public virtual async Task InitializeAsync()
    {
        await this.SetupStandingData();
    }

    public virtual async Task DisposeAsync()
    {
    }

    protected DatabaseHelper helper;

    protected EstateManagementGenericContext context;

    protected abstract Task ClearStandingData();
    protected abstract Task SetupStandingData();

    protected readonly HttpClient Client;
    protected readonly CustomWebApplicationFactory<Startup> factory;

    protected readonly Guid TestId;

    public ControllerTestsBase(){
       this.StartSqlContainer();

        this.TestId = Guid.NewGuid();
        String dbConnString = GetLocalConnectionString($"EstateReportingReadModel{this.TestId}");

        this.factory = new CustomWebApplicationFactory<Startup>(dbConnString);
        this.Client = this.factory.CreateClient();

        this.context = new EstateManagementSqlServerContext(GetLocalConnectionString($"EstateReportingReadModel{this.TestId.ToString()}"));

        this.helper = new DatabaseHelper(context);
    }

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