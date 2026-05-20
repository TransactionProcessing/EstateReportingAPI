using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Http;
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
        public async Task FileImportEndpoint_GetFileImportLogs_WithMerchantFilter_ReturnsData()
        {
            // create a user
            var userId = await this.helper.AddEstateUser("Test Estate", "Api User", "apiuser@example.com");

            // create a merchant and use it for the file
            var merchant = await this.helper.AddMerchant("Test Estate", "List Filter Merchant", 10, DateTime.MinValue, DateTime.MinValue, default, default);

            // create file import log, file and a line associated to the merchant
            var filId = await this.helper.AddFileImportLog(this.TestId, DateTime.Now);
            var fileId = await this.helper.AddFile(filId, merchant, userId, "test/location/file-filter-list.csv");
            await this.helper.AddFileLine(fileId, 1, "filterlistline", "OK");

            DateTime start = DateTime.Today.AddDays(-1);
            DateTime end = DateTime.Today.AddDays(1);

            Result<List<FileImportLog>> result = await this.CreateAndSendHttpRequestMessage<List<FileImportLog>>($"{this.BaseRoute}?merchantId={merchant}&startDate={start:yyyy-MM-dd}&endDate={end:yyyy-MM-dd}", CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            var list = result.Data;
            list.ShouldNotBeNull();
            list.Count.ShouldBeGreaterThan(0);

            var match = list.SelectMany(x => x.FileDetailsList).SelectMany(fd => fd.FileLines).SingleOrDefault(fl => fl.LineContents == "filterlistline");
            match.ShouldNotBeNull();
            match.LineStatus.ShouldBe("OK");
        }

        [Fact]
        public async Task FileImportEndpoint_GetFileImportLog_WithMerchantFilter_ReturnsData()
        {
            // create a user
            var userId = await this.helper.AddEstateUser("Test Estate", "Api User", "apiuser@example.com");

            // create a merchant and use it for the file
            var merchant = await this.helper.AddMerchant("Test Estate", "Filter Merchant", 10, DateTime.MinValue, DateTime.MinValue, default, default);

            // create file import log, file and a line associated to the merchant
            var filId = await this.helper.AddFileImportLog(this.TestId, DateTime.Now);
            var fileId = await this.helper.AddFile(filId, merchant, userId, "test/location/file-filter.csv");
            await this.helper.AddFileLine(fileId, 1, "filterline", "OK");

            Result<DataTransferObjects.FileImportLog> result = await this.CreateAndSendHttpRequestMessage<DataTransferObjects.FileImportLog>($"{this.BaseRoute}/{filId}?merchantId={merchant}", CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            var item = result.Data;
            item.ShouldNotBeNull();
            item.FileImportLogId.ShouldBe(filId);

            var match = item.FileDetailsList.SelectMany(fd => fd.FileLines).SingleOrDefault(fl => fl.LineContents == "filterline");
            match.ShouldNotBeNull();
            match.LineStatus.ShouldBe("OK");
        }

        [Fact]
        public async Task FileImportEndpoint_GetFileImportLog_ReturnsInsertedData()
        {
            // create a user
            var userId = await this.helper.AddEstateUser("Test Estate", "Api User", "apiuser@example.com");

            // pick a merchant
            var merchant = await this.context.Merchants.FirstAsync();

            // create file import log, file and a line
            var filId = await this.helper.AddFileImportLog(this.TestId, DateTime.Now);
            var fileId = await this.helper.AddFile(filId, merchant.MerchantId, userId, "test/location/file1.csv");
            await this.helper.AddFileLine(fileId, 1, "line1data", "OK");

            Result<DataTransferObjects.FileImportLog> result = await this.CreateAndSendHttpRequestMessage<DataTransferObjects.FileImportLog>($"{this.BaseRoute}/{filId}", CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            var item = result.Data;
            item.ShouldNotBeNull();
            item.FileImportLogId.ShouldBe(filId);

            var match = item.FileDetailsList.SelectMany(fd => fd.FileLines).SingleOrDefault(fl => fl.LineContents == "line1data");
            match.ShouldNotBeNull();
            match.LineStatus.ShouldBe("OK");
        }

        [Fact]
        public async Task FileImportEndpoint_GetFileImportLog_WithMerchantFilter_ReturnsNotFound()
        {
            // create a user
            var userId = await this.helper.AddEstateUser("Test Estate", "Api User", "apiuser@example.com");

            // pick a merchant and create another merchant to use as a mismatched filter
            var merchant1 = await this.context.Merchants.FirstAsync();
            var merchant2 = await this.helper.AddMerchant("Test Estate", "Other Merchant", 50, DateTime.MinValue, DateTime.MinValue, default, default);

            // create file import log, file and a line associated to merchant1
            var filId = await this.helper.AddFileImportLog(this.TestId, DateTime.Now);
            var fileId = await this.helper.AddFile(filId, merchant1.MerchantId, userId, "test/location/file1.csv");
            await this.helper.AddFileLine(fileId, 1, "line1data", "OK");

            var url = $"{this.BaseRoute}/{filId}?merchantId={merchant2}";
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            requestMessage.Headers.Add("estateId", this.TestId.ToString());
            requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Test");

            var response = await this.Client.SendAsync(requestMessage, CancellationToken.None);
            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
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
