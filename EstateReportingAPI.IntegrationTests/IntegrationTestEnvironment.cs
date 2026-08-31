using System;
using System.Threading;
using System.Threading.Tasks;
using Shared.IntegrationTesting;
using Shared.IntegrationTesting.TestContainers;
using Shared.Logger;
using NLog;

namespace EstateReportingAPI.IntegrationTests;

internal sealed class IntegrationTestEnvironment
{
    private static readonly SemaphoreSlim InitialisationLock = new(1, 1);
    private static IntegrationTestEnvironment? Instance;

    private IntegrationTestEnvironment(TestDockerHelper dockerHelper, int sqlPort)
    {
        this.DockerHelper = dockerHelper;
        this.SqlPort = sqlPort;
    }

    public TestDockerHelper DockerHelper { get; }

    public int SqlPort { get; }

    public static async Task<IntegrationTestEnvironment> GetAsync()
    {
        if (Instance != null)
        {
            return Instance;
        }

        await InitialisationLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (Instance != null)
            {
                return Instance;
            }

            string scenarioName = "IntegrationTests";
            NlogLogger logger = new();
            logger.Initialise(LogManager.GetLogger(scenarioName), scenarioName);
            LogManager.AddHiddenAssembly(typeof(NlogLogger).Assembly);

            TestDockerHelper dockerHelper = new();
            dockerHelper.Logger = logger;

            await dockerHelper.StartContainersForScenarioRun(scenarioName, DockerServices.SqlServer);

            int sqlPort = dockerHelper.GetSqlPort()
                ?? throw new InvalidOperationException("SQL Server container did not expose port 1433.");

            Instance = new IntegrationTestEnvironment(dockerHelper, sqlPort);
            return Instance;
        }
        finally
        {
            InitialisationLock.Release();
        }
    }
}
