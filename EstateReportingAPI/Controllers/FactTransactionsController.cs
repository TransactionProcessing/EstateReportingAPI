using EstateReportingAPI.BusinessLogic.Queries;
using JasperFx.Core;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Results;
using Shared.Results.Web;
using System.Diagnostics.CodeAnalysis;

namespace EstateReportingAPI.Controllers{
    using BusinessLogic;
    using DataTransferObjects;
    using EstateReportingAPI.DataTrasferObjects;
    using EstateReportingAPI.Models;
    using Microsoft.AspNetCore.Authorization;
    using Newtonsoft.Json;
    using SimpleResults;
    using MerchantKpi = DataTransferObjects.MerchantKpi;
    using SortDirection = DataTransferObjects.SortDirection;
    using SortField = DataTransferObjects.SortField;
    using TodaysSales = DataTransferObjects.TodaysSales;
    using TodaysSalesCountByHour = DataTransferObjects.TodaysSalesCountByHour;
    using TodaysSalesValueByHour = DataTransferObjects.TodaysSalesValueByHour;
    using TransactionResult = DataTransferObjects.TransactionResult;
    using TransactionSearchRequest = DataTransferObjects.TransactionSearchRequest;

    [ExcludeFromCodeCoverage]
    [Route(FactTransactionsController.ControllerRoute)]
    [ApiController]
    [Authorize]
    public class FactTransactionsController : ControllerBase{
        #region Others

        /// <summary>
        /// The controller name
        /// </summary>
        private const String ControllerName = "transactions";

        /// <summary>
        /// The controller route
        /// </summary>
        private const String ControllerRoute = "api/facts/" + FactTransactionsController.ControllerName;

        #endregion


        private readonly IReportingManager ReportingManager;
        private readonly IMediator Mediator;

        public FactTransactionsController(IReportingManager reportingManager, IMediator mediator) {
            this.ReportingManager = reportingManager;
            this.Mediator = mediator;
        }

        private const String ConnectionStringIdentifier = "EstateReportingReadModel";

        [HttpGet]
        [Route("todayssales")]
        public async Task<IResult> TodaysSales([FromHeader] Guid estateId, [FromQuery] Int32 merchantReportingId, [FromQuery] Int32 operatorReportingId, [FromQuery] DateTime comparisonDate, CancellationToken cancellationToken) {
            TransactionQueries.TodaysSalesQuery query = new(estateId, merchantReportingId, operatorReportingId, comparisonDate);
            Result<Models.TodaysSales> result = await this.Mediator.Send(query, cancellationToken);
            
            return ResponseFactory.FromResult(result, (r) => {
                TodaysSales response = new TodaysSales
                {
                    ComparisonSalesCount = result.Data.ComparisonSalesCount,
                    ComparisonSalesValue = result.Data.ComparisonSalesValue,
                    TodaysSalesCount = result.Data.TodaysSalesCount,
                    TodaysSalesValue = result.Data.TodaysSalesValue,
                    ComparisonAverageSalesValue = result.Data.ComparisonAverageSalesValue,
                    TodaysAverageSalesValue = result.Data.TodaysAverageSalesValue,
                };

                return response;
            });
        }

        [HttpGet]
        [Route("todayssales/countbyhour")]
        public async Task<IResult> TodaysSalesCountByHour([FromHeader] Guid estateId,
                                                          [FromQuery] Int32 merchantReportingId,
                                                          [FromQuery] Int32 operatorReportingId,
                                                          [FromQuery] DateTime comparisonDate,
                                                          CancellationToken cancellationToken) {
            TransactionQueries.TodaysSalesCountByHour query = new TransactionQueries.TodaysSalesCountByHour(estateId, merchantReportingId, operatorReportingId, comparisonDate);
            var result = await this.Mediator.Send(query, cancellationToken);

            return ResponseFactory.FromResult(result, (r) => {
                List<TodaysSalesCountByHour> response = new List<TodaysSalesCountByHour>();

                foreach (Models.TodaysSalesCountByHour todaysSalesCountByHour in r) {
                    response.Add(new TodaysSalesCountByHour { ComparisonSalesCount = todaysSalesCountByHour.ComparisonSalesCount, Hour = todaysSalesCountByHour.Hour, TodaysSalesCount = todaysSalesCountByHour.TodaysSalesCount, });
                }

                return response;
            });
        }

