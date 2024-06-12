using System;
using System.Collections.Generic;
using System.Text;

namespace EstateReportingAPI.Client{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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

        public async Task<LastSettlement> GetLastSettlement(String accessToken, Guid estateId, CancellationToken cancellationToken){
            LastSettlement response = null;

            String requestUri = this.BuildRequestUrl("/api/facts/settlements/lastsettlement");

            try{
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Add("EstateId", estateId.ToString());

                // Make the Http Call here
                HttpResponseMessage httpResponse = await this.HttpClient.SendAsync(request, cancellationToken);

                // Process the response
                String content = await this.HandleResponse(httpResponse, cancellationToken);

                // call was successful so now deserialise the body to the response object
                response = JsonConvert.DeserializeObject<LastSettlement>(content);
            }
            catch(Exception ex){
                // An exception has occurred, add some additional information to the message
                Exception exception = new Exception($"Error getting last settlement for estate {estateId}.", ex);

                throw exception;
            }

            return response;
        }

        public async Task<List<ResponseCode>> GetResponseCodes(String accessToken, Guid estateId, CancellationToken cancellationToken){
            List<ResponseCode> response = null;

            String requestUri = this.BuildRequestUrl("/api/dimensions/responsecodes");

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
                response = JsonConvert.DeserializeObject<List<ResponseCode>>(content);
            }
            catch (Exception ex)
            {
                // An exception has occurred, add some additional information to the message
                Exception exception = new Exception($"Error getting response codes for estate {estateId}.", ex);

                throw exception;
            }

