using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EstateReportingAPI.DataTransferObjects;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using SimpleResults;
using Xunit;
using Xunit.Abstractions;

namespace EstateReportingAPI.IntegrationTests
{
    public class FileImportLogsEndpointTests : ControllerTestsBase
    {
        public FileImportLogsEndpointTests(ITestOutputHelper output)
        {
            this.TestOutputHelper = output;
        }

        private String BaseRoute = "api/fileimportlogs";

        [Fact]
        public async Task FileImportEndpoint_GetFileImportLogs_NoData_ReturnsEmptyList()
        {
            DateTime start = DateTime.Today.AddDays(-7);
            DateTime end = DateTime.Today;

            Result<List<FileImportLog>> result = await this.CreateAndSendHttpRequestMessage<List<FileImportLog>>($"{this.BaseRoute}?startDate={start:yyyy-MM-dd}&endDate={end:yyyy-MM-dd}", CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            var list = result.Data;
            list.ShouldNotBeNull();
            list.Count.ShouldBe(0);
        }

        [Fact]
        public async Task FileImportEndpoint_GetFileImportLogs_ReturnsInsertedData()
        {
            // create estate and user already created in SetupStandingData
            var estate = await this.context.Estates.FirstAsync();

            // create a user
            var userId = await this.helper.AddEstateUser("Test Estate", "Api User", "apiuser@example.com");

            // pick a merchant
            var merchant = await this.context.Merchants.FirstAsync();

            // create file import log, file and a line
            var filId = await this.helper.AddFileImportLog(this.TestId, DateTime.Now);
            var fileId = await this.helper.AddFile(filId, merchant.MerchantId, userId, "test/location/file1.csv");
            await this.helper.AddFileLine(fileId, 1, "line1data", "OK");

            DateTime start = DateTime.Today.AddDays(-1);
            DateTime end = DateTime.Today.AddDays(1);

            Result<List<FileImportLog>> result = await this.CreateAndSendHttpRequestMessage<List<FileImportLog>>($"{this.BaseRoute}?startDate={start:yyyy-MM-dd}&endDate={end:yyyy-MM-dd}", CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            var list = result.Data;
            list.ShouldNotBeNull();
            list.Count.ShouldBeGreaterThan(0);

            var match = list.SelectMany(x => x.FileDetailsList).SelectMany(f => f.FileLines).SingleOrDefault(fl => fl.LineContents == "line1data");
            match.ShouldNotBeNull();
            match.LineStatus.ShouldBe("OK");
        }

        [Fact]
        public async Task FileImportEndpoint_GetFileImportLogs_WithMerchantFilter_NoData_ReturnsEmptyList()
        {
            DateTime start = DateTime.Today.AddDays(-7);
            DateTime end = DateTime.Today;

            // use one of the merchants created in SetupStandingData
            var merchant = await this.context.Merchants.FirstAsync();

            Result<List<FileImportLog>> result = await this.CreateAndSendHttpRequestMessage<List<FileImportLog>>($"{this.BaseRoute}?merchantId={merchant.MerchantId}&startDate={start:yyyy-MM-dd}&endDate={end:yyyy-MM-dd}", CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            var list = result.Data;
            list.ShouldNotBeNull();
            list.Count.ShouldBe(0);
        }

        protected override async Task ClearStandingData() {
            
        }

        protected override async Task SetupStandingData() {
            Stopwatch sw = Stopwatch.StartNew();
            this.TestOutputHelper.WriteLine("Setting up standing data");

            // Estates
            await this.helper.AddEstate("Test Estate", "Ref1");
            sw.Stop();
            this.TestOutputHelper.WriteLine($"Setup Estate {sw.ElapsedMilliseconds}ms");
            sw.Restart();
            
            // Estate Security User
            //await this.helper.AddEstateUser("Test Estate User", "testuser@example.com", this.TestId);
            sw.Stop();
            this.TestOutputHelper.WriteLine($"Setup Estate User {sw.ElapsedMilliseconds}ms");
            sw.Restart();

            // Merchants
            await this.helper.AddMerchant("Test Estate", "Test Merchant 1", 100, DateTime.MinValue, DateTime.MinValue, default, default);
            await this.helper.AddMerchant("Test Estate", "Test Merchant 2", 100, DateTime.MinValue, DateTime.MinValue, default, default);
            await this.helper.AddMerchant("Test Estate", "Test Merchant 3", 100, DateTime.MinValue, DateTime.MinValue, default, default);
            await this.helper.AddMerchant("Test Estate", "Test Merchant 4", 100, DateTime.MinValue, DateTime.MinValue, default, default);
            sw.Stop();
            this.TestOutputHelper.WriteLine($"Setup Merchants {sw.ElapsedMilliseconds}ms");
            sw.Restart();
            
            // File Profile (once the table is available at the RM)
        }
    }
}
