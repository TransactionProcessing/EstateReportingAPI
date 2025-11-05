using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EstateReportingAPI.Client;
using EstateReportingAPI.DataTransferObjects;
using Moq.Protected;
using Moq;
using Xunit;
using EstateReportingAPI.DataTrasferObjects;
using Newtonsoft.Json;
using Shouldly;
using SimpleResults;
using SortDirection = EstateReportingAPI.DataTransferObjects.SortDirection;

namespace EstateReportingAPI.Tests {
    public class EstateReportingApiClientTests {
        private readonly IEstateReportingApiClient EstateReportingApiClient;
        private readonly IProtectedMock<HttpMessageHandler> HttpMessageHandler;

        public EstateReportingApiClientTests() {
            var baseAddressResolver = new Func<string, string>(s => "http://localhost");
            var mockHandler = new Mock<HttpMessageHandler>();
            EstateReportingApiClient = new EstateReportingApiClient(baseAddressResolver, new HttpClient(mockHandler.Object));
            this.HttpMessageHandler = mockHandler.Protected();
        }

        [Fact]
        public async Task EstateReportingApiClient_GetCalendarDates_DatesReturned() {
            var resultData = TestData.CalendarDateList;

            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(JsonConvert.SerializeObject(resultData)) });

            var result = await this.EstateReportingApiClient.GetCalendarDates("", Guid.NewGuid(), 2024, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            result.Data.Count.ShouldBe(TestData.CalendarDateList.Count);
        }