        [HttpGet]
        [Route("todayssales/valuebyhour")]
        public async Task<IResult> TodaysSalesValueByHour([FromHeader] Guid estateId, [FromQuery] Int32 merchantReportingId, [FromQuery] Int32 operatorReportingId, [FromQuery] DateTime comparisonDate, CancellationToken cancellationToken){

            TransactionQueries.TodaysSalesValueByHour query = new TransactionQueries.TodaysSalesValueByHour(estateId, merchantReportingId, operatorReportingId, comparisonDate);
            var result = await this.Mediator.Send(query, cancellationToken);
            
            return ResponseFactory.FromResult(result, (r) => {
                List<TodaysSalesValueByHour> response = new List<TodaysSalesValueByHour>();

                foreach (Models.TodaysSalesValueByHour todaysSalesValueByHour in r)
                {
                    response.Add(new TodaysSalesValueByHour
                    {
                        ComparisonSalesValue = todaysSalesValueByHour.ComparisonSalesValue,
                        Hour = todaysSalesValueByHour.Hour,
                        TodaysSalesValue = todaysSalesValueByHour.TodaysSalesValue
                    });
                }

                return response;
            });
        }

        [HttpGet]
        [Route("merchants/lastsale")]
        public async Task<IResult> GetMerchantsByLastSale([FromHeader] Guid estateId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate, CancellationToken cancellationToken){
            MerchantQueries.GetByLastSaleQuery query = new(estateId, startDate, endDate);
            Result<List<Models.Merchant>> result = await this.Mediator.Send(query, cancellationToken);

            return ResponseFactory.FromResult(result, (r) => {
                List<DataTrasferObjects.Merchant> response = new();

                r.ForEach(m => response.Add(new DataTrasferObjects.Merchant
                {
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
                }));

                return response.OrderBy(m => m.Name);
            });
        }

        [HttpGet]
        [Route("merchantkpis")]
        public async Task<IResult> GetMerchantsTransactionKpis([FromHeader] Guid estateId, CancellationToken cancellationToken){

            MerchantQueries.GetTransactionKpisQuery query = new MerchantQueries.GetTransactionKpisQuery(estateId);
            var result = await this.Mediator.Send(query, cancellationToken);

            return ResponseFactory.FromResult(result, (r) => {
                MerchantKpi response = new MerchantKpi
                {
                    MerchantsWithNoSaleInLast7Days = result.Data.MerchantsWithNoSaleInLast7Days,
                    MerchantsWithNoSaleToday = result.Data.MerchantsWithNoSaleToday,
                    MerchantsWithSaleInLastHour = result.Data.MerchantsWithSaleInLastHour
                };

                return response;
            });
        }

        [HttpGet]
        [Route("todaysfailedsales")]
        public async Task<IResult> TodaysFailedSales([FromHeader] Guid estateId, [FromQuery] DateTime comparisonDate, [FromQuery] String responseCode, CancellationToken cancellationToken){
            TransactionQueries.TodaysFailedSales query = new(estateId, comparisonDate, responseCode);
            var result = await this.Mediator.Send(query, cancellationToken);

            return ResponseFactory.FromResult(result, (r) => {
                TodaysSales response = new TodaysSales
                {
                    ComparisonSalesCount = result.Data.ComparisonSalesCount,
                    ComparisonSalesValue = result.Data.ComparisonSalesValue,
                    TodaysSalesCount = result.Data.TodaysSalesCount,
                    TodaysSalesValue = result.Data.TodaysSalesValue,
                    ComparisonAverageSalesValue = result.Data.ComparisonAverageSalesValue,
                    TodaysAverageSalesValue = result.Data.TodaysAverageSalesValue,
                };

                return response;
            });
        }

        [HttpGet]
        [Route("products/topbottombyvalue")]
        public async Task<IResult> GetTopBottomProductsByValue([FromHeader] Guid estateId, [FromQuery] TopBottom topOrBottom, [FromQuery] Int32 count, CancellationToken cancellationToken){

            Models.TopBottom modelTopBottom = Enum.Parse<Models.TopBottom>(topOrBottom.ToString());

            IRequest<Result<List<TopBottomData>>> query = modelTopBottom switch
            {
                Models.TopBottom.Bottom => new ProductQueries.GetBottomProductsBySalesValueQuery(estateId, count),
                _ => new ProductQueries.GetTopProductsBySalesValueQuery(estateId, count)
            };
            Result<List<TopBottomData>> result = await this.Mediator.Send(query, cancellationToken);

            return ResponseFactory.FromResult(result, (r) => {
                List<TopBottomProductData> response = new List<TopBottomProductData>();
                r.ForEach(t => {
                    response.Add(new TopBottomProductData
                    {
                        ProductName = t.DimensionName,
                        SalesValue = t.SalesValue
                    });
                });

                return response;
            });
        }

