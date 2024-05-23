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
    Task<List<Merchant>> GetMerchantsByLastSale(Guid estateId, DateTime startDateTime, DateTime endDateTime, CancellationToken cancellationToken);
    Task<List<Operator>> GetOperators(Guid estateId, CancellationToken cancellationToken);
    Task<List<ResponseCode>> GetResponseCodes(Guid estateId, CancellationToken cancellationToken);
    Task<TodaysSales> GetTodaysFailedSales(Guid estateId, DateTime comparisonDate, String responseCode, CancellationToken cancellationToken);
    Task<TodaysSales> GetTodaysSales(Guid estateId, Guid? merchantId, Guid? operatorId, DateTime comparisonDate, CancellationToken cancellationToken);
    Task<List<TodaysSalesCountByHour>> GetTodaysSalesCountByHour(Guid estateId, Guid? merchantId, Guid? operatorId, DateTime comparisonDate, CancellationToken cancellationToken);
    Task<List<TodaysSalesValueByHour>> GetTodaysSalesValueByHour(Guid estateId, Guid? merchantId, Guid? operatorId, DateTime comparisonDate, CancellationToken cancellationToken);
    Task<TodaysSettlement> GetTodaysSettlement(Guid estateId, Guid? merchantId, Guid? operatorId, DateTime comparisonDate, CancellationToken cancellationToken);
    Task<List<TopBottomData>> GetTopBottomData(Guid estateId, TopBottom direction, Int32 resultCount, Dimension dimension, CancellationToken cancellationToken);

    Task<TodaysSales> GetMerchantPerformance(Guid estateId, DateTime comparisonDate, List<Int32> merchantIds,CancellationToken cancellationToken);
    Task<TodaysSales> GetProductPerformance(Guid estateId, DateTime comparisonDate, List<Int32> productIds, CancellationToken cancellationToken);

    Task<TodaysSales> GetOperatorPerformance(Guid estateId, DateTime comparisonDate, List<Int32> operatorIds, CancellationToken cancellationToken);

    Task<List<TransactionResult>> TransactionSearch(Guid estateId, TransactionSearchRequest searchRequest, PagingRequest pagingRequest, SortingRequest sortingRequest, CancellationToken cancellationToken);

    #endregion
}