using EstateReportingAPI.DataTransferObjects;
using EstateReportingAPI.DataTrasferObjects;
using EstateReportingAPI.Models;
using Io.Cucumber.Messages.Types;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using SimpleResults;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TransactionProcessor.Database.Contexts;
using Xunit;
using TransactionSummaryByMerchantResponse = EstateReportingAPI.Models.TransactionSummaryByMerchantResponse;

namespace EstateReportingAPI.IntegrationTests {
    public class CalendarEndpointTests : ControllerTestsBase {
        private String BaseRoute = "api/calendars";

        [Fact]
        public async Task CalendarEndpoint_GetComparisonDates_DatesReturned() {
            List<DateTime> datesInPreviousYear = helper.GetDatesForYear(DateTime.Now.Year - 1);
            await helper.AddCalendarDates(datesInPreviousYear);
            List<DateTime> datesInYear = helper.GetDatesForYear(DateTime.Now.Year);
            await helper.AddCalendarDates(datesInYear);

            Result<List<ComparisonDate>> result = await this.CreateAndSendHttpRequestMessage<List<ComparisonDate>>($"{this.BaseRoute}/comparisondates", CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();

            List<ComparisonDate> dates = result.Data;
            List<DateTime> expectedDates = datesInYear.Where(d => d <= DateTime.Now.Date.AddDays(-1)).ToList();
            dates.ShouldNotBeNull();
            foreach (DateTime date in expectedDates) {
                dates.Select(d => d.Date).Contains(date.Date).ShouldBeTrue();
            }

            dates.Select(d => d.Description).Contains("Yesterday");
            dates.Select(d => d.Description).Contains("Last Week");
            dates.Select(d => d.Description).Contains("Last Month");
        }

        protected override async Task ClearStandingData() {

        }

        protected override async Task SetupStandingData() {

        }
    }
}


