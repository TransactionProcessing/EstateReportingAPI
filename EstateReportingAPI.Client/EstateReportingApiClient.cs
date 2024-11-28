using System;
using System.Collections.Generic;
using System.Text;
using Shared.Results;
using SimpleResults;

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
    using Microsoft.AspNetCore.Http;
    using Newtonsoft.Json;

    internal class ResponseData<T>
    {
        public T Data { get; set; }
    }

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

        public async Task<Result<List<CalendarDate>>> GetCalendarDates(String accessToken, Guid estateId, Int32 year, CancellationToken cancellationToken){
            String requestUri = this.BuildRequestUrl($"/api/dimensions/calendar/{year}/dates");

            try{
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Add("EstateId", estateId.ToString());

                // Make the Http Call here
                HttpResponseMessage httpResponse = await this.HttpClient.SendAsync(request, cancellationToken);

                // Process the response
                return await ProcessResponse<List<CalendarDate>>(httpResponse, cancellationToken);
            }
            catch(Exception ex){
                // An exception has occurred, add some additional information to the message
                Exception exception = new Exception($"Error getting calendar dates for year {year} for estate {{estateId}}.", ex);

                throw exception;
            }
        }
        
        private async Task<Result<T>> ProcessResponse<T>(HttpResponseMessage httpResponse, CancellationToken cancellationToken) {
            Result<String> result = await this.HandleResponseX(httpResponse, cancellationToken);
            if (result.IsFailed)
                return ResultHelpers.CreateFailure(result);

            ResponseData<T> response = JsonConvert.DeserializeObject<ResponseData<T>>(result.Data);

            return Result.Success(response.Data);
        }

        public async Task<Result<List<CalendarYear>>> GetCalendarYears(String accessToken, Guid estateId, CancellationToken cancellationToken){

            String requestUri = this.BuildRequestUrl("/api/dimensions/calendar/years");

            try{
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Add("EstateId", estateId.ToString());

                // Make the Http Call here
                HttpResponseMessage httpResponse = await this.HttpClient.SendAsync(request, cancellationToken);

                // Process the response
                return await ProcessResponse<List<CalendarYear>>(httpResponse, cancellationToken);
            }
            catch(Exception ex){
                // An exception has occurred, add some additional information to the message
                Exception exception = new Exception($"Error getting calendar years for estate {estateId}.", ex);

                throw exception;
            }
        }

        public async Task<Result<List<ComparisonDate>>> GetComparisonDates(String accessToken, Guid estateId, CancellationToken cancellationToken){
            String requestUri = this.BuildRequestUrl("/api/dimensions/calendar/comparisondates");

            try{
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Add("EstateId", estateId.ToString());

                // Make the Http Call here
                HttpResponseMessage httpResponse = await this.HttpClient.SendAsync(request, cancellationToken);

                // Process the response
                return await ProcessResponse<List<ComparisonDate>>(httpResponse, cancellationToken);
            }
            catch(Exception ex){
                // An exception has occurred, add some additional information to the message
                Exception exception = new Exception($"Error getting comparison dates for estate {estateId}.", ex);

                throw exception;
            }
        }

        public async Task<Result<LastSettlement>> GetLastSettlement(String accessToken, Guid estateId, CancellationToken cancellationToken){
            LastSettlement response = null;

            String requestUri = this.BuildRequestUrl("/api/facts/settlements/lastsettlement");

            try{
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Add("EstateId", estateId.ToString());

                // Make the Http Call here
                HttpResponseMessage httpResponse = await this.HttpClient.SendAsync(request, cancellationToken);

                // Process the response
                return await ProcessResponse<LastSettlement>(httpResponse, cancellationToken);
            }
            catch(Exception ex){
                // An exception has occurred, add some additional information to the message
                Exception exception = new Exception($"Error getting last settlement for estate {estateId}.", ex);

                throw exception;
            }

            return response;
        }

        public async Task<Result<List<ResponseCode>>> GetResponseCodes(String accessToken, Guid estateId, CancellationToken cancellationToken){
            String requestUri = this.BuildRequestUrl("/api/dimensions/responsecodes");

            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Add("EstateId", estateId.ToString());

                // Make the Http Call here
                HttpResponseMessage httpResponse = await this.HttpClient.SendAsync(request, cancellationToken);

                // Process the response
                return await ProcessResponse<List<ResponseCode>>(httpResponse, cancellationToken);
            }
            catch (Exception ex)
            {
                // An exception has occurred, add some additional information to the message
                Exception exception = new Exception($"Error getting response codes for estate {estateId}.", ex);

                throw exception;
            }
        }

        public async Task<Result<TodaysSales>> GetMerchantPerformance(String accessToken, Guid estateId, DateTime comparisonDate, List<Int32> merchantReportingIds, CancellationToken cancellationToken){
            
            // Serialize the integer array into a comma-separated string
            string serializedArray = string.Join(",", merchantReportingIds);
            
            String requestUri = this.BuildRequestUrl($"/api/facts/transactions/merchants/performance?comparisonDate={comparisonDate.Date:yyyy-MM-dd}&merchantReportingIds={serializedArray}");

            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Add("EstateId", estateId.ToString());

                // Make the Http Call here
                HttpResponseMessage httpResponse = await this.HttpClient.SendAsync(request, cancellationToken);

                // Process the response
                return await ProcessResponse<TodaysSales>(httpResponse, cancellationToken);
            }
            catch (Exception ex)
            {
                // An exception has occurred, add some additional information to the message
                Exception exception = new Exception($"Error getting merchant performance for estate {estateId}.", ex);

                throw exception;
            }
        }

        public async Task<Result<TodaysSales>> GetProductPerformance(String accessToken, Guid estateId, DateTime comparisonDate, List<Int32> productReportingIds, CancellationToken cancellationToken){
            // Serialize the integer array into a comma-separated string
            string serializedArray = string.Join(",", productReportingIds);

            String requestUri = this.BuildRequestUrl($"/api/facts/transactions/products/performance?comparisonDate={comparisonDate.Date:yyyy-MM-dd}&productReportingIds={serializedArray}");

            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Add("EstateId", estateId.ToString());

                // Make the Http Call here
                HttpResponseMessage httpResponse = await this.HttpClient.SendAsync(request, cancellationToken);

                // Process the response
                return await ProcessResponse<TodaysSales>(httpResponse, cancellationToken);
            }
            catch (Exception ex)
            {
                // An exception has occurred, add some additional information to the message
                Exception exception = new Exception($"Error getting product performance for estate {estateId}.", ex);

                throw exception;
            }
        }

        public async Task<Result<List<Merchant>>> GetMerchantsByLastSaleDate(String accessToken, Guid estateId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken){

            String requestUri = this.BuildRequestUrl($"/api/facts/transactions/merchants/lastsale?startDate={startDate:yyyy-MM-dd HH:mm:ss}&enddate={endDate:yyyy-MM-dd HH:mm:ss}");

            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Add("EstateId", estateId.ToString());

                // Make the Http Call here
                HttpResponseMessage httpResponse = await this.HttpClient.SendAsync(request, cancellationToken);

                // Process the response
                return await ProcessResponse<List<Merchant>>(httpResponse, cancellationToken);
            }
            catch (Exception ex)
            {
                // An exception has occurred, add some additional information to the message
                Exception exception = new Exception($"Error getting merchant by last sale date estate {estateId}.", ex);

                throw exception;
            }
        }

        public async Task<Result<TodaysSales>> GetOperatorPerformance(String accessToken, Guid estateId, DateTime comparisonDate, List<Int32> operatorReportingIds, CancellationToken cancellationToken){
            // Serialize the integer array into a comma-separated string
            string serializedArray = string.Join(",", operatorReportingIds);

            String requestUri = this.BuildRequestUrl($"/api/facts/transactions/operators/performance?comparisonDate={comparisonDate.Date:yyyy-MM-dd}&operatorReportingIds={serializedArray}");

            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Add("EstateId", estateId.ToString());

                // Make the Http Call here
                HttpResponseMessage httpResponse = await this.HttpClient.SendAsync(request, cancellationToken);

                // Process the response
                return await ProcessResponse<TodaysSales>(httpResponse, cancellationToken);
            }
            catch (Exception ex)
            {
                // An exception has occurred, add some additional information to the message
                Exception exception = new Exception($"Error getting operator performance for estate {estateId}.", ex);

                throw exception;
            }
        }

        public async Task<Result<List<TransactionResult>>> TransactionSearch(String accessToken, Guid estateId, TransactionSearchRequest searchRequest, Int32? page, Int32? pageSize, SortField? sortField, SortDirection? sortDirection, CancellationToken cancellationToken){
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
                return await ProcessResponse<List<TransactionResult>>(httpResponse, cancellationToken);
            }
            catch (Exception ex)
            {
                // An exception has occurred, add some additional information to the message
                Exception exception = new Exception($"Error getting operator performance for estate {estateId}.", ex);

                throw exception;
            }
        }

        public async Task<Result<List<UnsettledFee>>> GetUnsettledFees(String accessToken, Guid estateId, DateTime startDate, DateTime endDate, List<Int32> merchantIds, List<Int32> operatorIds, List<Int32> productIds, GroupByOption groupBy, CancellationToken cancellationToken){
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
                return await ProcessResponse<List<UnsettledFee>>(httpResponse, cancellationToken);
            }
            catch (Exception ex)
            {
                // An exception has occurred, add some additional information to the message
                Exception exception = new Exception($"Error getting unsettled fees for estate {estateId}.", ex);

                throw exception;
            }

        }

        public async Task<Result<MerchantKpi>> GetMerchantKpi(String accessToken, Guid estateId, CancellationToken cancellationToken){
            String requestUri = this.BuildRequestUrl("/api/facts/transactions/merchantkpis");

            try{
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Add("EstateId", estateId.ToString());

                // Make the Http Call here
                HttpResponseMessage httpResponse = await this.HttpClient.SendAsync(request, cancellationToken);

                // Process the response
                return await ProcessResponse<MerchantKpi>(httpResponse, cancellationToken);
            }
            catch(Exception ex){
                // An exception has occurred, add some additional information to the message
                Exception exception = new Exception($"Error getting merchant kpis for estate {estateId}.", ex);

                throw exception;
            }
        }

        public async Task<Result<List<Merchant>>> GetMerchants(String accessToken, Guid estateId, CancellationToken cancellationToken){
            
            String requestUri = this.BuildRequestUrl("/api/dimensions/merchants");

            try{
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Add("EstateId", estateId.ToString());

                // Make the Http Call here
                HttpResponseMessage httpResponse = await this.HttpClient.SendAsync(request, cancellationToken);

                // Process the response
                return await ProcessResponse<List<Merchant>>(httpResponse, cancellationToken);
            }
            catch(Exception ex){
                // An exception has occurred, add some additional information to the message
                Exception exception = new Exception($"Error getting merchants for estate {estateId}.", ex);

                throw exception;
            }
        }

        public async Task<Result<List<Operator>>> GetOperators(String accessToken, Guid estateId, CancellationToken cancellationToken){
            
            String requestUri = this.BuildRequestUrl("/api/dimensions/operators");

            try{
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Add("EstateId", estateId.ToString());

                // Make the Http Call here
                HttpResponseMessage httpResponse = await this.HttpClient.SendAsync(request, cancellationToken);

                // Process the response
                return await ProcessResponse<List<Operator>>(httpResponse, cancellationToken);
            }
            catch(Exception ex){
                // An exception has occurred, add some additional information to the message
                Exception exception = new Exception($"Error getting operators for estate {estateId}.", ex);

                throw exception;
            }
        }

        public async Task<Result<TodaysSales>> GetTodaysFailedSales(String accessToken, Guid estateId, Int32 merchantReportingId, Int32 operatorReportingId, String responseCode, DateTime comparisonDate, CancellationToken cancellationToken){
            QueryStringBuilder builder = new QueryStringBuilder();
            builder.AddParameter("comparisonDate", $"{comparisonDate.Date:yyyy-MM-dd}");
            builder.AddParameter("merchantReportingId", merchantReportingId);
            builder.AddParameter("operatorReportingId", operatorReportingId);
            builder.AddParameter("responseCode", responseCode);

            String requestUri = this.BuildRequestUrl($"/api/facts/transactions/todaysfailedsales?{builder.BuildQueryString()}");

            try {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Add("EstateId", estateId.ToString());

                // Make the Http Call here
                HttpResponseMessage httpResponse = await this.HttpClient.SendAsync(request, cancellationToken);

                // Process the response
                return await ProcessResponse<TodaysSales>(httpResponse, cancellationToken);
            }
            catch (Exception ex) {
                // An exception has occurred, add some additional information to the message
                Exception exception = new Exception($"Error getting todays failed sales for estate {estateId} and response code {responseCode}.", ex);

                throw exception;
            }
        }

        public async Task<Result<TodaysSales>> GetTodaysSales(String accessToken, Guid estateId, Int32 merchantReportingId, Int32 operatorReportingId, DateTime comparisonDate, CancellationToken cancellationToken){
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
                return await ProcessResponse<TodaysSales>(httpResponse, cancellationToken);
            }
            catch(Exception ex){
                // An exception has occurred, add some additional information to the message
                Exception exception = new Exception($"Error getting todays sales for estate {estateId}.", ex);

                throw exception;
            }
        }

        public async Task<Result<List<TodaysSalesCountByHour>>> GetTodaysSalesCountByHour(String accessToken, Guid estateId, Int32 merchantReportingId, Int32 operatorReportingId, DateTime comparisonDate, CancellationToken cancellationToken){
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
                return await ProcessResponse<List<TodaysSalesCountByHour>>(httpResponse, cancellationToken);
            }
            catch(Exception ex){
                // An exception has occurred, add some additional information to the message
                Exception exception = new Exception($"Error getting todays sales count by hour for estate {estateId}.", ex);

                throw exception;
            }
        }

        public async Task<Result<List<TodaysSalesValueByHour>>> GetTodaysSalesValueByHour(String accessToken, Guid estateId, Int32 merchantReportingId, Int32 operatorReportingId, DateTime comparisonDate, CancellationToken cancellationToken){
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
                return await ProcessResponse<List<TodaysSalesValueByHour>>(httpResponse, cancellationToken);
            }
            catch(Exception ex){
                // An exception has occurred, add some additional information to the message
                Exception exception = new Exception($"Error getting todays sales value by hour for estate {estateId}.", ex);

                throw exception;
            }
        }

        public async Task<Result<TodaysSettlement>> GetTodaysSettlement(String accessToken, Guid estateId, Int32 merchantReportingId, Int32 operatorReportingId, DateTime comparisonDate, CancellationToken cancellationToken){
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
                return await ProcessResponse<TodaysSettlement>(httpResponse, cancellationToken);
            }
            catch(Exception ex){
                // An exception has occurred, add some additional information to the message
                Exception exception = new Exception($"Error getting todays settlement for estate {estateId}.", ex);

                throw exception;
            }
        }

        public async Task<Result<List<TopBottomMerchantData>>> GetTopBottomMerchantData(String accessToken, Guid estateId, TopBottom topBottom, Int32 resultCount, CancellationToken cancellationToken){
            String requestUri = this.BuildRequestUrl($"/api/facts/transactions/merchants/topbottombyvalue?topOrBottom={(Int32)topBottom}&count={resultCount}");

            try{
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Add("EstateId", estateId.ToString());

                // Make the Http Call here
                HttpResponseMessage httpResponse = await this.HttpClient.SendAsync(request, cancellationToken);

                // Process the response
                return await ProcessResponse<List<TopBottomMerchantData>>(httpResponse, cancellationToken);
            }
            catch(Exception ex){
                // An exception has occurred, add some additional information to the message
                Exception exception = new Exception($"Error getting top/bottom sales by merchant for estate {estateId} TopOrBottom {topBottom} ans count {resultCount}.", ex);

                throw exception;
            }
        }

        public async Task<Result<List<TopBottomOperatorData>>> GetTopBottomOperatorData(String accessToken, Guid estateId, TopBottom topBottom, Int32 resultCount, CancellationToken cancellationToken){
            String requestUri = this.BuildRequestUrl($"/api/facts/transactions/operators/topbottombyvalue?topOrBottom={(Int32)topBottom}&count={resultCount}");

            try{
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Add("EstateId", estateId.ToString());

                // Make the Http Call here
                HttpResponseMessage httpResponse = await this.HttpClient.SendAsync(request, cancellationToken);

                // Process the response
                return await ProcessResponse<List<TopBottomOperatorData>>(httpResponse, cancellationToken);
            }
            catch(Exception ex){
                // An exception has occurred, add some additional information to the message
                Exception exception = new Exception($"Error getting top/bottom sales by operator for estate {estateId} TopOrBottom {topBottom} ans count {resultCount}.", ex);

                throw exception;
            }
        }

        public async Task<Result<List<TopBottomProductData>>> GetTopBottomProductData(String accessToken, Guid estateId, TopBottom topBottom, Int32 resultCount, CancellationToken cancellationToken){
            String requestUri = this.BuildRequestUrl($"/api/facts/transactions/products/topbottombyvalue?topOrBottom={(Int32)topBottom}&count={resultCount}");

            try{
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Add("EstateId", estateId.ToString());

                // Make the Http Call here
                HttpResponseMessage httpResponse = await this.HttpClient.SendAsync(request, cancellationToken);

                // Process the response
                return await ProcessResponse<List<TopBottomProductData>>(httpResponse, cancellationToken);
            }
            catch(Exception ex){
                // An exception has occurred, add some additional information to the message
                Exception exception = new Exception($"Error getting top/bottom sales by product for estate {estateId} TopOrBottom {topBottom} ans count {resultCount}.", ex);

                throw exception;
            }
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