        [Fact]
        public async Task EstateReportingApiClient_GetCalendarDates_SendAsyncReturnsFailureResponse_ResultFailed()
        {
            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest });

            var result = await this.EstateReportingApiClient.GetCalendarDates("", Guid.NewGuid(), 2024, CancellationToken.None);
            result.IsFailed.ShouldBeTrue();
        }

        [Fact]
        public async Task EstateReportingApiClient_GetCalendarDates_SendAsyncThrowsException_ResultFailed() {
            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ThrowsAsync(new Exception());

            var result = await this.EstateReportingApiClient.GetCalendarDates("", Guid.NewGuid(), 2024, CancellationToken.None);
            result.IsFailed.ShouldBeTrue();
        }

        [Fact]
        public async Task EstateReportingApiClient_GetCalendarYears_YearsReturned() {
            var resultData = TestData.CalendarYearList;

            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(JsonConvert.SerializeObject(resultData)) });

            var result = await this.EstateReportingApiClient.GetCalendarYears("", Guid.NewGuid(), CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            result.Data.Count.ShouldBe(TestData.CalendarYearList.Count);
        }

        [Fact]
        public async Task EstateReportingApiClient_GetCalendarYears_SendAsyncThrowsException_ResultFailed() {
            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ThrowsAsync(new Exception());

            var result = await this.EstateReportingApiClient.GetCalendarYears("", Guid.NewGuid(), CancellationToken.None);
            result.IsFailed.ShouldBeTrue();
        }

        [Fact]
        public async Task EstateReportingApiClient_GetComparisonDates_DatesReturned() {
            var resultData = TestData.ComparisonDateList;

            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(JsonConvert.SerializeObject(resultData)) });

            var result = await this.EstateReportingApiClient.GetComparisonDates("", Guid.NewGuid(), CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            result.Data.Count.ShouldBe(TestData.ComparisonDateList.Count);
        }

        [Fact]
        public async Task EstateReportingApiClient_GetComparisonDates_SendAsyncThrowsException_ResultFailed() {
            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ThrowsAsync(new Exception());

            var result = await this.EstateReportingApiClient.GetComparisonDates("", Guid.NewGuid(), CancellationToken.None);
            result.IsFailed.ShouldBeTrue();
        }

        [Fact]
        public async Task EstateReportingApiClient_GetLastSettlement_LastSettlementReturned() {
            var resultData = TestData.LastSettlement;

            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(JsonConvert.SerializeObject(resultData)) });

            var result = await this.EstateReportingApiClient.GetLastSettlement("", Guid.NewGuid(), CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            result.Data.FeesValue.ShouldBe(TestData.LastSettlement.FeesValue);
            result.Data.SalesCount.ShouldBe(TestData.LastSettlement.SalesCount);
            result.Data.SalesValue.ShouldBe(TestData.LastSettlement.SalesValue);
            result.Data.SettlementDate.ShouldBe(TestData.LastSettlement.SettlementDate);
        }

        [Fact]
        public async Task EstateReportingApiClient_GetLastSettlement_SendAsyncThrowsException_ResultFailed() {
            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ThrowsAsync(new Exception());

            var result = await this.EstateReportingApiClient.GetLastSettlement("", Guid.NewGuid(), CancellationToken.None);
            result.IsFailed.ShouldBeTrue();
        }

        [Fact]
        public async Task EstateReportingApiClient_GetResponseCodes_ResponseCodesReturned() {
            var resultData = TestData.ResponseCodeList;

            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(JsonConvert.SerializeObject(resultData)) });

            var result = await this.EstateReportingApiClient.GetResponseCodes("", Guid.NewGuid(), CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            result.Data.Count.ShouldBe(TestData.ResponseCodeList.Count);
        }

        [Fact]
        public async Task EstateReportingApiClient_GetResponseCodes_SendAsyncThrowsException_ResultFailed() {
            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ThrowsAsync(new Exception());

            var result = await this.EstateReportingApiClient.GetResponseCodes("", Guid.NewGuid(), CancellationToken.None);
            result.IsFailed.ShouldBeTrue();
        }

        [Fact]
        public async Task EstateReportingApiClient_GetMerchantPerformance_PerformanceReturned() {
            var resultData = TestData.TodaysSales;

            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(JsonConvert.SerializeObject(resultData)) });

            var result = await this.EstateReportingApiClient.GetMerchantPerformance("", Guid.NewGuid(), DateTime.Now, new List<int> { 1, 2 }, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            result.Data.ComparisonAverageSalesValue.ShouldBe(TestData.TodaysSales.ComparisonAverageSalesValue);
            result.Data.ComparisonSalesCount.ShouldBe(TestData.TodaysSales.ComparisonSalesCount);
            result.Data.ComparisonSalesValue.ShouldBe(TestData.TodaysSales.ComparisonSalesValue);
            result.Data.TodaysAverageSalesValue.ShouldBe(TestData.TodaysSales.TodaysAverageSalesValue);
            result.Data.TodaysSalesCount.ShouldBe(TestData.TodaysSales.TodaysSalesCount);
            result.Data.TodaysSalesValue.ShouldBe(TestData.TodaysSales.TodaysSalesValue);
        }

        [Fact]
        public async Task EstateReportingApiClient_GetMerchantPerformance_SendAsyncThrowsException_ResultFailed() {
            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ThrowsAsync(new Exception());

            var result = await this.EstateReportingApiClient.GetMerchantPerformance("", Guid.NewGuid(), DateTime.Now, new List<int> { 1, 2 }, CancellationToken.None);
            result.IsFailed.ShouldBeTrue();
        }

        [Fact]
        public async Task EstateReportingApiClient_GetProductPerformance_PerformanceReturned() {
            var resultData = TestData.TodaysSales;

            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(JsonConvert.SerializeObject(resultData)) });

            var result = await this.EstateReportingApiClient.GetProductPerformance("", Guid.NewGuid(), DateTime.Now, new List<int> { 1, 2 }, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            result.Data.ComparisonAverageSalesValue.ShouldBe(TestData.TodaysSales.ComparisonAverageSalesValue);
            result.Data.ComparisonSalesCount.ShouldBe(TestData.TodaysSales.ComparisonSalesCount);
            result.Data.ComparisonSalesValue.ShouldBe(TestData.TodaysSales.ComparisonSalesValue);
            result.Data.TodaysAverageSalesValue.ShouldBe(TestData.TodaysSales.TodaysAverageSalesValue);
            result.Data.TodaysSalesCount.ShouldBe(TestData.TodaysSales.TodaysSalesCount);
            result.Data.TodaysSalesValue.ShouldBe(TestData.TodaysSales.TodaysSalesValue);
        }

        [Fact]
        public async Task EstateReportingApiClient_GetProductPerformance_SendAsyncThrowsException_ResultFailed() {
            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ThrowsAsync(new Exception());

            var result = await this.EstateReportingApiClient.GetProductPerformance("", Guid.NewGuid(), DateTime.Now, new List<int> { 1, 2 }, CancellationToken.None);
            result.IsFailed.ShouldBeTrue();
        }

        [Fact]
        public async Task EstateReportingApiClient_GetMerchantsByLastSaleDate_MerchantsReturned() {
            var resultData = TestData.MerchantList;

            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(JsonConvert.SerializeObject(resultData)) });

            var result = await this.EstateReportingApiClient.GetMerchantsByLastSaleDate("", Guid.NewGuid(), DateTime.Now.AddDays(-1), DateTime.Now, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            result.Data.Count.ShouldBe(TestData.MerchantList.Count);
        }

        [Fact]
        public async Task EstateReportingApiClient_GetMerchantsByLastSaleDate_SendAsyncThrowsException_ResultFailed() {
            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ThrowsAsync(new Exception());

            var result = await this.EstateReportingApiClient.GetMerchantsByLastSaleDate("", Guid.NewGuid(), DateTime.Now.AddDays(-1), DateTime.Now, CancellationToken.None);
            result.IsFailed.ShouldBeTrue();
        }

        [Fact]
        public async Task EstateReportingApiClient_GetOperatorPerformance_PerformanceReturned() {
            var resultData = TestData.TodaysSales;

            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(JsonConvert.SerializeObject(resultData)) });

            var result = await this.EstateReportingApiClient.GetOperatorPerformance("", Guid.NewGuid(), DateTime.Now, new List<int> { 1, 2 }, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            result.Data.ComparisonAverageSalesValue.ShouldBe(TestData.TodaysSales.ComparisonAverageSalesValue);
            result.Data.ComparisonSalesCount.ShouldBe(TestData.TodaysSales.ComparisonSalesCount);
            result.Data.ComparisonSalesValue.ShouldBe(TestData.TodaysSales.ComparisonSalesValue);
            result.Data.TodaysAverageSalesValue.ShouldBe(TestData.TodaysSales.TodaysAverageSalesValue);
            result.Data.TodaysSalesCount.ShouldBe(TestData.TodaysSales.TodaysSalesCount);
            result.Data.TodaysSalesValue.ShouldBe(TestData.TodaysSales.TodaysSalesValue);
        }

        [Fact]
        public async Task EstateReportingApiClient_GetOperatorPerformance_SendAsyncThrowsException_ResultFailed() {
            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ThrowsAsync(new Exception());

            var result = await this.EstateReportingApiClient.GetOperatorPerformance("", Guid.NewGuid(), DateTime.Now, new List<int> { 1, 2 }, CancellationToken.None);
            result.IsFailed.ShouldBeTrue();
        }

        [Fact]
        public async Task EstateReportingApiClient_TransactionSearch_TransactionsReturned() {
            var resultData = TestData.TransactionResultList;

            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(JsonConvert.SerializeObject(resultData)) });

            var result = await this.EstateReportingApiClient.TransactionSearch("", Guid.NewGuid(), new TransactionSearchRequest(), null, null, null, null, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            result.Data.Count.ShouldBe(TestData.TransactionResultList.Count);
        }

        [Fact]
        public async Task EstateReportingApiClient_TransactionSearch_WithPageNumber_TransactionsReturned()
        {
            var resultData = TestData.TransactionResultList;

            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(JsonConvert.SerializeObject(resultData)) });

            var result = await this.EstateReportingApiClient.TransactionSearch("", Guid.NewGuid(), new TransactionSearchRequest(), 1, null, null, null, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            result.Data.Count.ShouldBe(TestData.TransactionResultList.Count);
        }

        [Fact]
        public async Task EstateReportingApiClient_TransactionSearch_WithPageSize_TransactionsReturned()
        {
            var resultData = TestData.TransactionResultList;

            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(JsonConvert.SerializeObject(resultData)) });

            var result = await this.EstateReportingApiClient.TransactionSearch("", Guid.NewGuid(), new TransactionSearchRequest(), null, 1, null, null, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            result.Data.Count.ShouldBe(TestData.TransactionResultList.Count);
        }

        [Fact]
        public async Task EstateReportingApiClient_TransactionSearch_WithSort_TransactionsReturned()
        {
            var resultData = TestData.TransactionResultList;

            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(JsonConvert.SerializeObject(resultData)) });

            var result = await this.EstateReportingApiClient.TransactionSearch("", Guid.NewGuid(), new TransactionSearchRequest(), null, null, SortField.MerchantName, SortDirection.Ascending, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            result.Data.Count.ShouldBe(TestData.TransactionResultList.Count);
        }

        [Fact]
        public async Task EstateReportingApiClient_TransactionSearch_SendAsyncThrowsException_ResultFailed() {
            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ThrowsAsync(new Exception());

            var result = await this.EstateReportingApiClient.TransactionSearch("", Guid.NewGuid(), new TransactionSearchRequest(), null, null, null, null, CancellationToken.None);
            result.IsFailed.ShouldBeTrue();
        }

        [Fact]
        public async Task EstateReportingApiClient_GetUnsettledFees_FeesReturned() {
            var resultData = TestData.UnsettledFeeList;

            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(JsonConvert.SerializeObject(resultData)) });

            var result = await this.EstateReportingApiClient.GetUnsettledFees("", Guid.NewGuid(), DateTime.Now.AddDays(-1), DateTime.Now, new List<int> { 1, 2 }, new List<int> { 1, 2 }, new List<int> { 1, 2 }, GroupByOption.Merchant, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            result.Data.Count.ShouldBe(TestData.UnsettledFeeList.Count);
        }

        [Fact]
        public async Task EstateReportingApiClient_GetUnsettledFees_SendAsyncThrowsException_ResultFailed() {
            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ThrowsAsync(new Exception());

            var result = await this.EstateReportingApiClient.GetUnsettledFees("", Guid.NewGuid(), DateTime.Now.AddDays(-1), DateTime.Now, new List<int> { 1, 2 }, new List<int> { 1, 2 }, new List<int> { 1, 2 }, GroupByOption.Merchant, CancellationToken.None);
            result.IsFailed.ShouldBeTrue();
        }

        [Fact]
        public async Task EstateReportingApiClient_GetMerchantKpi_KpiReturned() {
            var resultData = TestData.MerchantKpi;

            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(JsonConvert.SerializeObject(resultData)) });

            var result = await this.EstateReportingApiClient.GetMerchantKpi("", Guid.NewGuid(), CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            result.Data.MerchantsWithNoSaleToday.ShouldBe(TestData.MerchantKpi.MerchantsWithNoSaleToday);
            result.Data.MerchantsWithNoSaleInLast7Days.ShouldBe(TestData.MerchantKpi.MerchantsWithNoSaleInLast7Days);
            result.Data.MerchantsWithSaleInLastHour.ShouldBe(TestData.MerchantKpi.MerchantsWithSaleInLastHour);
        }

        [Fact]
        public async Task EstateReportingApiClient_GetMerchantKpi_SendAsyncThrowsException_ResultFailed() {
            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ThrowsAsync(new Exception());

            var result = await this.EstateReportingApiClient.GetMerchantKpi("", Guid.NewGuid(), CancellationToken.None);
            result.IsFailed.ShouldBeTrue();
        }

        [Fact]
        public async Task EstateReportingApiClient_GetMerchants_MerchantsReturned() {
            var resultData = TestData.MerchantList;

            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(JsonConvert.SerializeObject(resultData)) });

            var result = await this.EstateReportingApiClient.GetMerchants("", Guid.NewGuid(), CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            result.Data.Count.ShouldBe(TestData.MerchantList.Count);
        }

        [Fact]
        public async Task EstateReportingApiClient_GetMerchants_SendAsyncThrowsException_ResultFailed() {
            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ThrowsAsync(new Exception());

            var result = await this.EstateReportingApiClient.GetMerchants("", Guid.NewGuid(), CancellationToken.None);
            result.IsFailed.ShouldBeTrue();
        }

        [Fact]
        public async Task EstateReportingApiClient_GetOperators_OperatorsReturned() {
            var resultData = TestData.OperatorList;

            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(JsonConvert.SerializeObject(resultData)) });

            var result = await this.EstateReportingApiClient.GetOperators("", Guid.NewGuid(), CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            result.Data.Count.ShouldBe(TestData.OperatorList.Count);
        }

        [Fact]
        public async Task EstateReportingApiClient_GetOperators_SendAsyncThrowsException_ResultFailed() {
            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ThrowsAsync(new Exception());

            var result = await this.EstateReportingApiClient.GetOperators("", Guid.NewGuid(), CancellationToken.None);
            result.IsFailed.ShouldBeTrue();
        }

        [Fact]
        public async Task EstateReportingApiClient_GetTodaysFailedSales_FailedSalesReturned() {
            var resultData = TestData.TodaysSales;

            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(JsonConvert.SerializeObject(resultData)) });

            var result = await this.EstateReportingApiClient.GetTodaysFailedSales("", Guid.NewGuid(), 1, 1, "00", DateTime.Now, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            result.Data.ComparisonAverageSalesValue.ShouldBe(TestData.TodaysSales.ComparisonAverageSalesValue);
            result.Data.ComparisonSalesCount.ShouldBe(TestData.TodaysSales.ComparisonSalesCount);
            result.Data.ComparisonSalesValue.ShouldBe(TestData.TodaysSales.ComparisonSalesValue);
            result.Data.TodaysAverageSalesValue.ShouldBe(TestData.TodaysSales.TodaysAverageSalesValue);
            result.Data.TodaysSalesCount.ShouldBe(TestData.TodaysSales.TodaysSalesCount);
            result.Data.TodaysSalesValue.ShouldBe(TestData.TodaysSales.TodaysSalesValue);
        }

        [Fact]
        public async Task EstateReportingApiClient_GetTodaysFailedSales_SendAsyncThrowsException_ResultFailed() {
            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ThrowsAsync(new Exception());

            var result = await this.EstateReportingApiClient.GetTodaysFailedSales("", Guid.NewGuid(), 1, 1, "00", DateTime.Now, CancellationToken.None);
            result.IsFailed.ShouldBeTrue();
        }

        [Fact]
        public async Task EstateReportingApiClient_GetTodaysSales_SalesReturned() {
            var resultData = TestData.TodaysSales;

            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(JsonConvert.SerializeObject(resultData)) });

            var result = await this.EstateReportingApiClient.GetTodaysSales("", Guid.NewGuid(), 1, 1, DateTime.Now, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            result.Data.ComparisonAverageSalesValue.ShouldBe(TestData.TodaysSales.ComparisonAverageSalesValue);
            result.Data.ComparisonSalesCount.ShouldBe(TestData.TodaysSales.ComparisonSalesCount);
            result.Data.ComparisonSalesValue.ShouldBe(TestData.TodaysSales.ComparisonSalesValue);
            result.Data.TodaysAverageSalesValue.ShouldBe(TestData.TodaysSales.TodaysAverageSalesValue);
            result.Data.TodaysSalesCount.ShouldBe(TestData.TodaysSales.TodaysSalesCount);
            result.Data.TodaysSalesValue.ShouldBe(TestData.TodaysSales.TodaysSalesValue);
        }

        [Fact]
        public async Task EstateReportingApiClient_GetTodaysSales_SendAsyncThrowsException_ResultFailed() {
            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ThrowsAsync(new Exception());

            var result = await this.EstateReportingApiClient.GetTodaysSales("", Guid.NewGuid(), 1, 1, DateTime.Now, CancellationToken.None);
            result.IsFailed.ShouldBeTrue();
        }

        [Fact]
        public async Task EstateReportingApiClient_GetTodaysSalesCountByHour_CountsReturned() {
            var resultData = TestData.TodaysSalesCountByHourList;

            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(JsonConvert.SerializeObject(resultData)) });

            var result = await this.EstateReportingApiClient.GetTodaysSalesCountByHour("", Guid.NewGuid(), 1, 1, DateTime.Now, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            result.Data.Count.ShouldBe(TestData.TodaysSalesCountByHourList.Count);
        }

        [Fact]
        public async Task EstateReportingApiClient_GetTodaysSalesCountByHour_SendAsyncThrowsException_ResultFailed() {
            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ThrowsAsync(new Exception());

            var result = await this.EstateReportingApiClient.GetTodaysSalesCountByHour("", Guid.NewGuid(), 1, 1, DateTime.Now, CancellationToken.None);
            result.IsFailed.ShouldBeTrue();
        }

        [Fact]
        public async Task EstateReportingApiClient_GetTodaysSalesValueByHour_ValuesReturned() {
            var resultData = TestData.TodaysSalesValueByHourList;

            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(JsonConvert.SerializeObject(resultData)) });

            var result = await this.EstateReportingApiClient.GetTodaysSalesValueByHour("", Guid.NewGuid(), 1, 1, DateTime.Now, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            result.Data.Count.ShouldBe(TestData.TodaysSalesValueByHourList.Count);
        }

        [Fact]
        public async Task EstateReportingApiClient_GetTodaysSalesValueByHour_SendAsyncThrowsException_ResultFailed() {
            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ThrowsAsync(new Exception());

            var result = await this.EstateReportingApiClient.GetTodaysSalesValueByHour("", Guid.NewGuid(), 1, 1, DateTime.Now, CancellationToken.None);
            result.IsFailed.ShouldBeTrue();
        }

        [Fact]
        public async Task EstateReportingApiClient_GetTodaysSettlement_SettlementReturned() {
            var resultData = TestData.TodaysSettlement;

            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(JsonConvert.SerializeObject(resultData)) });

            var result = await this.EstateReportingApiClient.GetTodaysSettlement("", Guid.NewGuid(), 1, 1, DateTime.Now, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            result.Data.ComparisonPendingSettlementCount.ShouldBe(TestData.TodaysSettlement.ComparisonPendingSettlementCount);
            result.Data.ComparisonPendingSettlementValue.ShouldBe(TestData.TodaysSettlement.ComparisonPendingSettlementValue);
            result.Data.ComparisonSettlementCount.ShouldBe(TestData.TodaysSettlement.ComparisonSettlementCount);
            result.Data.ComparisonSettlementValue.ShouldBe(TestData.TodaysSettlement.ComparisonSettlementValue);
            result.Data.TodaysPendingSettlementCount.ShouldBe(TestData.TodaysSettlement.TodaysPendingSettlementCount);
            result.Data.TodaysPendingSettlementValue.ShouldBe(TestData.TodaysSettlement.TodaysPendingSettlementValue);
            result.Data.TodaysSettlementCount.ShouldBe(TestData.TodaysSettlement.TodaysSettlementCount);
            result.Data.TodaysSettlementValue.ShouldBe(TestData.TodaysSettlement.TodaysSettlementValue);
        }

        [Fact]
        public async Task EstateReportingApiClient_GetTodaysSettlement_SendAsyncThrowsException_ResultFailed() {
            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ThrowsAsync(new Exception());

            var result = await this.EstateReportingApiClient.GetTodaysSettlement("", Guid.NewGuid(), 1, 1, DateTime.Now, CancellationToken.None);
            result.IsFailed.ShouldBeTrue();
        }

        [Fact]
        public async Task EstateReportingApiClient_GetTopBottomMerchantData_DataReturned() {
            var resultData = TestData.TopBottomMerchantDataList;

            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(JsonConvert.SerializeObject(resultData)) });

            var result = await this.EstateReportingApiClient.GetTopBottomMerchantData("", Guid.NewGuid(), TopBottom.Top, 5, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            result.Data.Count.ShouldBe(TestData.TopBottomMerchantDataList.Count);
        }

        [Fact]
        public async Task EstateReportingApiClient_GetTopBottomMerchantData_SendAsyncThrowsException_ResultFailed() {
            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ThrowsAsync(new Exception());

            var result = await this.EstateReportingApiClient.GetTopBottomMerchantData("", Guid.NewGuid(), TopBottom.Top, 5, CancellationToken.None);
            result.IsFailed.ShouldBeTrue();
        }

        [Fact]
        public async Task EstateReportingApiClient_GetTopBottomOperatorData_DataReturned() {
            var resultData = TestData.TopBottomOperatorDataList;

            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(JsonConvert.SerializeObject(resultData)) });

            var result = await this.EstateReportingApiClient.GetTopBottomOperatorData("", Guid.NewGuid(), TopBottom.Top, 5, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            result.Data.Count.ShouldBe(TestData.TopBottomOperatorDataList.Count);
        }

        [Fact]
        public async Task EstateReportingApiClient_GetTopBottomOperatorData_SendAsyncThrowsException_ResultFailed() {
            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ThrowsAsync(new Exception());

            var result = await this.EstateReportingApiClient.GetTopBottomOperatorData("", Guid.NewGuid(), TopBottom.Top, 5, CancellationToken.None);
            result.IsFailed.ShouldBeTrue();
        }

        [Fact]
        public async Task EstateReportingApiClient_GetTopBottomProductData_DataReturned() {
            var resultData = TestData.TopBottomProductDataList;

            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(JsonConvert.SerializeObject(resultData)) });

            var result = await this.EstateReportingApiClient.GetTopBottomProductData("", Guid.NewGuid(), TopBottom.Top, 5, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            result.Data.Count.ShouldBe(TestData.TopBottomProductDataList.Count);
        }

        [Fact]
        public async Task EstateReportingApiClient_GetTopBottomProductData_SendAsyncThrowsException_ResultFailed() {
            this.HttpMessageHandler.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ThrowsAsync(new Exception());

            var result = await this.EstateReportingApiClient.GetTopBottomProductData("", Guid.NewGuid(), TopBottom.Top, 5, CancellationToken.None);
            result.IsFailed.ShouldBeTrue();
        }
    }

    public static class TestData {

        public static TodaysSales TodaysSales = new TodaysSales {
            TodaysSalesValue = 1000,
            TodaysAverageSalesValue = 100,
            TodaysSalesCount = 10,
            ComparisonSalesValue = 900,
            ComparisonAverageSalesValue = 90,
            ComparisonSalesCount = 9
        };

        public static LastSettlement LastSettlement = new LastSettlement { SalesCount = 1, FeesValue = 1, SalesValue = 1, SettlementDate = new DateTime(2024, 1, 1) };

        public static List<ComparisonDate> ComparisonDateList = new List<ComparisonDate> { new ComparisonDate { Date = new DateTime(2024, 1, 1), Description = "Jan 1 2024" }, new ComparisonDate { Date = new DateTime(2024, 2, 1), Description = "Feb 1 2024" } };

        public static List<CalendarDate> CalendarDateList = new List<CalendarDate> { new CalendarDate { Date = new DateTime(2024, 1, 1) }, new CalendarDate { Date = new DateTime(2024, 1, 2) }, new CalendarDate { Date = new DateTime(2024, 1, 3) } };

        public static List<CalendarYear> CalendarYearList = new List<CalendarYear> { new CalendarYear { Year = 2024 }, new CalendarYear { Year = 2025 }, new CalendarYear { Year = 2026 } };

        public static List<ResponseCode> ResponseCodeList = new List<ResponseCode> { new ResponseCode { Code = 1, Description = "Success" }, new ResponseCode { Code = 2, Description = "Failure" } };

        public static List<Merchant> MerchantList = new List<Merchant> { new Merchant { MerchantId = Guid.NewGuid(), Name = "Merchant 1" }, new Merchant { MerchantId = Guid.NewGuid(), Name = "Merchant 2" } };

        public static List<Operator> OperatorList = new List<Operator> { new Operator { OperatorId = Guid.NewGuid(), Name = "Operator 1" }, new Operator { OperatorId = Guid.NewGuid(), Name = "Operator 2" } };

        public static List<TransactionResult> TransactionResultList = new List<TransactionResult> { new TransactionResult { TransactionId = Guid.NewGuid(), IsAuthorised = true, ResponseCode = "00", TransactionAmount = 100 }, new TransactionResult { TransactionId = Guid.NewGuid(), IsAuthorised = false, ResponseCode = "01", TransactionAmount = 200 } };

        public static List<UnsettledFee> UnsettledFeeList = new List<UnsettledFee> { new UnsettledFee { DimensionName = "Fee 1", FeesValue = 100, FeesCount = 10 }, new UnsettledFee { DimensionName = "Fee 2", FeesValue = 200, FeesCount = 20 } };

        public static MerchantKpi MerchantKpi = new MerchantKpi { MerchantsWithNoSaleInLast7Days = 1, MerchantsWithNoSaleToday = 2, MerchantsWithSaleInLastHour = 3 };
            
        public static List<TodaysSalesCountByHour> TodaysSalesCountByHourList = new List<TodaysSalesCountByHour> { new TodaysSalesCountByHour { Hour = 1, TodaysSalesCount = 10, ComparisonSalesCount = 9 }, new TodaysSalesCountByHour { Hour = 2, TodaysSalesCount = 20, ComparisonSalesCount = 18 } };

        public static List<TodaysSalesValueByHour> TodaysSalesValueByHourList = new List<TodaysSalesValueByHour> { new TodaysSalesValueByHour { Hour = 1, TodaysSalesValue = 100, ComparisonSalesValue = 90 }, new TodaysSalesValueByHour { Hour = 2, TodaysSalesValue = 200, ComparisonSalesValue = 180 } };

        public static TodaysSettlement TodaysSettlement = new TodaysSettlement {
            TodaysSettlementValue = 1000,
            TodaysPendingSettlementValue = 500,
            TodaysSettlementCount = 10,
            TodaysPendingSettlementCount = 5,
            ComparisonSettlementValue = 900,
            ComparisonPendingSettlementValue = 450,
            ComparisonSettlementCount = 9,
            ComparisonPendingSettlementCount = 4
        };

        public static List<TopBottomMerchantData> TopBottomMerchantDataList = new List<TopBottomMerchantData> { new TopBottomMerchantData { MerchantName = "Merchant 1", SalesValue = 1000 }, new TopBottomMerchantData { MerchantName = "Merchant 2", SalesValue = 900 } };

        public static List<TopBottomOperatorData> TopBottomOperatorDataList = new List<TopBottomOperatorData> { new TopBottomOperatorData { OperatorName = "Operator 1", SalesValue = 1000 }, new TopBottomOperatorData { OperatorName = "Operator 2", SalesValue = 900 } };

        public static List<TopBottomProductData> TopBottomProductDataList = new List<TopBottomProductData> { new TopBottomProductData { ProductName = "Product 1", SalesValue = 1000 }, new TopBottomProductData { ProductName = "Product 2", SalesValue = 900 } };
    }
}
