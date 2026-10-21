using EstateReportingAPI.BusinessLogic.RequestHandlers;
using EstateReportingAPI.Models;
using Lamar;
using MediatR;
using SimpleResults;
using System.Diagnostics.CodeAnalysis;
using EstateReportingAPI.BusinessLogic.Queries;
using Microsoft.Extensions.Configuration;

namespace EstateReportingAPI.Bootstrapper;

[ExcludeFromCodeCoverage]
public class MediatorRegistry : ServiceRegistry {
    public MediatorRegistry(IConfiguration configuration) {
        this.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(EstateRequestHandler).Assembly));
        this.AddSingleton<Func<String, String>>(_ => serviceName => new Uri(configuration[$"AppSettings:{serviceName}"]!).OriginalString);
    }
}


