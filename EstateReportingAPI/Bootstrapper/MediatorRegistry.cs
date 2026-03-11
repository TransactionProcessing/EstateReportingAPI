using EstateReportingAPI.BusinessLogic.RequestHandlers;
using EstateReportingAPI.Models;
using Lamar;
using MediatR;
using SimpleResults;
using System.Diagnostics.CodeAnalysis;
using EstateReportingAPI.BusinessLogic.Queries;
using Shared.General;

namespace EstateReportingAPI.Bootstrapper;

[ExcludeFromCodeCoverage]
public class MediatorRegistry : ServiceRegistry {
    public MediatorRegistry() {
        this.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(EstateRequestHandler).Assembly));
        this.AddSingleton<Func<String, String>>(container => (serviceName) => ConfigurationReader.GetBaseServerUri(serviceName).OriginalString);
    }
}


