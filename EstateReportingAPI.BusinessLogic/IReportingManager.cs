namespace EstateReportingAPI.BusinessLogic;

using Models;

public interface IReportingManager{
    #region Methods

    Task<List<Calendar>> GetCalendarComparisonDates(Guid estateId, CancellationToken cancellationToken);
    Task<List<Calendar>> GetCalendarDates(Guid estateId, CancellationToken cancellationToken);
    Task<List<Int32>> GetCalendarYears(Guid estateId, CancellationToken cancellationToken);
    Task<LastSettlement> GetLastSettlement(Guid estateId, CancellationToken cancellationToken);
    Task<List<Merchant>> GetMerchants(Guid estateId, CancellationToken cancellationToken);
    Task<MerchantKpi> GetMerchantsTransactionKpis(Guid estateId, CancellationToken cancellationToken);
    Task<TodaysSales> GetTodaysFailedSales(Guid estateId, DateTime comparisonDate, String responseCode, CancellationToken cancellationToken);
    Task<TodaysSales> GetTodaysSales(Guid estateId, DateTime comparisonDate, CancellationToken cancellationToken);
    Task<List<TodaysSalesCountByHour>> GetTodaysSalesCountByHour(Guid estateId, DateTime comparisonDate, CancellationToken cancellationToken);
    Task<List<TodaysSalesValueByHour>> GetTodaysSalesValueByHour(Guid estateId, DateTime comparisonDate, CancellationToken cancellationToken);
    Task<TodaysSettlement> GetTodaysSettlement(Guid estateId, DateTime comparisonDate, CancellationToken cancellationToken);
    Task<List<TopBottomData>> GetTopBottomData(Guid estateId, TopBottom direction, Int32 resultCount, Dimension dimension, CancellationToken cancellationToken);

    Task<List<Merchant>> GetMerchants(Guid estateId, CancellationToken cancellationToken);

    Task<List<Operator>> GetOperators(Guid estateId, CancellationToken cancellationToken);

    #endregion
}