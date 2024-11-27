namespace EstateReportingAPI.Client{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using DataTransferObjects;
    using DataTrasferObjects;
    using SimpleResults;

    public interface IEstateReportingApiClient{
        #region Methods

        Task<Result<List<CalendarDate>>> GetCalendarDates(String accessToken, Guid estateId, Int32 year, CancellationToken cancellationToken);
        Task<Result<List<CalendarYear>>> GetCalendarYears(String accessToken, Guid estateId, CancellationToken cancellationToken);
        Task<Result<List<ComparisonDate>>> GetComparisonDates(String accessToken, Guid estateId, CancellationToken cancellationToken);
        Task<Result<LastSettlement>>  GetLastSettlement(String accessToken, Guid estateId, CancellationToken cancellationToken);
        Task<MerchantKpi> GetMerchantKpi(String accessToken, Guid estateId, CancellationToken cancellationToken);
        Task<TodaysSales> GetMerchantPerformance(String accessToken, Guid estateId, DateTime comparisonDate, List<Int32> merchantReportingIds, CancellationToken cancellationToken);
        Task<Result<List<Merchant>>> GetMerchants(String accessToken, Guid estateId, CancellationToken cancellationToken);

        Task<List<Merchant>> GetMerchantsByLastSaleDate(String accessToken, Guid estateId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken);

        Task<TodaysSales> GetOperatorPerformance(String accessToken, Guid estateId, DateTime comparisonDate, List<Int32> operatorReportingIds, CancellationToken cancellationToken);
        Task<Result<List<Operator>>> GetOperators(String accessToken, Guid estateId, CancellationToken cancellationToken);

        Task<TodaysSales> GetProductPerformance(String accessToken, Guid estateId, DateTime comparisonDate, List<Int32> productReportingIds, CancellationToken cancellationToken);

        Task<Result<List<ResponseCode>>> GetResponseCodes(String accessToken, Guid estateId, CancellationToken cancellationToken);

        Task<TodaysSales> GetTodaysFailedSales(String accessToken, Guid estateId, Int32 merchantReportingId, Int32 operatorReportingId, String responseCode, DateTime comparisonDate, CancellationToken cancellationToken);
        Task<TodaysSales> GetTodaysSales(String accessToken, Guid estateId, Int32 merchantReportingId, Int32 operatorReportingId, DateTime comparisonDate, CancellationToken cancellationToken);
        Task<List<TodaysSalesCountByHour>> GetTodaysSalesCountByHour(String accessToken, Guid estateId, Int32 merchantReportingId, Int32 operatorReportingId, DateTime comparisonDate, CancellationToken cancellationToken);
        Task<List<TodaysSalesValueByHour>> GetTodaysSalesValueByHour(String accessToken, Guid estateId, Int32 merchantReportingId, Int32 operatorReportingId, DateTime comparisonDate, CancellationToken cancellationToken);
        Task<Result<TodaysSettlement>> GetTodaysSettlement(String accessToken, Guid estateId, Int32 merchantReportingId, Int32 operatorReportingId, DateTime comparisonDate, CancellationToken cancellationToken);
        Task<List<TopBottomMerchantData>> GetTopBottomMerchantData(String accessToken, Guid estateId, TopBottom topBottom, Int32 resultCount, CancellationToken cancellationToken);

        Task<List<TopBottomOperatorData>> GetTopBottomOperatorData(String accessToken, Guid estateId, TopBottom topBottom, Int32 resultCount, CancellationToken cancellationToken);
        Task<List<TopBottomProductData>> GetTopBottomProductData(String accessToken, Guid estateId, TopBottom topBottom, Int32 resultCount, CancellationToken cancellationToken);

        Task<List<TransactionResult>> TransactionSearch(String accessToken, Guid estateId, TransactionSearchRequest searchRequest, Int32? page, Int32? pageSize, SortField? sortField, SortDirection? sortDirection, CancellationToken cancellationToken);
        Task<Result<List<UnsettledFee>>> GetUnsettledFees(String accessToken, Guid estateId, DateTime startDate, DateTime endDate, List<Int32> merchantIds, List<Int32> operatorIds, List<Int32> productIds, GroupByOption groupBy, CancellationToken cancellationToken);

        #endregion
    }
}