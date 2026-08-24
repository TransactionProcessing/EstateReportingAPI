using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

namespace EstateReportingAPI.IntegrationTests
{
    public class FileImportLogsEndpointTests : ControllerTestsBase
    {
        public FileImportLogsEndpointTests(ITestOutputHelper output)
        {
            this.TestOutputHelper = output;
        }

        private String BaseRoute = "api/fileimportlogs";

        private void AssertFileLineMatchesSource(FileLine actual, int lineNumber, string contents, string status)
        {
            actual.LineNumber.ShouldBe(lineNumber);
            actual.LineContents.ShouldBe(contents);
            actual.LineStatus.ShouldBe(status);
        }

        private void AssertFileDetailsMatchesSource(FileDetails actual,
                                                    Guid fileId,
                                                    string fileName,
                                                    Guid fileProfileId,
                                                    Guid userId,
                                                    string uploadedBy,
                                                    Guid merchantId,
                                                    string merchantName,
                                                    DateTime uploadedAt)
        {
            actual.FileId.ShouldBe(fileId);
            actual.FileName.ShouldBe(fileName);
            actual.FileProfile.ShouldBe(fileProfileId.ToString());
            actual.DateTimeUploaded.ShouldBe(uploadedAt, TimeSpan.FromSeconds(1));
            actual.UserId.ShouldBe(userId);
            actual.UploadedBy.ShouldBe(uploadedBy);
            actual.MerchantId.ShouldBe(merchantId);
            actual.MerchantName.ShouldBe(merchantName);
        }

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

            var operatorRecord = await this.context.Operators.FirstAsync();
             
            // create file import log, file and a line
            Guid fileProfileId = Guid.Parse("A5D66966-0E95-4F62-A530-7669284EB616");
            await this.helper.AddFileProfile(operatorRecord.OperatorId, Guid.NewGuid(), "Test Profile 1", Guid.NewGuid(), fileProfileId);
            var filId = await this.helper.AddFileImportLog(this.TestId, DateTime.Now);
            var fileId = await this.helper.AddFile(filId, merchant, userId, "test/location/file-filter-list.csv", fileProfileId);
            await this.helper.AddFileLine(fileId, 1, "filterlistline", "OK");

            DateTime start = DateTime.Today.AddDays(-1);
            DateTime end = DateTime.Today.AddDays(1);

            Result<List<FileImportLog>> result = await this.CreateAndSendHttpRequestMessage<List<FileImportLog>>($"{this.BaseRoute}?merchantId={merchant}&startDate={start:yyyy-MM-dd}&endDate={end:yyyy-MM-dd}", CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            var list = result.Data;
            list.ShouldNotBeNull();
            list.Count.ShouldBe(1);

            var sourceLog = this.context.FileImportLogs.Single(x => x.FileImportLogId == filId);
            var sourceFile = this.context.Files.Single(x => x.FileImportLogId == filId);
            var sourceLine = this.context.FileLines.Single(x => x.FileId == fileId);

            var match = list.Single();
            match.FileImportLogId.ShouldBe(sourceLog.FileImportLogId);
            match.ImportLogDateTime.ShouldBe(sourceLog.ImportLogDateTime, TimeSpan.FromSeconds(1));
            match.FileDetailsList.Count.ShouldBe(1);

            var fileDetail = match.FileDetailsList.Single();
            AssertFileDetailsMatchesSource(
                fileDetail,
                sourceFile.FileId,
                sourceFile.FileLocation,
                sourceFile.FileProfileId,
                sourceFile.UserId,
                "apiuser@example.com",
                merchant,
                "List Filter Merchant",
                sourceFile.FileReceivedDateTime);

            fileDetail.FileLines.Count.ShouldBe(1);
            AssertFileLineMatchesSource(fileDetail.FileLines.Single(), sourceLine.LineNumber, sourceLine.FileLineData, sourceLine.Status);
        }

