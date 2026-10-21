using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EstateReportingAPI.Bootstrapper;

/// <summary>
/// Initializes legacy shared-library static APIs that are still used by shared middleware.
/// Remove this adapter when Shared exposes instance-based configuration and logging APIs.
/// </summary>
internal static class SharedLibraryCompatibility
{
    public static void InitializeConfiguration(IConfigurationRoot configuration)
    {
        Shared.General.ConfigurationReader.Initialise(configuration);
    }

    public static void InitializeLogging(ILogger logger)
    {
        Shared.Logger.Logger.Initialise(logger);
    }
}
