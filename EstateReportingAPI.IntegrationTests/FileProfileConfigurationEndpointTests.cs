using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EstateReportingAPI.DataTransferObjects;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Shouldly;
using Xunit;

namespace EstateReportingAPI.IntegrationTests
{
    public class FileProfileConfigurationEndpointTests : ControllerTestsBase
    {
        private const string BaseRoute = "api/fileprofiles";

        [Fact]
        public async Task FileProfileConfiguration_Get_NoData_ReturnsEmptyList()
        {
            // ensure estate exists
            await this.helper.AddEstate("Test Estate", "Ref1");

            var result = await this.CreateAndSendHttpRequestMessage<List<FileProfileConfiguration>>($"{BaseRoute}", CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            var list = result.Data;
            list.ShouldNotBeNull();
            list.Count.ShouldBe(0);
        }

        [Fact]
        public async Task FileProfileConfiguration_Get_ReturnsInsertedData()
        {
            // setup estate and operator
            await this.helper.AddEstate("Test Estate", "Ref1");
            var operatorId = await this.helper.AddOperator("Test Estate", "Test Operator");

            // insert supporting rows and a profile using helper methods
            Guid profileId = Guid.NewGuid();
            string handlerName = $"TestHandler-{this.TestId}";
            string requestName = $"TestRequest-{this.TestId}";
            string profileName = $"TestProfile-{this.TestId}";

            Guid handlerId = await this.helper.AddFileFormatHandler(handlerName);
            Guid requestId = await this.helper.AddRequestType(requestName);

            await this.helper.AddFileProfileConfiguration(profileId, profileName, "C:\\listen", handlerId, requestId, operatorId, "\\n");

            var result = await this.CreateAndSendHttpRequestMessage<List<FileProfileConfiguration>>($"{BaseRoute}", CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            var list = result.Data;
            list.ShouldNotBeNull();
            list.Count.ShouldBeGreaterThan(0);

            var match = list.SingleOrDefault(f => f.FileProfileId == profileId);
            match.ShouldNotBeNull();
            match.Name.ShouldBe(profileName);
        }

        protected override Task ClearStandingData()
        {
            return Task.CompletedTask;
        }

        protected override Task SetupStandingData()
        {
            // tests will create their own standing data as needed
            return Task.CompletedTask;
        }
    }
}
