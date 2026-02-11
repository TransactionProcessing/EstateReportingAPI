using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Shared.IntegrationTesting.TestContainers;
using SimpleResults;
using TransactionProcessor.Database.Contexts;
using TransactionProcessor.Database.Entities;

namespace EstateReportingAPI.IntegrationTests;

using System.Net.Http.Headers;
using System.Text;
using NLog;
using Shared.IntegrationTesting;
using Shared.Logger;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

public abstract class ControllerTestsBase : IAsyncLifetime
{
    protected List<Merchant> merchantsList;
    protected List<Operator> operatorsList;
    protected List<(Guid contractId, String contractName, Guid operatorId, String operatorName)> contractList;
    protected Dictionary<Guid, List<(Guid productId, String productName, Decimal? productValue, Int32 contractProductReportingId)>> contractProducts;
    protected DatabaseHelper helper;
    protected ITestOutputHelper TestOutputHelper;
    protected DockerHelper DockerHelper;
    public virtual async Task InitializeAsync()
    {
        this.TestId = Guid.NewGuid();
        String scenarioName = this.TestId.ToString();
        NlogLogger logger = new NlogLogger();
        logger.Initialise(LogManager.GetLogger(scenarioName), scenarioName);
        LogManager.AddHiddenAssembly(typeof(NlogLogger).Assembly);

        this.DockerHelper = new TestDockerHelper();
        this.DockerHelper.Logger = logger;
        
        await this.DockerHelper.StartContainersForScenarioRun(scenarioName, DockerServices.SqlServer);
        
        String dbConnString = GetLocalConnectionString($"TransactionProcessorReadModel-{this.TestId}");

        this.factory = new CustomWebApplicationFactory<Startup>(dbConnString);
        this.Client = this.factory.CreateClient();
        
        this.context = new EstateManagementContext(dbConnString);

        this.helper = new DatabaseHelper(context, this.TestId);
        await this.helper.CreateStoredProcedures(CancellationToken.None);
        await this.SetupStandingData();
    }

    public virtual async Task DisposeAsync()
    {
        await this.DockerHelper.StopContainersForScenarioRun(DockerServices.None);
    }

    protected EstateManagementContext context;

    protected abstract Task ClearStandingData();
    protected abstract Task SetupStandingData();

    protected HttpClient Client;
    protected CustomWebApplicationFactory<Startup> factory;

    protected Guid TestId;

    internal async Task<Result<T?>> CreateAndSendHttpRequestMessage<T>(String url, CancellationToken cancellationToken)
    {
        HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
        requestMessage.Headers.Add("estateId", this.TestId.ToString());
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Test");
        
        HttpResponseMessage result = await this.Client.SendAsync(requestMessage, cancellationToken);
        result.IsSuccessStatusCode.ShouldBeTrue(result.StatusCode.ToString());
        String content = await result.Content.ReadAsStringAsync(cancellationToken);
        content.ShouldNotBeNull();

        return Result.Success(JsonConvert.DeserializeObject<T>(content));
    }

    internal async Task<Result<T?>> CreateAndSendHttpRequestMessage<T>(String url, String payload, CancellationToken cancellationToken)
    {
        HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
        requestMessage.Headers.Add("estateId", this.TestId.ToString());
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Test");
        if (String.IsNullOrEmpty(payload) == false){
            requestMessage.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        }

        HttpResponseMessage result = await this.Client.SendAsync(requestMessage, cancellationToken);
        result.IsSuccessStatusCode.ShouldBeTrue(result.StatusCode.ToString());
        String content = await result.Content.ReadAsStringAsync(cancellationToken);
        content.ShouldNotBeNull();

        return Result.Success(JsonConvert.DeserializeObject<T>(content));
    }
    
    //public static IContainer DatabaseServerContainer;
    //public static INetwork DatabaseServerNetwork;
    public static (String usename, String password) SqlCredentials = ("sa", "thisisalongpassword123!");

    public String GetLocalConnectionString(String databaseName) {
        var dockerHelper = this.DockerHelper as TestDockerHelper;
        Int32? databaseHostPort = dockerHelper.GetSqlPort();

        return $"server=localhost,{databaseHostPort};database={databaseName};user id={SqlCredentials.usename};password={SqlCredentials.password};Encrypt=false";
    }

    //internal async Task StartSqlContainer(){
    //    DockerHelper dockerHelper = new TestDockerHelper();

    //    NlogLogger logger = new NlogLogger();
    //    logger.Initialise(LogManager.GetLogger("Specflow"), "Specflow");
    //    LogManager.AddHiddenAssembly(typeof(NlogLogger).Assembly);
    //    dockerHelper.Logger = logger;
    //    dockerHelper.SqlCredentials = SqlCredentials;
    //    dockerHelper.SqlServerContainerName = "sharedsqlserver";
    //    dockerHelper.RequiredDockerServices = DockerServices.SqlServer;

    //    DatabaseServerNetwork = await dockerHelper.SetupTestNetwork("sharednetwork", true);
    //    await Retry.For(async () => {
    //                  DatabaseServerContainer = await dockerHelper.StartContainersForScenarioRun().SetupSqlServerContainer(DatabaseServerNetwork);
    //              });
    //}

    public void Dispose()
    {
        EstateManagementContext context = new EstateManagementContext(GetLocalConnectionString($"TransactionProcessorReadModel-{this.TestId}"));

        Console.WriteLine($"About to delete database TransactionProcessorReadModel-{this.TestId}");
        Boolean result = context.Database.EnsureDeleted();
        Console.WriteLine($"Delete result is {result}");
        result.ShouldBeTrue();
    }

    internal async Task<T> ExecuteAsyncFunction<T>(Func<Task<T>> asyncFunction)
    {
        try
        {
            // Execute the provided asynchronous function
            return await asyncFunction();
        }
        catch (Exception ex)
        {
            // Handle exceptions here
            Console.WriteLine($"An error occurred: {ex.Message}");
            return default(T);
        }
    }
}



public class TestDockerHelper : DockerHelper{
    public override async Task CreateSubscriptions(){
        // Nothing here
    }

    public Int32? GetSqlPort() {
        var sqlContainer = this.Containers.SingleOrDefault(c => c.Item1 == DockerServices.SqlServer);
        if (sqlContainer == default) {
            return null;
        }

        return sqlContainer.Item2.GetMappedPublicPort(1433);
    }
}