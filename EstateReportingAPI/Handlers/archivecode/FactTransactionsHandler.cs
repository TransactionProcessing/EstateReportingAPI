using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.DataTransferObjects;
using EstateReportingAPI.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Results.Web;
using SimpleResults;
using GroupByOption = EstateReportingAPI.DataTransferObjects.GroupByOption;
using LastSettlement = EstateReportingAPI.DataTransferObjects.LastSettlement;
using Merchant = EstateReportingAPI.DataTransferObjects.Merchant;
using MerchantKpi = EstateReportingAPI.DataTransferObjects.MerchantKpi;
using Operator = EstateReportingAPI.DataTransferObjects.Operator;
using ResponseCode = EstateReportingAPI.DataTrasferObjects.ResponseCode;
using SortDirection = EstateReportingAPI.DataTransferObjects.SortDirection;
using SortField = EstateReportingAPI.DataTransferObjects.SortField;
using TodaysSales = EstateReportingAPI.DataTransferObjects.TodaysSales;
using TodaysSalesCountByHour = EstateReportingAPI.DataTransferObjects.TodaysSalesCountByHour;
using TodaysSalesValueByHour = EstateReportingAPI.DataTransferObjects.TodaysSalesValueByHour;
using TodaysSettlement = EstateReportingAPI.DataTransferObjects.TodaysSettlement;
using TopBottom = EstateReportingAPI.DataTransferObjects.TopBottom;
using TransactionResult = EstateReportingAPI.DataTransferObjects.TransactionResult;
using TransactionSearchRequest = EstateReportingAPI.DataTransferObjects.TransactionSearchRequest;

