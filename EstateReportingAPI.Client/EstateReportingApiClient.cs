namespace EstateReportingAPI.Client{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;
    using ClientProxyBase;
    using DataTransferObjects;
    using DataTrasferObjects;
    using Newtonsoft.Json;

    public class EstateReportingApiClient : ClientProxyBase, IEstateReportingApiClient{
        #region Fields

        private readonly Func<String, String> BaseAddressResolver;

        #endregion

        #region Constructors

        public EstateReportingApiClient(Func<String, String> baseAddressResolver,
                                        HttpClient httpClient) : base(httpClient){
            this.BaseAddressResolver = baseAddressResolver;

            // Add the API version header
            this.HttpClient.DefaultRequestHeaders.Add("api-version", "1.0");
        }

        #endregion

        #region Methods

        public async Task<List<CalendarDate>> GetCalendarDates(String accessToken, Guid estateId, Int32 year, CancellationToken cancellationToken){
            List<CalendarDate> response = null;

            String requestUri = this.BuildRequestUrl($"/api/dimensions/calendar/{year}/dates");

            try{
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Add("EstateId", estateId.ToString());
                
                // Make the Http Call here
                HttpResponseMessage httpResponse = await this.HttpClient.SendAsync(request, cancellationToken);

                // Process the response
                String content = await this.HandleResponse(httpResponse, cancellationToken);

                // call was successful so now deserialise the body to the response object
                response = JsonConvert.DeserializeObject<List<CalendarDate>>(content);
            }
            catch(Exception ex){
                // An exception has occurred, add some additional information to the message
                Exception exception = new Exception($"Error getting calendar dates for year {year} for estate {{estateId}}.", ex);

                throw exception;
            }

            return response;
        }

        public async Task<List<CalendarYear>> GetCalendarYears(String accessToken, Guid estateId, CancellationToken cancellationToken){
            List<CalendarYear> response = null;

            String requestUri = this.BuildRequestUrl("/api/dimensions/calendar/years");

            try{
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Add("EstateId", estateId.ToString());

                // Make the Http Call here
                HttpResponseMessage httpResponse = await this.HttpClient.SendAsync(request, cancellationToken);

                // Process the response
                String content = await this.HandleResponse(httpResponse, cancellationToken);

                // call was successful so now deserialise the body to the response object
                response = JsonConvert.DeserializeObject<List<CalendarYear>>(content);
            }
            catch(Exception ex){
                // An exception has occurred, add some additional information to the message
                Exception exception = new Exception($"Error getting calendar years for estate {estateId}.", ex);

                throw exception;
            }

            return response;
        }

        public async Task<List<ComparisonDate>> GetComparisonDates(String accessToken, Guid estateId, CancellationToken cancellationToken){
            List<ComparisonDate> response = null;

            String requestUri = this.BuildRequestUrl("/api/dimensions/calendar/comparisondates");

            try{
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Add("EstateId", estateId.ToString());

                // Make the Http Call here
                HttpResponseMessage httpResponse = await this.HttpClient.SendAsync(request, cancellationToken);

                // Process the response
                String content = await this.HandleResponse(httpResponse, cancellationToken);

                // call was successful so now deserialise the body to the response object
                response = JsonConvert.DeserializeObject<List<ComparisonDate>>(content);
            }
            catch(Exception ex){
                // An exception has occurred, add some additional information to the message
                Exception exception = new Exception($"Error getting comparison dates for estate {estateId}.", ex);

                throw exception;
            }

            return response;
        }

        public async Task<TodaysSales> GetTodaysSales(String accessToken, Guid estateId, DateTime comparisonDate, CancellationToken cancellationToken){
            TodaysSales response = null;

            String requestUri = this.BuildRequestUrl($"/api/facts/transactions/todayssales?comparisonDate={comparisonDate.Date:yyyy-MM-dd}");

            try{
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Add("EstateId", estateId.ToString());

                // Make the Http Call here
                HttpResponseMessage httpResponse = await this.HttpClient.SendAsync(request, cancellationToken);

                // Process the response
                String content = await this.HandleResponse(httpResponse, cancellationToken);

                // call was successful so now deserialise the body to the response object
                response = JsonConvert.DeserializeObject<TodaysSales>(content);
            }
            catch(Exception ex){
                // An exception has occurred, add some additional information to the message
                Exception exception = new Exception($"Error getting todays sales for estate {estateId}.", ex);

                throw exception;
            }

            return response;
        }

        public async Task<List<TodaysSalesCountByHour>> GetTodaysSalesCountByHour(String accessToken, Guid estateId, DateTime comparisonDate, CancellationToken cancellationToken){
            List<TodaysSalesCountByHour> response = null;

            String requestUri = this.BuildRequestUrl($"/api/facts/transactions/todayssales/countbyhour?comparisonDate={comparisonDate.Date:yyyy-MM-dd}");

            try{
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Add("EstateId", estateId.ToString());

                // Make the Http Call here
                HttpResponseMessage httpResponse = await this.HttpClient.SendAsync(request, cancellationToken);

                // Process the response
                String content = await this.HandleResponse(httpResponse, cancellationToken);

                // call was successful so now deserialise the body to the response object
                response = JsonConvert.DeserializeObject<List<TodaysSalesCountByHour>>(content);
            }
            catch(Exception ex){
                // An exception has occurred, add some additional information to the message
                Exception exception = new Exception($"Error getting todays sales count by hour for estate {estateId}.", ex);

                throw exception;
            }

            return response;
        }

        public async Task<List<TodaysSalesValueByHour>> GetTodaysSalesValueByHour(String accessToken, Guid estateId, DateTime comparisonDate, CancellationToken cancellationToken){
            List<TodaysSalesValueByHour> response = null;

            String requestUri = this.BuildRequestUrl($"/api/facts/transactions/todayssales/valuebyhour?comparisonDate={comparisonDate.Date:yyyy-MM-dd}");

            try{
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Add("EstateId", estateId.ToString());

                // Make the Http Call here
                HttpResponseMessage httpResponse = await this.HttpClient.SendAsync(request, cancellationToken);

                // Process the response
                String content = await this.HandleResponse(httpResponse, cancellationToken);

                // call was successful so now deserialise the body to the response object
                response = JsonConvert.DeserializeObject<List<TodaysSalesValueByHour>>(content);
            }
            catch(Exception ex){
                // An exception has occurred, add some additional information to the message
                Exception exception = new Exception($"Error getting todays sales value by hour for estate {estateId}.", ex);

                throw exception;
            }

            return response;
        }

        public async Task<TodaysSettlement> GetTodaysSettlement(String accessToken, Guid estateId, DateTime comparisonDate, CancellationToken cancellationToken){
            TodaysSettlement response = null;

            String requestUri = this.BuildRequestUrl($"/api/facts/settlements/todayssettlement?comparisonDate={comparisonDate.Date:yyyy-MM-dd}");

            try{
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Add("EstateId", estateId.ToString());

                // Make the Http Call here
                HttpResponseMessage httpResponse = await this.HttpClient.SendAsync(request, cancellationToken);

                // Process the response
                String content = await this.HandleResponse(httpResponse, cancellationToken);

                // call was successful so now deserialise the body to the response object
                response = JsonConvert.DeserializeObject<TodaysSettlement>(content);
            }
            catch(Exception ex){
                // An exception has occurred, add some additional information to the message
                Exception exception = new Exception($"Error getting todays settlement for estate {estateId}.", ex);

                throw exception;
            }

            return response;
        }

        public async Task<MerchantKpi> GetMerchantKpi(String accessToken, Guid estateId, CancellationToken cancellationToken){
            MerchantKpi response = null;

            String requestUri = this.BuildRequestUrl($"/api/facts/transactions/merchantkpi");

            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Add("EstateId", estateId.ToString());

                // Make the Http Call here
                HttpResponseMessage httpResponse = await this.HttpClient.SendAsync(request, cancellationToken);

                // Process the response
                String content = await this.HandleResponse(httpResponse, cancellationToken);

                // call was successful so now deserialise the body to the response object
                response = JsonConvert.DeserializeObject<MerchantKpi>(content);
            }
            catch (Exception ex)
            {
                // An exception has occurred, add some additional information to the message
                Exception exception = new Exception($"Error getting merchant kpis for estate {estateId}.", ex);

                throw exception;
            }

            return response;
        }

        private String BuildRequestUrl(String route){
            String baseAddress = this.BaseAddressResolver("EstateReportingApi");

            String requestUri = $"{baseAddress}{route}";

            return requestUri;
        }

        #endregion
    }
}