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
        operators.Single(o => o.Name == "Safaricom").ShouldSatisfyAllConditions(x => {
            x.OperatorId.ShouldNotBe(Guid.Empty);
            x.EstateReportingId.ShouldBeGreaterThan(0);
            x.Name.ShouldBe("Safaricom");
            x.RequireCustomMerchantNumber.ShouldBeFalse();
            x.RequireCustomTerminalNumber.ShouldBeFalse();
        });
        operators.Single(o => o.Name == "Voucher").ShouldSatisfyAllConditions(x => {
            x.OperatorId.ShouldNotBe(Guid.Empty);
            x.EstateReportingId.ShouldBeGreaterThan(0);
            x.Name.ShouldBe("Voucher");
            x.RequireCustomMerchantNumber.ShouldBeFalse();
            x.RequireCustomTerminalNumber.ShouldBeFalse();
        });
    }

    [Fact]
    public async Task OperatorEndpoint_GetOperator_OperatorReturned() {
        await this.helper.AddEstate("Test Estate", "Ref1");
        var operatorId = await this.helper.AddOperator("Test Estate", "Safaricom");
        Result<Operator> result = await this.CreateAndSendHttpRequestMessage<Operator>($"{this.BaseRoute}/{operatorId}", CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        Operator operatorData = result.Data;
        operatorData.ShouldNotBeNull();
        operatorData.OperatorId.ShouldBe(operatorId);
        operatorData.EstateReportingId.ShouldBeGreaterThan(0);
        operatorData.Name.ShouldBe("Safaricom");
        operatorData.RequireCustomMerchantNumber.ShouldBeFalse();
        operatorData.RequireCustomTerminalNumber.ShouldBeFalse();
    }

    protected override async Task ClearStandingData() {

    }

    protected override async Task SetupStandingData() {

    }
}
