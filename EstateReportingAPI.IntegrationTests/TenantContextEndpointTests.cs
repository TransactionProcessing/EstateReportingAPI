using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Xunit;
using Shouldly;

namespace EstateReportingAPI.IntegrationTests;

public sealed class TenantContextEndpointTests : ControllerTestsBase
{
    [Theory]
    [InlineData("api/calendars/comparisondates")]
    [InlineData("api/contracts")]
    [InlineData("api/estates")]
    [InlineData("api/fileimportlogs")]
    [InlineData("api/fileprofiles")]
    [InlineData("api/merchants")]
    [InlineData("api/operators")]
    [InlineData("api/settlements/todayssettlements")]
    [InlineData("api/transactions/todayssales")]
    public async Task ProtectedEndpoint_DifferentEstateHeader_ReturnsForbidden(string route)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, route);
        request.Headers.Authorization = new AuthenticationHeaderValue("Test");
        request.Headers.Add("estateId", Guid.NewGuid().ToString());

        using HttpResponseMessage response = await this.Client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ProtectedEndpoint_RepeatedEstateHeaders_ReturnsForbidden()
    {
        using HttpRequestMessage request = new(HttpMethod.Get, "api/estates");
        request.Headers.Authorization = new AuthenticationHeaderValue("Test");
        request.Headers.Add("estateId", this.TestId.ToString());
        request.Headers.Add("estateId", Guid.NewGuid().ToString());

        using HttpResponseMessage response = await this.Client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("health")]
    [InlineData("healthui")]
    public async Task HealthEndpoint_DoesNotRequireEstateContext(string route)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, route);

        using HttpResponseMessage response = await this.Client.SendAsync(request);

        ((int)response.StatusCode).ShouldNotBe((int)HttpStatusCode.Unauthorized);
        ((int)response.StatusCode).ShouldNotBe((int)HttpStatusCode.Forbidden);
    }

    protected override Task ClearStandingData()
    {
        return Task.CompletedTask;
    }

    protected override Task SetupStandingData()
    {
        return Task.CompletedTask;
    }
}
