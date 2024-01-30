using System;
using System.Collections.Generic;
using System.Text;

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

        public async Task<TodaysSales> GetMerchantPerformance(String accessToken, Guid estateId, DateTime comparisonDate, List<Int32> merchantIds, CancellationToken cancellationToken){
            
            // Serialize the integer array into a comma-separated string
            string serializedArray = string.Join(",", merchantIds);
            
            TodaysSales response = null;

            String requestUri = this.BuildRequestUrl($"/api/facts/transactions/merchants/performance?comparisonDate={comparisonDate.Date:yyyy-MM-dd}&merchantIds={serializedArray}");

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

        public async Task<TodaysSales> GetProductPerformance(String accessToken, Guid estateId, DateTime comparisonDate, List<Int32> productIds, CancellationToken cancellationToken){
            // Serialize the integer array into a comma-separated string
            string serializedArray = string.Join(",", productIds);

            TodaysSales response = null;

            String requestUri = this.BuildRequestUrl($"/api/facts/transactions/products/performance?comparisonDate={comparisonDate.Date:yyyy-MM-dd}&productIds={serializedArray}");

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

        public async Task<TodaysSales> GetOperatorPerformance(String accessToken, Guid estateId, DateTime comparisonDate, List<String> operatorIds, CancellationToken cancellationToken){
            // Serialize the integer array into a comma-separated string
            string serializedArray = string.Join(",", operatorIds);

            TodaysSales response = null;

            String requestUri = this.BuildRequestUrl($"/api/facts/transactions/operators/performance?comparisonDate={comparisonDate.Date:yyyy-MM-dd}&operatorIds={serializedArray}");

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

        public async Task<TodaysSales> GetTodaysFailedSales(String accessToken, Guid estateId, Guid? merchantId, Guid? operatorId, String responseCode, DateTime comparisonDate, CancellationToken cancellationToken){
            TodaysSales response = null;


            QueryStringBuilder builder = new QueryStringBuilder();
            builder.AddParameter("comparisonDate", $"{comparisonDate.Date:yyyy-MM-dd}");
            builder.AddParameter("merchantId", merchantId);
            builder.AddParameter("operatorId", operatorId);
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

        public async Task<TodaysSales> GetTodaysSales(String accessToken, Guid estateId, Guid? merchantId, Guid? operatorId, DateTime comparisonDate, CancellationToken cancellationToken){
            TodaysSales response = null;

            QueryStringBuilder builder = new QueryStringBuilder();
            builder.AddParameter("comparisonDate", $"{comparisonDate.Date:yyyy-MM-dd}");
            builder.AddParameter("merchantId", merchantId);
            builder.AddParameter("operatorId", operatorId);

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

        public async Task<List<TodaysSalesCountByHour>> GetTodaysSalesCountByHour(String accessToken, Guid estateId, Guid? merchantId, Guid? operatorId, DateTime comparisonDate, CancellationToken cancellationToken){
            List<TodaysSalesCountByHour> response = null;


            QueryStringBuilder builder = new QueryStringBuilder();
            builder.AddParameter("comparisonDate", $"{comparisonDate.Date:yyyy-MM-dd}");
            builder.AddParameter("merchantId", merchantId);
            builder.AddParameter("operatorId", operatorId);

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

        public async Task<List<TodaysSalesValueByHour>> GetTodaysSalesValueByHour(String accessToken, Guid estateId, Guid? merchantId, Guid? operatorId, DateTime comparisonDate, CancellationToken cancellationToken){
            List<TodaysSalesValueByHour> response = null;


            QueryStringBuilder builder = new QueryStringBuilder();
            builder.AddParameter("comparisonDate", $"{comparisonDate.Date:yyyy-MM-dd}");
            builder.AddParameter("merchantId", merchantId);
            builder.AddParameter("operatorId", operatorId);

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

        public async Task<TodaysSettlement> GetTodaysSettlement(String accessToken, Guid estateId, Guid? merchantId, Guid? operatorId, DateTime comparisonDate, CancellationToken cancellationToken){
            TodaysSettlement response = null;
            
            QueryStringBuilder builder = new QueryStringBuilder();
            builder.AddParameter("comparisonDate", $"{comparisonDate.Date:yyyy-MM-dd}");
            builder.AddParameter("merchantId", merchantId);
            builder.AddParameter("operatorId", operatorId);

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
    private Dictionary<string, object> parameters = new Dictionary<String, Object>();

    public QueryStringBuilder AddParameter(string key, Object value)
    {
        this.parameters.Add(key, value);
        return this;
    }

    static Dictionary<string, object> FilterDictionary(Dictionary<string, object> inputDictionary)
    {
        Dictionary<string, object> result = new Dictionary<string, object>();

        foreach (KeyValuePair<String, Object> entry in inputDictionary)
        {
            if (entry.Value != null && !IsDefaultValue(entry.Value))
            {
                result.Add(entry.Key, entry.Value);
            }
        }

        return result;
    }

    static bool IsDefaultValue<T>(T value)
    {
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
