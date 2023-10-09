namespace EstateReportingAPI.Client{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using DataTransferObjects;
    using DataTrasferObjects;

    public interface IEstateReportingApiClient{
        #region Methods

        Task<List<CalendarDate>> GetCalendarDates(String accessToken, Guid estateId, Int32 year, CancellationToken cancellationToken);
        Task<List<CalendarYear>> GetCalendarYears(String accessToken, Guid estateId, CancellationToken cancellationToken);
        Task<List<ComparisonDate>> GetComparisonDates(String accessToken, Guid estateId, CancellationToken cancellationToken);
        Task<TodaysSales> GetTodaysSales(String accessToken, Guid estateId, DateTime comparisonDate, CancellationToken cancellationToken);
        Task<List<TodaysSalesCountByHour>> GetTodaysSalesCountByHour(String accessToken, Guid estateId, DateTime comparisonDate, CancellationToken cancellationToken);
        Task<List<TodaysSalesValueByHour>> GetTodaysSalesValueByHour(String accessToken, Guid estateId, DateTime comparisonDate, CancellationToken cancellationToken);
        Task<TodaysSettlement> GetTodaysSettlement(String accessToken, Guid estateId, DateTime comparisonDate, CancellationToken cancellationToken);
        Task<MerchantKpi> GetMerchantKpi(String accessToken, Guid estateId, CancellationToken cancellationToken);

        Task<TodaysSales> GetTodaysFailedSales(String accessToken, Guid estateId, String responseCode, DateTime comparisonDate, CancellationToken cancellationToken);

        Task<List<TopBottomOperatorData>> GetTopBottomOperatorData(String accessToken, Guid estateId, TopBottom topBottom, Int32 resultCount, CancellationToken cancellationToken);
        Task<List<TopBottomMerchantData>> GetTopBottomMerchantData(String accessToken, Guid estateId, TopBottom topBottom, Int32 resultCount, CancellationToken cancellationToken);
        Task<List<TopBottomProductData>> GetTopBottomProductData(String accessToken, Guid estateId, TopBottom topBottom, Int32 resultCount, CancellationToken cancellationToken);
        Task<List<Merchant>> GetMerchants(String accessToken, Guid estateId, CancellationToken cancellationToken);

        #endregion
    }
}