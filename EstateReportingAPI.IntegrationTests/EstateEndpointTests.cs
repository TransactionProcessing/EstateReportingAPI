using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using EstateReportingAPI.DataTransferObjects;
using Shouldly;
using SimpleResults;
using Xunit;

namespace EstateReportingAPI.IntegrationTests;

public class EstateEndpointTests : ControllerTestsBase {
    private String BaseRoute = "api/estates";

    [Fact]
    public async Task EstateEndpoint_GetEstates_EstateReturned() {
        await this.helper.AddEstate("Test Estate", "Ref1");

        Result<Estate> result = await this.CreateAndSendHttpRequestMessage<Estate>($"{this.BaseRoute}", CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();

        Estate estate = result.Data;
        estate.ShouldNotBeNull();
        estate.EstateName.ShouldBe("Test Estate");
        estate.Reference.ShouldBe("Ref1");
        estate.EstateId.ShouldBe(this.context.Estates.Single().EstateId);
        estate.Operators.ShouldNotBeNull();
        estate.Merchants.ShouldNotBeNull();
        estate.Contracts.ShouldNotBeNull();
        estate.Users.ShouldNotBeNull();
        estate.Operators.ShouldBeEmpty();
        estate.Merchants.ShouldBeEmpty();
        estate.Contracts.ShouldBeEmpty();
        estate.Users.ShouldBeEmpty();
    }

    [Fact]
    public async Task EstateEndpoint_GetEstateOperator_EstateOperatorsReturned() {
        await this.helper.AddEstate("Test Estate", "Ref1");
        await this.helper.AddOperator("Test Estate", "Safaricom");
        await this.helper.AddOperator("Test Estate", "Voucher");

        await this.helper.AddEstateOperators("Test Estate", ["Safaricom", "Voucher"]);

        Result<List<EstateOperator>> result = await this.CreateAndSendHttpRequestMessage<List<EstateOperator>>($"{this.BaseRoute}/operators", CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();

        List<EstateOperator> estateOperators = result.Data;
        estateOperators.Count.ShouldBe(2);
        estateOperators.Single(e => e.Name == "Safaricom").OperatorId.ShouldBe(this.context.Operators.Single(o => o.Name == "Safaricom").OperatorId);
        estateOperators.Single(e => e.Name == "Voucher").OperatorId.ShouldBe(this.context.Operators.Single(o => o.Name == "Voucher").OperatorId);
    }

    [Fact]
    public async Task EstateEndpoint_MatchingEstateClaimAndHeader_ReturnsEstate()
    {
        await this.helper.AddEstate("Test Estate", "Ref1");

        using HttpResponseMessage response = await SendEstateRequest(this.TestId.ToString());

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task EstateEndpoint_ValidEstateClaimWithoutHeader_ReturnsEstate()
    {
        await this.helper.AddEstate("Test Estate", "Ref1");

        using HttpResponseMessage response = await SendEstateRequest();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task EstateEndpoint_DifferentEstateHeader_ReturnsForbidden()
    {
        using HttpResponseMessage response = await SendEstateRequest(Guid.NewGuid().ToString());

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task EstateEndpoint_MalformedEstateHeader_ReturnsForbidden()
    {
        using HttpResponseMessage response = await SendEstateRequest("not-a-guid");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task EstateEndpoint_MissingEstateClaim_ReturnsForbidden()
    {
        using HttpResponseMessage response = await SendEstateRequest(omitEstateClaim: true);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task EstateEndpoint_MalformedEstateClaim_ReturnsForbidden()
    {
        using HttpResponseMessage response = await SendEstateRequest(estateClaim: "not-a-guid");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task EstateEndpoint_EmptyEstateClaim_ReturnsForbidden()
    {
        using HttpResponseMessage response = await SendEstateRequest(estateClaim: Guid.Empty.ToString());

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task EstateEndpoint_EstateIdClaim_ReturnsEstate()
    {
        await this.helper.AddEstate("Test Estate", "Ref1");

        using HttpResponseMessage response = await SendEstateRequest(
            estateHeader: this.TestId.ToString(),
            estateClaimType: "estateId");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task EstateEndpoint_MissingAuthenticationAndEstateContext_ReturnsForbidden()
    {
        using HttpRequestMessage request = new(HttpMethod.Get, this.BaseRoute);

        using HttpResponseMessage response = await this.Client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task EstateEndpoint_DisabledAuthorisationWithEstateHeader_ReturnsEstate()
    {
        await this.helper.AddEstate("Test Estate", "Ref1");

        using HttpRequestMessage request = new(HttpMethod.Get, this.BaseRoute);
        request.Headers.Add("estateId", this.TestId.ToString());

        using HttpResponseMessage response = await this.Client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private async Task<HttpResponseMessage> SendEstateRequest(
        string? estateHeader = null,
        bool omitEstateClaim = false,
        string? estateClaim = null,
        string? estateClaimType = null)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, this.BaseRoute);
        request.Headers.Authorization = new AuthenticationHeaderValue("Test");

        if (estateHeader != null)
            request.Headers.Add("estateId", estateHeader);

        if (omitEstateClaim)
            request.Headers.Add(TestAuthHandler.OmitEstateClaim, "true");

        if (estateClaim != null)
            request.Headers.Add(TestAuthHandler.EstateClaim, estateClaim);

        if (estateClaimType != null)
            request.Headers.Add(TestAuthHandler.EstateClaimType, estateClaimType);

        return await this.Client.SendAsync(request);
    }

    protected override async Task ClearStandingData() {

    }

    protected override async Task SetupStandingData() {

    }
}
