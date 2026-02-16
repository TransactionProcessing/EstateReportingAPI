using System;
using System.Collections.Generic;
using System.Linq;
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
        estateOperators.SingleOrDefault(e => e.Name == "Safaricom").ShouldNotBeNull();
        estateOperators.SingleOrDefault(e => e.Name == "Voucher").ShouldNotBeNull();
    }

    protected override async Task ClearStandingData() {

    }

    protected override async Task SetupStandingData() {

    }
}