        [Fact]
        public async Task FileImportEndpoint_GetFileImportLog_WithMerchantFilter_ReturnsData()
        {
            // create a user
            var userId = await this.helper.AddEstateUser("Test Estate", "Api User", "apiuser@example.com");

            // create a merchant and use it for the file
            var merchant = await this.helper.AddMerchant("Test Estate", "Filter Merchant", 10, DateTime.MinValue, DateTime.MinValue, default, default);

            // create file import log, file and a line
            var operatorRecord = await this.context.Operators.FirstAsync();

            // create file import log, file and a line
            Guid fileProfileId = Guid.Parse("A5D66966-0E95-4F62-A530-7669284EB616");
            await this.helper.AddFileProfile(operatorRecord.OperatorId, Guid.NewGuid(), "Test Profile 1", Guid.NewGuid(), fileProfileId);
            
            var filId = await this.helper.AddFileImportLog(this.TestId, DateTime.Now);
            var fileId = await this.helper.AddFile(filId, merchant, userId, "test/location/file-filter.csv", fileProfileId);
            await this.helper.AddFileLine(fileId, 1, "filterline", "OK");

            Result<DataTransferObjects.FileImportLog> result = await this.CreateAndSendHttpRequestMessage<DataTransferObjects.FileImportLog>($"{this.BaseRoute}/{filId}?merchantId={merchant}", CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            var item = result.Data;
            item.ShouldNotBeNull();
            var sourceLog = this.context.FileImportLogs.Single(x => x.FileImportLogId == filId);
            var sourceFile = this.context.Files.Single(x => x.FileImportLogId == filId);
            var sourceLine = this.context.FileLines.Single(x => x.FileId == fileId);

            item.FileImportLogId.ShouldBe(sourceLog.FileImportLogId);
            item.ImportLogDateTime.ShouldBe(sourceLog.ImportLogDateTime, TimeSpan.FromSeconds(1));
            item.FileDetailsList.Count.ShouldBe(1);

            var fileDetail = item.FileDetailsList.Single();
            AssertFileDetailsMatchesSource(
                fileDetail,
                sourceFile.FileId,
                Path.GetFileName(sourceFile.FileLocation),
                sourceFile.FileProfileId,
                sourceFile.UserId,
                "apiuser@example.com",
                merchant,
                "Filter Merchant",
                sourceFile.FileReceivedDateTime);

            fileDetail.FileLines.Count.ShouldBe(1);
            AssertFileLineMatchesSource(fileDetail.FileLines.Single(), sourceLine.LineNumber, sourceLine.FileLineData, sourceLine.Status);
        }

        [Fact]
        public async Task FileImportEndpoint_GetFileImportLog_ReturnsInsertedData()
        {
            // create a user
            var userId = await this.helper.AddEstateUser("Test Estate", "Api User", "apiuser@example.com");

            // pick a merchant
            var merchant = await this.context.Merchants.FirstAsync();

            var operatorRecord = await this.context.Operators.FirstAsync();
            
            // create file import log, file and a line
            Guid fileProfileId = Guid.Parse("A5D66966-0E95-4F62-A530-7669284EB616");
            await this.helper.AddFileProfile(operatorRecord.OperatorId, Guid.NewGuid(), "Test Profile 1", Guid.NewGuid(), fileProfileId);
            var filId = await this.helper.AddFileImportLog(this.TestId, DateTime.Now);
            var fileId = await this.helper.AddFile(filId, merchant.MerchantId, userId, "test/location/file1.csv", fileProfileId);
            await this.helper.AddFileLine(fileId, 1, "line1data", "OK");

            Result<DataTransferObjects.FileImportLog> result = await this.CreateAndSendHttpRequestMessage<DataTransferObjects.FileImportLog>($"{this.BaseRoute}/{filId}", CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            var item = result.Data;
            item.ShouldNotBeNull();
            var sourceLog = this.context.FileImportLogs.Single(x => x.FileImportLogId == filId);
            var sourceFile = this.context.Files.Single(x => x.FileImportLogId == filId);
            var sourceLine = this.context.FileLines.Single(x => x.FileId == fileId);

            item.FileImportLogId.ShouldBe(sourceLog.FileImportLogId);
            item.ImportLogDateTime.ShouldBe(sourceLog.ImportLogDateTime, TimeSpan.FromSeconds(1));
            item.FileDetailsList.Count.ShouldBe(1);

            var fileDetail = item.FileDetailsList.Single();
            AssertFileDetailsMatchesSource(
                fileDetail,
                sourceFile.FileId,
                Path.GetFileName(sourceFile.FileLocation),
                sourceFile.FileProfileId,
                sourceFile.UserId,
                "apiuser@example.com",
                merchant.MerchantId,
                merchant.Name,
                sourceFile.FileReceivedDateTime);

            fileDetail.FileLines.Count.ShouldBe(1);
            AssertFileLineMatchesSource(fileDetail.FileLines.Single(), sourceLine.LineNumber, sourceLine.FileLineData, sourceLine.Status);
        }

        [Fact]
        public async Task FileImportEndpoint_GetFileImportLog_WithMerchantFilter_ReturnsNotFound()
        {
            // create a user
            var userId = await this.helper.AddEstateUser("Test Estate", "Api User", "apiuser@example.com");

            // pick a merchant and create another merchant to use as a mismatched filter
            var merchant1 = await this.context.Merchants.FirstAsync();
            var merchant2 = await this.helper.AddMerchant("Test Estate", "Other Merchant", 50, DateTime.MinValue, DateTime.MinValue, default, default);

            var operatorRecord = await this.context.Operators.FirstAsync();

            // create file import log, file and a line
            Guid fileProfileId = Guid.Parse("A5D66966-0E95-4F62-A530-7669284EB616");
            await this.helper.AddFileProfile(operatorRecord.OperatorId, Guid.NewGuid(), "Test Profile 1", Guid.NewGuid(), fileProfileId);

            var filId = await this.helper.AddFileImportLog(this.TestId, DateTime.Now);
            var fileId = await this.helper.AddFile(filId, merchant1.MerchantId, userId, "test/location/file1.csv", fileProfileId);
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

            var operatorRecord = await this.context.Operators.FirstAsync();

            // create file import log, file and a line
            Guid fileProfileId = Guid.Parse("A5D66966-0E95-4F62-A530-7669284EB616");
            await this.helper.AddFileProfile(operatorRecord.OperatorId, Guid.NewGuid(), "Test Profile 1", Guid.NewGuid(), fileProfileId);

            var filId = await this.helper.AddFileImportLog(this.TestId, DateTime.Now);
            var fileId = await this.helper.AddFile(filId, merchant.MerchantId, userId, "test/location/file1.csv", fileProfileId);
            await this.helper.AddFileLine(fileId, 1, "line1data", "OK");

            DateTime start = DateTime.Today.AddDays(-1);
            DateTime end = DateTime.Today.AddDays(1);

            Result<List<FileImportLog>> result = await this.CreateAndSendHttpRequestMessage<List<FileImportLog>>($"{this.BaseRoute}?startDate={start:yyyy-MM-dd}&endDate={end:yyyy-MM-dd}", CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            var list = result.Data;
            list.ShouldNotBeNull();
            list.Count.ShouldBe(1);

            var sourceLog = this.context.FileImportLogs.Single(x => x.FileImportLogId == filId);
            var sourceFile = this.context.Files.Single(x => x.FileImportLogId == filId);
            var sourceLine = this.context.FileLines.Single(x => x.FileId == fileId);

            var match = list.Single();
            match.FileImportLogId.ShouldBe(sourceLog.FileImportLogId);
            match.ImportLogDateTime.ShouldBe(sourceLog.ImportLogDateTime, TimeSpan.FromSeconds(1));
            match.FileDetailsList.Count.ShouldBe(1);

            var fileDetail = match.FileDetailsList.Single();
            AssertFileDetailsMatchesSource(
                fileDetail,
                sourceFile.FileId,
                sourceFile.FileLocation,
                sourceFile.FileProfileId,
                sourceFile.UserId,
                "apiuser@example.com",
                merchant.MerchantId,
                merchant.Name,
                sourceFile.FileReceivedDateTime);
            fileDetail.FileLines.Count.ShouldBe(1);
            AssertFileLineMatchesSource(fileDetail.FileLines.Single(), sourceLine.LineNumber, sourceLine.FileLineData, sourceLine.Status);
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

            // Operators
            await this.helper.AddOperator("Test Estate", "Safaricom");
            await this.helper.AddOperator("Test Estate", "Voucher");
            await this.helper.AddOperator("Test Estate", "PataPawa PostPay");
            await this.helper.AddOperator("Test Estate", "PataPawa PrePay");

            // File Profile (once the table is available at the RM)
        }
    }
}
