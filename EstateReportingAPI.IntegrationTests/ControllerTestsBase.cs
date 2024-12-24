using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EstateManagement.Database.Entities;
using SimpleResults;

namespace EstateReportingAPI.IntegrationTests;

using System.Net.Http.Headers;
using System.Text;
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
using Xunit.Abstractions;

public abstract class ControllerTestsBase : IAsyncLifetime
{
    protected List<Merchant> merchantsList;
    protected List<(Guid contractId, String contractName, Guid operatorId, String operatorName)> contractList;
    protected Dictionary<Guid, List<(Guid productId, String productName, Decimal? productValue)>> contractProducts;
    protected DatabaseHelper helper;
    protected ITestOutputHelper TestOutputHelper;
    public virtual async Task InitializeAsync()
    {
        this.TestId = Guid.NewGuid();

        await this.StartSqlContainer();

        String dbConnString = GetLocalConnectionString($"EstateReportingReadModel{this.TestId}");

        this.factory = new CustomWebApplicationFactory<Startup>(dbConnString);
        this.Client = this.factory.CreateClient();
        this.ApiClient = new EstateReportingApiClient((s) => "http://localhost", this.Client);

        this.context = new EstateManagementSqlServerContext(GetLocalConnectionString($"EstateReportingReadModel{this.TestId.ToString()}"));

        this.helper = new DatabaseHelper(context);
        await this.helper.CreateStoredProcedures(CancellationToken.None);
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

    protected EstateReportingApiClient ApiClient;

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

        return null;
    }

    internal async Task<Result<T?>> CreateAndSendHttpRequestMessage<T>(String url, String payload, CancellationToken cancellationToken)
    {
        HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
        requestMessage.Headers.Add("estateId", this.TestId.ToString());
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Test");
        if (String.IsNullOrEmpty(payload) == false){
            requestMessage.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        }

        HttpResponseMessage result = await this.Client.SendAsync(requestMessage, cancellationToken);
        result.IsSuccessStatusCode.ShouldBeTrue(result.StatusCode.ToString());
        String content = await result.Content.ReadAsStringAsync(cancellationToken);
        content.ShouldNotBeNull();

        return null;
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
}