            return response;
        }

        public async Task<TodaysSales> GetMerchantPerformance(String accessToken, Guid estateId, DateTime comparisonDate, List<Int32> merchantReportingIds, CancellationToken cancellationToken){
            
            // Serialize the integer array into a comma-separated string
            string serializedArray = string.Join(",", merchantReportingIds);
            
            TodaysSales response = null;

            String requestUri = this.BuildRequestUrl($"/api/facts/transactions/merchants/performance?comparisonDate={comparisonDate.Date:yyyy-MM-dd}&merchantReportingIds={serializedArray}");

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
                response = JsonConvert.DeserializeObject<TodaysSales>(content);
            }
            catch (Exception ex)
            {
                // An exception has occurred, add some additional information to the message
                Exception exception = new Exception($"Error getting merchant performance for estate {estateId}.", ex);

                throw exception;
            }

            return response;
        }

        public async Task<TodaysSales> GetProductPerformance(String accessToken, Guid estateId, DateTime comparisonDate, List<Int32> productReportingIds, CancellationToken cancellationToken){
            // Serialize the integer array into a comma-separated string
            string serializedArray = string.Join(",", productReportingIds);

            TodaysSales response = null;

            String requestUri = this.BuildRequestUrl($"/api/facts/transactions/products/performance?comparisonDate={comparisonDate.Date:yyyy-MM-dd}&productReportingIds={serializedArray}");

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
                response = JsonConvert.DeserializeObject<TodaysSales>(content);
            }
            catch (Exception ex)
            {
                // An exception has occurred, add some additional information to the message
                Exception exception = new Exception($"Error getting product performance for estate {estateId}.", ex);

                throw exception;
            }

            return response;
        }

        public async Task<List<Merchant>> GetMerchantsByLastSaleDate(String accessToken, Guid estateId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken){
            List<Merchant> response = new List<Merchant>();

            String requestUri = this.BuildRequestUrl($"/api/facts/transactions/merchants/lastsale?startDate={startDate:yyyy-MM-dd HH:mm:ss}&enddate={endDate:yyyy-MM-dd HH:mm:ss}");

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
                response = JsonConvert.DeserializeObject<List<Merchant>>(content);
            }
            catch (Exception ex)
            {
                // An exception has occurred, add some additional information to the message
                Exception exception = new Exception($"Error getting merchant by last sale date estate {estateId}.", ex);

                throw exception;
            }

            return response;
        }

        public async Task<TodaysSales> GetOperatorPerformance(String accessToken, Guid estateId, DateTime comparisonDate, List<Int32> operatorReportingIds, CancellationToken cancellationToken){
            // Serialize the integer array into a comma-separated string
            string serializedArray = string.Join(",", operatorReportingIds);

            TodaysSales response = null;

            String requestUri = this.BuildRequestUrl($"/api/facts/transactions/operators/performance?comparisonDate={comparisonDate.Date:yyyy-MM-dd}&operatorReportingIds={serializedArray}");

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
                response = JsonConvert.DeserializeObject<TodaysSales>(content);
            }
            catch (Exception ex)
            {
                // An exception has occurred, add some additional information to the message
                Exception exception = new Exception($"Error getting operator performance for estate {estateId}.", ex);

                throw exception;
            }

            return response;
        }

        public async Task<List<TransactionResult>> TransactionSearch(String accessToken, Guid estateId, TransactionSearchRequest searchRequest, Int32? page, Int32? pageSize, SortField? sortField, SortDirection? sortDirection, CancellationToken cancellationToken){

            List<TransactionResult> response = null;
            QueryStringBuilder builder = new QueryStringBuilder();
            if (page.HasValue){
                builder.AddParameter("page", page.Value);
            }
            if (pageSize.HasValue)
            {
                builder.AddParameter("pageSize", pageSize.Value);
            }
            if (sortField.HasValue)
            {
                builder.AddParameter("sortField", (Int32)sortField.Value);
            }
            if (sortDirection.HasValue)
            {
                builder.AddParameter("sortDirection", (Int32)sortDirection.Value);
            }
            
            String requestUri = this.BuildRequestUrl($"/api/facts/transactions/search?{builder.BuildQueryString()}");

            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Add("EstateId", estateId.ToString());

                request.Content = new StringContent(JsonConvert.SerializeObject(searchRequest), Encoding.UTF8, "application/json");

                // Make the Http Call here
                HttpResponseMessage httpResponse = await this.HttpClient.SendAsync(request, cancellationToken);

                // Process the response
                String content = await this.HandleResponse(httpResponse, cancellationToken);

                // call was successful so now deserialise the body to the response object
                response = JsonConvert.DeserializeObject<List<TransactionResult>>(content);
            }
            catch (Exception ex)
            {
                // An exception has occurred, add some additional information to the message
                Exception exception = new Exception($"Error getting operator performance for estate {estateId}.", ex);

                throw exception;
            }

            return response;
        }

        public async Task<List<UnsettledFee>> GetUnsettledFees(String accessToken, Guid estateId, DateTime startDate, DateTime endDate, List<Int32> merchantIds, List<Int32> operatorIds, List<Int32> productIds, GroupByOption groupBy, CancellationToken cancellationToken){
            List<UnsettledFee> response = null;
            QueryStringBuilder builder = new QueryStringBuilder();
            builder.AddParameter("startDate", $"{startDate:yyyy-MM-dd}");
            builder.AddParameter("endDate", $"{endDate:yyyy-MM-dd}");
            if (merchantIds != null)
            {
                string serializedMerchantIdsArray = string.Join(",", merchantIds);
                builder.AddParameter("merchantIds", serializedMerchantIdsArray);
            }
            if (operatorIds != null)
            {
                string serializedOperatorIdsArray = string.Join(",", operatorIds);
                builder.AddParameter("operatorIds", serializedOperatorIdsArray);
            }
            if (productIds != null)
            {
                string serializedProductIdsArray = string.Join(",", productIds);
                builder.AddParameter("productIds", serializedProductIdsArray);
            }

            builder.AddParameter("groupByOption", (Int32)groupBy, true);


            String requestUri = this.BuildRequestUrl($"/api/facts/settlements/unsettledfees?{builder.BuildQueryString()}");

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
                response = JsonConvert.DeserializeObject<List<UnsettledFee>>(content);
            }
            catch (Exception ex)
            {
                // An exception has occurred, add some additional information to the message
                Exception exception = new Exception($"Error getting unsettled fees for estate {estateId}.", ex);

                throw exception;
            }

            return response;

        }

        public async Task<MerchantKpi> GetMerchantKpi(String accessToken, Guid estateId, CancellationToken cancellationToken){
            MerchantKpi response = null;

            String requestUri = this.BuildRequestUrl("/api/facts/transactions/merchantkpis");

            try{
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
            catch(Exception ex){
                // An exception has occurred, add some additional information to the message
                Exception exception = new Exception($"Error getting merchant kpis for estate {estateId}.", ex);

                throw exception;
            }

            return response;
        }

        public async Task<List<Merchant>> GetMerchants(String accessToken, Guid estateId, CancellationToken cancellationToken){
            List<Merchant> response = null;

            String requestUri = this.BuildRequestUrl("/api/dimensions/merchants");

            try{
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Add("EstateId", estateId.ToString());

                // Make the Http Call here
                HttpResponseMessage httpResponse = await this.HttpClient.SendAsync(request, cancellationToken);

                // Process the response
                String content = await this.HandleResponse(httpResponse, cancellationToken);

                // call was successful so now deserialise the body to the response object
                response = JsonConvert.DeserializeObject<List<Merchant>>(content);
            }
            catch(Exception ex){
                // An exception has occurred, add some additional information to the message
                Exception exception = new Exception($"Error getting merchants for estate {estateId}.", ex);

                throw exception;
            }

            return response;
        }

        public async Task<List<Operator>> GetOperators(String accessToken, Guid estateId, CancellationToken cancellationToken){
            List<Operator> response = null;

            String requestUri = this.BuildRequestUrl("/api/dimensions/operators");

            try{
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Add("EstateId", estateId.ToString());

                // Make the Http Call here
                HttpResponseMessage httpResponse = await this.HttpClient.SendAsync(request, cancellationToken);

                // Process the response
                String content = await this.HandleResponse(httpResponse, cancellationToken);

                // call was successful so now deserialise the body to the response object
                response = JsonConvert.DeserializeObject<List<Operator>>(content);
            }
            catch(Exception ex){
                // An exception has occurred, add some additional information to the message
                Exception exception = new Exception($"Error getting operators for estate {estateId}.", ex);

                throw exception;
            }

            return response;
        }

        public async Task<TodaysSales> GetTodaysFailedSales(String accessToken, Guid estateId, Int32 merchantReportingId, Int32 operatorReportingId, String responseCode, DateTime comparisonDate, CancellationToken cancellationToken){
            TodaysSales response = null;


            QueryStringBuilder builder = new QueryStringBuilder();
            builder.AddParameter("comparisonDate", $"{comparisonDate.Date:yyyy-MM-dd}");
            builder.AddParameter("merchantReportingId", merchantReportingId);
            builder.AddParameter("operatorReportingId", operatorReportingId);
            builder.AddParameter("responseCode", responseCode);

            String requestUri = this.BuildRequestUrl($"/api/facts/transactions/todaysfailedsales?{builder.BuildQueryString()}");

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
                Exception exception = new Exception($"Error getting todays failed sales for estate {estateId} and response code {responseCode}.", ex);

                throw exception;
            }

            return response;
        }

        public async Task<TodaysSales> GetTodaysSales(String accessToken, Guid estateId, Int32 merchantReportingId, Int32 operatorReportingId, DateTime comparisonDate, CancellationToken cancellationToken){
            TodaysSales response = null;

            QueryStringBuilder builder = new QueryStringBuilder();
            builder.AddParameter("comparisonDate", $"{comparisonDate.Date:yyyy-MM-dd}");
            builder.AddParameter("merchantReportingId", merchantReportingId);
            builder.AddParameter("operatorReportingId", operatorReportingId);

            String requestUri = this.BuildRequestUrl($"/api/facts/transactions/todayssales?{builder.BuildQueryString()}");

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

        public async Task<List<TodaysSalesCountByHour>> GetTodaysSalesCountByHour(String accessToken, Guid estateId, Int32 merchantReportingId, Int32 operatorReportingId, DateTime comparisonDate, CancellationToken cancellationToken){
            List<TodaysSalesCountByHour> response = null;


            QueryStringBuilder builder = new QueryStringBuilder();
            builder.AddParameter("comparisonDate", $"{comparisonDate.Date:yyyy-MM-dd}");
            builder.AddParameter("merchantReportingId", merchantReportingId);
            builder.AddParameter("operatorReportingId", operatorReportingId);

            String requestUri = this.BuildRequestUrl($"/api/facts/transactions/todayssales/countbyhour?{builder.BuildQueryString()}");

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

        public async Task<List<TodaysSalesValueByHour>> GetTodaysSalesValueByHour(String accessToken, Guid estateId, Int32 merchantReportingId, Int32 operatorReportingId, DateTime comparisonDate, CancellationToken cancellationToken){
            List<TodaysSalesValueByHour> response = null;


            QueryStringBuilder builder = new QueryStringBuilder();
            builder.AddParameter("comparisonDate", $"{comparisonDate.Date:yyyy-MM-dd}");
            builder.AddParameter("merchantReportingId", merchantReportingId);
            builder.AddParameter("operatorReportingId", operatorReportingId);

            String requestUri = this.BuildRequestUrl($"/api/facts/transactions/todayssales/valuebyhour?{builder.BuildQueryString()}");

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

        public async Task<TodaysSettlement> GetTodaysSettlement(String accessToken, Guid estateId, Int32 merchantReportingId, Int32 operatorReportingId, DateTime comparisonDate, CancellationToken cancellationToken){
            TodaysSettlement response = null;
            
            QueryStringBuilder builder = new QueryStringBuilder();
            builder.AddParameter("comparisonDate", $"{comparisonDate.Date:yyyy-MM-dd}");
            builder.AddParameter("merchantReportingId", merchantReportingId);
            builder.AddParameter("operatorReportingId", operatorReportingId);

            String requestUri = this.BuildRequestUrl($"/api/facts/settlements/todayssettlement?{builder.BuildQueryString()}");

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

        public async Task<List<TopBottomMerchantData>> GetTopBottomMerchantData(String accessToken, Guid estateId, TopBottom topBottom, Int32 resultCount, CancellationToken cancellationToken){
            List<TopBottomMerchantData> response = null;

            String requestUri = this.BuildRequestUrl($"/api/facts/transactions/merchants/topbottombyvalue?topOrBottom={(Int32)topBottom}&count={resultCount}");

            try{
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Add("EstateId", estateId.ToString());

                // Make the Http Call here
                HttpResponseMessage httpResponse = await this.HttpClient.SendAsync(request, cancellationToken);

                // Process the response
                String content = await this.HandleResponse(httpResponse, cancellationToken);

                // call was successful so now deserialise the body to the response object
                response = JsonConvert.DeserializeObject<List<TopBottomMerchantData>>(content);
            }
            catch(Exception ex){
                // An exception has occurred, add some additional information to the message
                Exception exception = new Exception($"Error getting top/bottom sales by merchant for estate {estateId} TopOrBottom {topBottom} ans count {resultCount}.", ex);

                throw exception;
            }

            return response;
        }

        public async Task<List<TopBottomOperatorData>> GetTopBottomOperatorData(String accessToken, Guid estateId, TopBottom topBottom, Int32 resultCount, CancellationToken cancellationToken){
            List<TopBottomOperatorData> response = null;

            String requestUri = this.BuildRequestUrl($"/api/facts/transactions/operators/topbottombyvalue?topOrBottom={(Int32)topBottom}&count={resultCount}");

            try{
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Add("EstateId", estateId.ToString());

                // Make the Http Call here
                HttpResponseMessage httpResponse = await this.HttpClient.SendAsync(request, cancellationToken);

                // Process the response
                String content = await this.HandleResponse(httpResponse, cancellationToken);

                // call was successful so now deserialise the body to the response object
                response = JsonConvert.DeserializeObject<List<TopBottomOperatorData>>(content);
            }
            catch(Exception ex){
                // An exception has occurred, add some additional information to the message
                Exception exception = new Exception($"Error getting top/bottom sales by operator for estate {estateId} TopOrBottom {topBottom} ans count {resultCount}.", ex);

                throw exception;
            }

            return response;
        }

        public async Task<List<TopBottomProductData>> GetTopBottomProductData(String accessToken, Guid estateId, TopBottom topBottom, Int32 resultCount, CancellationToken cancellationToken){
            List<TopBottomProductData> response = null;

            String requestUri = this.BuildRequestUrl($"/api/facts/transactions/products/topbottombyvalue?topOrBottom={(Int32)topBottom}&count={resultCount}");

            try{
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Add("EstateId", estateId.ToString());

                // Make the Http Call here
                HttpResponseMessage httpResponse = await this.HttpClient.SendAsync(request, cancellationToken);

                // Process the response
                String content = await this.HandleResponse(httpResponse, cancellationToken);

                // call was successful so now deserialise the body to the response object
                response = JsonConvert.DeserializeObject<List<TopBottomProductData>>(content);
            }
            catch(Exception ex){
                // An exception has occurred, add some additional information to the message
                Exception exception = new Exception($"Error getting top/bottom sales by product for estate {estateId} TopOrBottom {topBottom} ans count {resultCount}.", ex);

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

public class QueryStringBuilder
{
    private Dictionary<string, (object value, Boolean alwaysInclude)> parameters = new Dictionary<String, (Object value, Boolean alwaysInclude)>();

    public QueryStringBuilder AddParameter(string key, object value, Boolean alwaysInclude=false)
    {
        this.parameters.Add(key, (value, alwaysInclude));
        return this;
    }

    static Dictionary<string, object> FilterDictionary(Dictionary<string, (object value, Boolean alwaysInclude)> inputDictionary)
    {
        Dictionary<string, object> result = new Dictionary<string, object>();

        foreach (KeyValuePair<String, (object value, Boolean alwaysInclude)> entry in inputDictionary)
        {
            if (entry.Value.value != null && !IsDefaultValue(entry.Value.value, entry.Value.alwaysInclude))
            {
                result.Add(entry.Key, entry.Value.value);
            }
        }

        return result;
    }

    static bool IsDefaultValue<T>(T value, Boolean alwaysInclude){
        if (alwaysInclude)
            return false;

        Object? defaultValue = GetDefault(value.GetType());

        if (defaultValue == null && value.GetType() == typeof(String))
        {
            defaultValue = String.Empty;
        }
        return defaultValue.Equals(value);
    }

    public static object GetDefault(Type t)
    {
        Func<object> f = GetDefault<object>;
        return f.Method.GetGenericMethodDefinition().MakeGenericMethod(t).Invoke(null, null);
    }

    private static T GetDefault<T>()
    {
        return default(T);
    }

    public string BuildQueryString(){
        Dictionary<String, Object> filtered = FilterDictionary(this.parameters);

        if (filtered.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder queryString = new StringBuilder();
        
        foreach (KeyValuePair<String, Object> kvp in filtered)
        {
            if (queryString.Length > 0)
            {
                queryString.Append("&");
            }

            queryString.Append($"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value.ToString())}");
        }

        return queryString.ToString();
    }
}
