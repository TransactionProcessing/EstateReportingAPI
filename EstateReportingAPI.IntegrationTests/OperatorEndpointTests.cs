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

public class OperatorEndpointTests : ControllerTestsBase {
    private String BaseRoute = "api/operators";

    [Fact]
    public async Task OperatorEndpoint_GetOperators_OperatorsReturned() {
        await this.helper.AddEstate("Test Estate", "Ref1");
        await this.helper.AddOperator("Test Estate", "Safaricom");
        await this.helper.AddOperator("Test Estate", "Voucher");

        Result<List<Operator>> result = await this.CreateAndSendHttpRequestMessage<List<Operator>>($"{this.BaseRoute}", CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        List<Operator> operators = result.Data;
        operators.Count.ShouldBe(2);
        operators.SingleOrDefault(o => o.Name == "Safaricom").ShouldNotBeNull();
        operators.SingleOrDefault(o => o.Name == "Voucher").ShouldNotBeNull();
    }

    [Fact]
    public async Task OperatorEndpoint_GetOperator_OperatorReturned() {
        await this.helper.AddEstate("Test Estate", "Ref1");
        var operatorId = await this.helper.AddOperator("Test Estate", "Safaricom");
        Result<Operator> result = await this.CreateAndSendHttpRequestMessage<Operator>($"{this.BaseRoute}/{operatorId}", CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        Operator operatorData = result.Data;
        operatorData.ShouldNotBeNull();
        operatorData.Name.ShouldBe("Safaricom");
    }

    protected override async Task ClearStandingData() {

    }

    protected override async Task SetupStandingData() {

    }
}