/*namespace EstateReportingAPI.Handlers
{
    public static class FactTransactionsHandler {
        public static async Task<IResult> TodaysSales([FromHeader] Guid estateId,
                                                      [FromQuery] int? merchantReportingId,
                                                      [FromQuery] int? operatorReportingId,
                                                      [FromQuery] DateTime comparisonDate,
                                                      IMediator mediator,
                                                      CancellationToken cancellationToken) {
            var query = new TransactionQueries.TodaysSalesQuery(estateId, merchantReportingId.GetValueOrDefault(), operatorReportingId.GetValueOrDefault(), comparisonDate);
            var result = await mediator.Send(query, cancellationToken);

            return ResponseFactory.FromResult(result, r => new TodaysSales {
                ComparisonSalesCount = r.ComparisonSalesCount,
                ComparisonSalesValue = r.ComparisonSalesValue,
                TodaysSalesCount = r.TodaysSalesCount,
                TodaysSalesValue = r.TodaysSalesValue,
                ComparisonAverageSalesValue = r.ComparisonAverageSalesValue,
                TodaysAverageSalesValue = r.TodaysAverageSalesValue,
            });
        }

        public static async Task<IResult> TodaysSalesCountByHour([FromHeader] Guid estateId,
                                                                 [FromQuery] int? merchantReportingId,
                                                                 [FromQuery] int? operatorReportingId,
                                                                 [FromQuery] DateTime comparisonDate,
                                                                 IMediator mediator,
                                                                 CancellationToken cancellationToken) {
            var query = new TransactionQueries.TodaysSalesCountByHour(estateId, merchantReportingId.GetValueOrDefault(), operatorReportingId.GetValueOrDefault(), comparisonDate);
            var result = await mediator.Send(query, cancellationToken);

            return ResponseFactory.FromResult(result, r => {
                var response = r.Select(t => new TodaysSalesCountByHour { ComparisonSalesCount = t.ComparisonSalesCount, Hour = t.Hour, TodaysSalesCount = t.TodaysSalesCount }).ToList();

                return response;
            });
        }

        public static async Task<IResult> TodaysSalesValueByHour([FromHeader] Guid estateId,
                                                                 [FromQuery] int? merchantReportingId,
                                                                 [FromQuery] int? operatorReportingId,
                                                                 [FromQuery] DateTime comparisonDate,
                                                                 IMediator mediator,
                                                                 CancellationToken cancellationToken) {
            var query = new TransactionQueries.TodaysSalesValueByHour(estateId, merchantReportingId.GetValueOrDefault(), operatorReportingId.GetValueOrDefault(), comparisonDate);
            var result = await mediator.Send(query, cancellationToken);

            return ResponseFactory.FromResult(result, r => {
                var response = r.Select(t => new TodaysSalesValueByHour { ComparisonSalesValue = t.ComparisonSalesValue, Hour = t.Hour, TodaysSalesValue = t.TodaysSalesValue }).ToList();

                return response;
            });
        }

        public static async Task<IResult> GetMerchantsByLastSale([FromHeader] Guid estateId,
                                                                 [FromQuery] DateTime startDate,
                                                                 [FromQuery] DateTime endDate,
                                                                 IMediator mediator,
                                                                 CancellationToken cancellationToken) {
            var query = new MerchantQueries.GetByLastSaleQuery(estateId, startDate, endDate);
            var result = await mediator.Send(query, cancellationToken);

            return ResponseFactory.FromResult(result, r => {
                var response = r.Select(m => new DataTrasferObjects.Merchant {
                    MerchantReportingId = m.MerchantReportingId,
                    MerchantId = m.MerchantId,
                    EstateReportingId = m.EstateReportingId,
                    Name = m.Name,
                    LastSaleDateTime = m.LastSaleDateTime,
                    CreatedDateTime = m.CreatedDateTime,
                    LastSale = m.LastSale,
                    LastStatement = m.LastStatement,
                    PostCode = m.PostCode,
                    Reference = m.Reference,
                    Region = m.Region,
                    Town = m.Town,
                }).OrderBy(x => x.Name).ToList();

                return response;
            });
        }

        public static async Task<IResult> GetMerchantsTransactionKpis([FromHeader] Guid estateId,
                                                                      IMediator mediator,
                                                                      CancellationToken cancellationToken) {
            var query = new MerchantQueries.GetTransactionKpisQuery(estateId);
            var result = await mediator.Send(query, cancellationToken);

            return ResponseFactory.FromResult(result, r => new MerchantKpi { MerchantsWithNoSaleInLast7Days = r.MerchantsWithNoSaleInLast7Days, MerchantsWithNoSaleToday = r.MerchantsWithNoSaleToday, MerchantsWithSaleInLastHour = r.MerchantsWithSaleInLastHour });
        }

        public static async Task<IResult> TodaysFailedSales([FromHeader] Guid estateId,
                                                            [FromQuery] DateTime comparisonDate,
                                                            [FromQuery] string responseCode,
                                                            IMediator mediator,
                                                            CancellationToken cancellationToken) {
            var query = new TransactionQueries.TodaysFailedSales(estateId, comparisonDate, responseCode);
            var result = await mediator.Send(query, cancellationToken);

            return ResponseFactory.FromResult(result, r => new TodaysSales {
                ComparisonSalesCount = r.ComparisonSalesCount,
                ComparisonSalesValue = r.ComparisonSalesValue,
                TodaysSalesCount = r.TodaysSalesCount,
                TodaysSalesValue = r.TodaysSalesValue,
                ComparisonAverageSalesValue = r.ComparisonAverageSalesValue,
                TodaysAverageSalesValue = r.TodaysAverageSalesValue,
            });
        }

        public static async Task<IResult> GetTopBottomProductsByValue([FromHeader] Guid estateId,
                                                                      [FromQuery] TopBottom topOrBottom,
                                                                      [FromQuery] int count,
                                                                      IMediator mediator,
                                                                      CancellationToken cancellationToken) {
            var modelTopBottom = Enum.Parse<Models.TopBottom>(topOrBottom.ToString());

            IRequest<Result<List<TopBottomData>>> query = modelTopBottom switch {
                Models.TopBottom.Bottom => new ProductQueries.GetBottomProductsBySalesValueQuery(estateId, count),
                _ => new ProductQueries.GetTopProductsBySalesValueQuery(estateId, count)
            };

            var result = await mediator.Send(query, cancellationToken);

            return ResponseFactory.FromResult(result, r => {
                var response = r.Select(t => new TopBottomProductData { ProductName = t.DimensionName, SalesValue = t.SalesValue }).ToList();

                return response;
            });
        }

        public static async Task<IResult> GetTopBottomMerchantsByValue([FromHeader] Guid estateId,
                                                                       [FromQuery] TopBottom topOrBottom,
                                                                       [FromQuery] int count,
                                                                       IMediator mediator,
                                                                       CancellationToken cancellationToken) {
            var modelTopBottom = Enum.Parse<Models.TopBottom>(topOrBottom.ToString());

            IRequest<Result<List<TopBottomData>>> query = modelTopBottom switch {
                Models.TopBottom.Bottom => new MerchantQueries.GetBottomMerchantsBySalesValueQuery(estateId, count),
                _ => new MerchantQueries.GetTopMerchantsBySalesValueQuery(estateId, count)
            };

            var result = await mediator.Send(query, cancellationToken);

            return ResponseFactory.FromResult(result, r => {
                var response = r.Select(t => new TopBottomMerchantData { MerchantName = t.DimensionName, SalesValue = t.SalesValue }).ToList();

                return response;
            });
        }

        public static async Task<IResult> GetTopBottomOperatorsByValue([FromHeader] Guid estateId,
                                                                       [FromQuery] TopBottom topOrBottom,
                                                                       [FromQuery] int count,
                                                                       IMediator mediator,
                                                                       CancellationToken cancellationToken) {
            var modelTopBottom = Enum.Parse<Models.TopBottom>(topOrBottom.ToString());

            IRequest<Result<List<TopBottomData>>> query = modelTopBottom switch {
                Models.TopBottom.Bottom => new OperatorQueries.GetBottomOperatorsBySalesValueQuery(estateId, count),
                _ => new OperatorQueries.GetTopOperatorsBySalesValueQuery(estateId, count)
            };

            var result = await mediator.Send(query, cancellationToken);

            return ResponseFactory.FromResult(result, r => {
                var response = r.Select(t => new TopBottomOperatorData { OperatorName = t.DimensionName, SalesValue = t.SalesValue }).ToList();

                return response;
            });
        }

        public static async Task<IResult> GetMerchantPerformance([FromHeader] Guid estateId,
                                                                 [FromQuery] DateTime comparisonDate,
                                                                 [FromQuery] string? merchantReportingIds,
                                                                 IMediator mediator,
                                                                 CancellationToken cancellationToken) {
            var merchantIdFilter = ParseCsvIds(merchantReportingIds);

            var query = new MerchantQueries.GetMerchantPerformanceQuery(estateId, comparisonDate, merchantIdFilter);
            var result = await mediator.Send(query, cancellationToken);

            return ResponseFactory.FromResult(result, r => new TodaysSales {
                ComparisonSalesCount = r.ComparisonSalesCount,
                ComparisonSalesValue = r.ComparisonSalesValue,
                TodaysSalesCount = r.TodaysSalesCount,
                TodaysSalesValue = r.TodaysSalesValue,
                ComparisonAverageSalesValue = r.ComparisonAverageSalesValue,
                TodaysAverageSalesValue = r.TodaysAverageSalesValue,
            });
        }

        public static async Task<IResult> GetProductPerformance([FromHeader] Guid estateId,
                                                                [FromQuery] DateTime comparisonDate,
                                                                [FromQuery] string? productReportingIds,
                                                                IMediator mediator,
                                                                CancellationToken cancellationToken) {
            var productIdFilter = ParseCsvIds(productReportingIds);

            var query = new ProductQueries.GetProductPerformanceQuery(estateId, comparisonDate, productIdFilter);
            var result = await mediator.Send(query, cancellationToken);

            return ResponseFactory.FromResult(result, r => new TodaysSales {
                ComparisonSalesCount = r.ComparisonSalesCount,
                ComparisonSalesValue = r.ComparisonSalesValue,
                TodaysSalesCount = r.TodaysSalesCount,
                TodaysSalesValue = r.TodaysSalesValue,
                ComparisonAverageSalesValue = r.ComparisonAverageSalesValue,
                TodaysAverageSalesValue = r.TodaysAverageSalesValue,
            });
        }

        public static async Task<IResult> GetOperatorPerformance([FromHeader] Guid estateId,
                                                                 [FromQuery] DateTime comparisonDate,
                                                                 [FromQuery] string? operatorReportingIds,
                                                                 IMediator mediator,
                                                                 CancellationToken cancellationToken) {
            var operatorIdFilter = ParseCsvIds(operatorReportingIds);

            var query = new OperatorQueries.GetOperatorPerformanceQuery(estateId, comparisonDate, operatorIdFilter);
            var result = await mediator.Send(query, cancellationToken);

            return ResponseFactory.FromResult(result, r => new TodaysSales {
                ComparisonSalesCount = r.ComparisonSalesCount,
                ComparisonSalesValue = r.ComparisonSalesValue,
                TodaysSalesCount = r.TodaysSalesCount,
                TodaysSalesValue = r.TodaysSalesValue,
                ComparisonAverageSalesValue = r.ComparisonAverageSalesValue,
                TodaysAverageSalesValue = r.TodaysAverageSalesValue,
            });
        }

        public static async Task<IResult> TransactionSearch([FromHeader] Guid estateId,
                                                            [FromBody] TransactionSearchRequest request,
                                                            [FromQuery] int? page,
                                                            [FromQuery] int? pageSize,
                                                            [FromQuery] SortField? sortField,
                                                            [FromQuery] SortDirection? sortDirection,
                                                            IMediator mediator,
                                                            CancellationToken cancellationToken) {
            var paging = CreatePagingRequest(page, pageSize);
            var sorting = CreateSortingRequest(sortField, sortDirection);

            var searchModel = new Models.TransactionSearchRequest {
                AuthCode = request.AuthCode,
                Merchants = request.Merchants,
                Operators = request.Operators,
                QueryDate = request.QueryDate,
                ResponseCode = request.ResponseCode,
                TransactionNumber = request.TransactionNumber,
                ValueRange = request.ValueRange == null ? null : new Models.ValueRange { StartValue = request.ValueRange.StartValue, EndValue = request.ValueRange.EndValue }
            };

            var query = new TransactionQueries.TransactionSearchQuery(estateId, searchModel, paging, sorting);
            var result = await mediator.Send(query, cancellationToken);

            return ResponseFactory.FromResult(result, r => {
                var response = r.Select(t => new TransactionResult {
                    MerchantReportingId = t.MerchantReportingId,
                    ResponseCode = t.ResponseCode,
                    Product = t.Product,
                    TransactionReportingId = t.TransactionReportingId,
                    TransactionSource = t.TransactionSource,
                    IsAuthorised = t.IsAuthorised,
                    MerchantName = t.MerchantName,
                    OperatorName = t.OperatorName,
                    OperatorReportingId = t.OperatorReportingId,
                    ProductReportingId = t.ProductReportingId,
                    ResponseMessage = t.ResponseMessage,
                    TransactionDateTime = t.TransactionDateTime,
                    TransactionId = t.TransactionId,
                    TransactionAmount = t.TransactionAmount
                }).ToList();

                return response;
            });
        }

        private static PagingRequest CreatePagingRequest(int? page, int? pageSize) => new PagingRequest(page, pageSize);

        private static Models.SortingRequest CreateSortingRequest(SortField? sortField, SortDirection? sortDirection)
        {
            Models.SortField modelSortField = Models.SortField.TransactionAmount;
            Models.SortDirection modelSortDirection = Models.SortDirection.Ascending;

            if (sortField != null)
            {
                modelSortField = Enum.Parse<Models.SortField>(sortField.ToString(), true);
            }
            if (sortDirection != null)
            {
                modelSortDirection = Enum.Parse<Models.SortDirection>(sortDirection.ToString(), true);
            }

            return new Models.SortingRequest(modelSortField, modelSortDirection);
        }

        private static List<int> ParseCsvIds(string? csv)
        {
            if (string.IsNullOrWhiteSpace(csv))
                return new List<int>();

            return csv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => int.TryParse(s, out _))
                .Select(int.Parse)
                .ToList();
        }
    }

}
*/