        [HttpGet]
        [Route("merchants/topbottombyvalue")]
        public async Task<IResult> GetTopBottomMerchantsByValue([FromHeader] Guid estateId, [FromQuery] TopBottom topOrBottom, [FromQuery] Int32 count, CancellationToken cancellationToken){
            Models.TopBottom modelTopBottom = Enum.Parse<Models.TopBottom>(topOrBottom.ToString());

            IRequest<Result<List<TopBottomData>>> query = modelTopBottom switch {
                Models.TopBottom.Bottom => new MerchantQueries.GetBottomMerchantsBySalesValueQuery(estateId, count),
                _ => new MerchantQueries.GetTopMerchantsBySalesValueQuery(estateId, count)
            };
            Result<List<TopBottomData>> result = await this.Mediator.Send(query, cancellationToken);

            return ResponseFactory.FromResult(result, (r) => {
                List<TopBottomMerchantData> response = new List<TopBottomMerchantData>();
                r.ForEach(t => {
                    response.Add(new TopBottomMerchantData
                    {
                        MerchantName = t.DimensionName,
                        SalesValue = t.SalesValue
                    });
                });

                return response;
            });
        }

        [HttpGet]
        [Route("operators/topbottombyvalue")]
        public async Task<IResult> GetTopBottomOperatorsByValue([FromHeader] Guid estateId, [FromQuery] TopBottom topOrBottom, [FromQuery] Int32 count, CancellationToken cancellationToken){
            Models.TopBottom modelTopBottom = Enum.Parse<Models.TopBottom>(topOrBottom.ToString());

            IRequest<Result<List<TopBottomData>>> query = modelTopBottom switch
            {
                Models.TopBottom.Bottom => new OperatorQueries.GetBottomOperatorsBySalesValueQuery(estateId, count),
                _ => new OperatorQueries.GetTopOperatorsBySalesValueQuery(estateId, count)
            };
            Result<List<TopBottomData>> result = await this.Mediator.Send(query, cancellationToken);
            
            return ResponseFactory.FromResult(result, (r) => {
                List<TopBottomOperatorData> response = new List<TopBottomOperatorData>();
                r.ForEach(t => {
                    response.Add(new TopBottomOperatorData
                    {
                        OperatorName = t.DimensionName,
                        SalesValue = t.SalesValue
                    });
                });

                return response;
            });
        }

        [HttpGet]
        [Route("merchants/performance")]
        public async Task<IResult> GetMerchantPerformance([FromHeader] Guid estateId, [FromQuery] DateTime comparisonDate, [FromQuery] string? merchantReportingIds, CancellationToken cancellationToken){

            List<Int32> merchantIdFilter = new List<Int32>();
            if (String.IsNullOrEmpty(merchantReportingIds) == false){
                List<String> merchantListStrings = merchantReportingIds.Split(',').ToList();
                foreach (String merchantListString in merchantListStrings){
                    merchantIdFilter.Add(Int32.Parse(merchantListString));
                }
            }

            MerchantQueries.GetMerchantPerformanceQuery query = new(estateId, comparisonDate, merchantIdFilter);
            var result = await this.Mediator.Send(query, cancellationToken);
            return ResponseFactory.FromResult(result, (r) => {
                TodaysSales response = new TodaysSales
                {
                    ComparisonSalesCount = result.Data.ComparisonSalesCount,
                    ComparisonSalesValue = result.Data.ComparisonSalesValue,
                    TodaysSalesCount = result.Data.TodaysSalesCount,
                    TodaysSalesValue = result.Data.TodaysSalesValue,
                    ComparisonAverageSalesValue = result.Data.ComparisonAverageSalesValue,
                    TodaysAverageSalesValue = result.Data.TodaysAverageSalesValue,
                };

                return response;
            });
        }

