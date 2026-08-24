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

    protected override async Task ClearStandingData() {

    }

    protected override async Task SetupStandingData() {

    }
}
