using System;
using System.Diagnostics;
using System.Runtime.Loader;
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
    private static readonly string ScenarioName = $"IntegrationTests-{Process.GetCurrentProcess().Id}-{Guid.NewGuid():N}";
    private static bool CleanupRegistered;

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

            RegisterCleanupHandlers();

            NlogLogger logger = new();
            logger.Initialise(LogManager.GetLogger(ScenarioName), ScenarioName);
            LogManager.AddHiddenAssembly(typeof(NlogLogger).Assembly);

            TestDockerHelper dockerHelper = new();
            dockerHelper.Logger = logger;

            await dockerHelper.StartContainersForScenarioRun(ScenarioName, DockerServices.SqlServer);

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

    private static void RegisterCleanupHandlers()
    {
        if (CleanupRegistered)
        {
            return;
        }

        CleanupRegistered = true;
        AppDomain.CurrentDomain.ProcessExit += (_, _) => CleanupSafely().GetAwaiter().GetResult();
        AssemblyLoadContext.Default.Unloading += _ => CleanupSafely().GetAwaiter().GetResult();
    }

    private static async Task CleanupSafely()
    {
        if (Instance == null)
        {
            return;
        }

        await InitialisationLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (Instance == null)
            {
                return;
            }

            try
            {
                await Instance.DockerHelper.StopContainersForScenarioRun(DockerServices.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogManager.GetLogger(ScenarioName).Error(ex, "Failed to stop integration test containers during shutdown.");
            }
        }
        finally
        {
            Instance = null;
            InitialisationLock.Release();
        }
    }
}