        [HttpGet]
        [Route("products/performance")]
        public async Task<IResult> GetProductPerformance([FromHeader] Guid estateId, [FromQuery] DateTime comparisonDate, [FromQuery] string? productReportingIds, CancellationToken cancellationToken)
        {

            List<Int32> productIdFilter = new List<Int32>();
            if (String.IsNullOrEmpty(productReportingIds) == false)
            {
                List<String> productListStrings = productReportingIds.Split(',').ToList();
                foreach (String productListString in productListStrings)
                {
                    productIdFilter.Add(Int32.Parse(productListString));
                }
            }

            ProductQueries.GetProductPerformanceQuery query = new(estateId, comparisonDate, productIdFilter);
            var result = await this.Mediator.Send(query, cancellationToken);
            return ResponseFactory.FromResult(result, (r) => {
                TodaysSales response = new TodaysSales
                {
                    ComparisonSalesCount = result.Data.ComparisonSalesCount,
                    ComparisonSalesValue = result.Data.ComparisonSalesValue,
                    TodaysSalesCount = result.Data.TodaysSalesCount,
                    TodaysSalesValue = result.Data.TodaysSalesValue,
                    ComparisonAverageSalesValue = result.Data.ComparisonAverageSalesValue,
                    TodaysAverageSalesValue = result.Data.TodaysAverageSalesValue,
                };

                return response;
            });
        }

        [HttpGet]
        [Route("operators/performance")]
        public async Task<IResult> GetOperatorPerformance([FromHeader] Guid estateId, [FromQuery] DateTime comparisonDate, [FromQuery] string? operatorReportingIds, CancellationToken cancellationToken)
        {

            List<Int32> operatorIdFilter = new List<Int32>();
            if (String.IsNullOrEmpty(operatorReportingIds) == false)
            {
                List<String> productListStrings = operatorReportingIds.Split(',').ToList();
                foreach (String productListString in productListStrings)
                {
                    operatorIdFilter.Add(Int32.Parse(productListString));
                }
            }

            OperatorQueries.GetOperatorPerformanceQuery query = new(estateId, comparisonDate, operatorIdFilter);
            Result<Models.TodaysSales> result = await this.Mediator.Send(query, cancellationToken);
            return ResponseFactory.FromResult(result, (r) => {
                TodaysSales response = new TodaysSales
                {
                    ComparisonSalesCount = result.Data.ComparisonSalesCount,
                    ComparisonSalesValue = result.Data.ComparisonSalesValue,
                    TodaysSalesCount = result.Data.TodaysSalesCount,
                    TodaysSalesValue = result.Data.TodaysSalesValue,
                    ComparisonAverageSalesValue = result.Data.ComparisonAverageSalesValue,
                    TodaysAverageSalesValue = result.Data.TodaysAverageSalesValue,
                };

                return response;
            });
        }

        [HttpGet]
        [Route("search")]
        public async Task<IResult> TransactionSearch([FromHeader] Guid estateId, [FromBody] TransactionSearchRequest request,
                                                           [FromQuery] int? page, [FromQuery] int? pageSize,
                                                           [FromQuery] SortField? sortField, [FromQuery] SortDirection? sortDirection,
                                                           CancellationToken cancellationToken){

            PagingRequest pagingRequest = CreatePagingRequest(page, pageSize);
            Models.SortingRequest sortingRequest = CreateSortingRequest(sortField, sortDirection);
            // TODO: Convert the request
            Models.TransactionSearchRequest searchModel = new Models.TransactionSearchRequest{
                                                                                           AuthCode = request.AuthCode,
                                                                                           Merchants = request.Merchants,
                                                                                           Operators = request.Operators,
                                                                                           QueryDate = request.QueryDate,
                                                                                           ResponseCode = request.ResponseCode,
                                                                                           TransactionNumber = request.TransactionNumber,
                                                                                       };

            if (request.ValueRange != null){
                searchModel.ValueRange = new Models.ValueRange{
                                                                  EndValue = request.ValueRange.EndValue,
                                                                  StartValue = request.ValueRange.StartValue,
                                                              };
            }

            TransactionQueries.TransactionSearchQuery query = new(estateId, searchModel, pagingRequest, sortingRequest);
            var result = await this.Mediator.Send(query, cancellationToken);

            return ResponseFactory.FromResult(result, (r) => {
                List<TransactionResult> response = new List<TransactionResult>();

                r.ForEach(t => {
                    response.Add(new TransactionResult
                    {
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
                    });
                });

                return response;
            });
        }

        private static PagingRequest CreatePagingRequest(int? page, int? pageSize) => new(page, pageSize);

        private static SortingRequest CreateSortingRequest(SortField? sortField, SortDirection? sortDirection)
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
    }
    
    public enum TopBottom{
        Top = 0,

        Bottom = 1
    }

    
}
