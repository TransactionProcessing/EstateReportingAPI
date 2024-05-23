using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;

namespace EstateReportingAPI.Controllers{
    using DataTransferObjects;
    using BusinessLogic;
    using Microsoft.AspNetCore.Authorization;
    using Newtonsoft.Json;
    using EstateReportingAPI.Models;
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
        public const String ControllerName = "transactions";

        /// <summary>
        /// The controller route
        /// </summary>
        private const String ControllerRoute = "api/facts/" + FactTransactionsController.ControllerName;

        #endregion


        private readonly IReportingManager ReportingManager;

        public FactTransactionsController(IReportingManager reportingManager){
            this.ReportingManager = reportingManager;
        }

        private const String ConnectionStringIdentifier = "EstateReportingReadModel";

        [HttpGet]
        [Route("todayssales")]
        public async Task<IActionResult> TodaysSales([FromHeader] Guid estateId, [FromQuery] Guid? merchantId, [FromQuery] Guid? operatorId, [FromQuery] DateTime comparisonDate, CancellationToken cancellationToken){
            Models.TodaysSales model = await this.ReportingManager.GetTodaysSales(estateId, merchantId, operatorId, comparisonDate, cancellationToken);

            TodaysSales response = new TodaysSales{
                                                      ComparisonSalesCount = model.ComparisonSalesCount,
                                                      ComparisonSalesValue = model.ComparisonSalesValue,
                                                      TodaysSalesCount = model.TodaysSalesCount,
                                                      TodaysSalesValue = model.TodaysSalesValue,
                                                      ComparisonAverageSalesValue = model.ComparisonAverageSalesValue,
                                                      TodaysAverageSalesValue = model.TodaysAverageSalesValue,
                                                  };

            return this.Ok(response);
        }

        [HttpGet]
        [Route("todayssales/countbyhour")]
        public async Task<IActionResult> TodaysSalesCountByHour([FromHeader] Guid estateId, Guid? merchantId, [FromQuery] Guid? operatorId, [FromQuery] DateTime comparisonDate, CancellationToken cancellationToken){
            List<Models.TodaysSalesCountByHour> models = await this.ReportingManager.GetTodaysSalesCountByHour(estateId, merchantId, operatorId, comparisonDate, cancellationToken);

            List<TodaysSalesCountByHour> response = new List<TodaysSalesCountByHour>();

            foreach (Models.TodaysSalesCountByHour todaysSalesCountByHour in models){
                response.Add(new TodaysSalesCountByHour{
                                                           ComparisonSalesCount = todaysSalesCountByHour.ComparisonSalesCount,
                                                           Hour = todaysSalesCountByHour.Hour,
                                                           TodaysSalesCount = todaysSalesCountByHour.TodaysSalesCount,
                                                       });
            }

            return this.Ok(response);
        }

        [HttpGet]
        [Route("todayssales/valuebyhour")]
        public async Task<IActionResult> TodaysSalesValueByHour([FromHeader] Guid estateId, Guid? merchantId, [FromQuery] Guid? operatorId, [FromQuery] DateTime comparisonDate, CancellationToken cancellationToken){


            List<Models.TodaysSalesValueByHour> models = await this.ReportingManager.GetTodaysSalesValueByHour(estateId, merchantId, operatorId, comparisonDate, cancellationToken);

            List<TodaysSalesValueByHour> response = new List<TodaysSalesValueByHour>();

            foreach (Models.TodaysSalesValueByHour todaysSalesValueByHour in models){
                response.Add(new TodaysSalesValueByHour{
                                                           ComparisonSalesValue = todaysSalesValueByHour.ComparisonSalesValue,
                                                           Hour = todaysSalesValueByHour.Hour,
                                                           TodaysSalesValue = todaysSalesValueByHour.TodaysSalesValue
                                                       });
            }

            return this.Ok(response);
        }

        [HttpGet]
        [Route("merchants/lastsale")]
        public async Task<IActionResult> GetMerchantsByLastSale([FromHeader] Guid estateId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate, CancellationToken cancellationToken){
            List<Merchant> merchants = await this.ReportingManager.GetMerchantsByLastSale(estateId,startDate, endDate, cancellationToken);

            List<Merchant> response = new List<Merchant>();

            merchants.ForEach(m => response.Add(new Merchant
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

            return this.Ok(response.OrderBy(m => m.Name));
        }

        [HttpGet]
        [Route("merchantkpis")]
        public async Task<IActionResult> GetMerchantsTransactionKpis([FromHeader] Guid estateId, CancellationToken cancellationToken){

            Models.MerchantKpi model = await this.ReportingManager.GetMerchantsTransactionKpis(estateId, cancellationToken);

            MerchantKpi response = new MerchantKpi{
                                                      MerchantsWithNoSaleInLast7Days = model.MerchantsWithNoSaleInLast7Days,
                                                      MerchantsWithNoSaleToday = model.MerchantsWithNoSaleToday,
                                                      MerchantsWithSaleInLastHour = model.MerchantsWithSaleInLastHour
                                                  };

            return this.Ok(response);
        }

        [HttpGet]
        [Route("todaysfailedsales")]
        public async Task<IActionResult> TodaysFailedSales([FromHeader] Guid estateId, Guid? merchantId, [FromQuery] Guid? operatorId, [FromQuery] DateTime comparisonDate, [FromQuery] String responseCode, CancellationToken cancellationToken){
            Models.TodaysSales model = await this.ReportingManager.GetTodaysFailedSales(estateId, comparisonDate, responseCode, cancellationToken);

            TodaysSales response = new TodaysSales{
                                                      ComparisonSalesCount = model.ComparisonSalesCount,
                                                      ComparisonSalesValue = model.ComparisonSalesValue,
                                                      TodaysSalesCount = model.TodaysSalesCount,
                                                      TodaysSalesValue = model.TodaysSalesValue
                                                  };

            return this.Ok(response);
        }

        [HttpGet]
        [Route("products/topbottombyvalue")]
        public async Task<IActionResult> GetTopBottomProductsByValue([FromHeader] Guid estateId, [FromQuery] TopBottom topOrBottom, [FromQuery] Int32 count, CancellationToken cancellationToken){

            Models.TopBottom modelTopBottom = Enum.Parse<Models.TopBottom>(topOrBottom.ToString());

            List<Models.TopBottomData> topbottomData = await this.ReportingManager.GetTopBottomData(estateId, modelTopBottom, count, Models.Dimension.Product, cancellationToken);

            List<TopBottomProductData> response = new List<TopBottomProductData>();
            topbottomData.ForEach(t => {
                                      response.Add(new TopBottomProductData{
                                                                               ProductName = t.DimensionName,
                                                                               SalesValue = t.SalesValue
                                                                           });
                                  });
            return this.Ok(response);
        }

        [HttpGet]
        [Route("merchants/topbottombyvalue")]
        public async Task<IActionResult> GetTopBottomMerchantsByValue([FromHeader] Guid estateId, [FromQuery] TopBottom topOrBottom, [FromQuery] Int32 count, CancellationToken cancellationToken){
            Models.TopBottom modelTopBottom = Enum.Parse<Models.TopBottom>(topOrBottom.ToString());

            List<Models.TopBottomData> topbottomData = await this.ReportingManager.GetTopBottomData(estateId, modelTopBottom, count, Models.Dimension.Merchant, cancellationToken);


            List<TopBottomMerchantData> response = new List<TopBottomMerchantData>();
            topbottomData.ForEach(t => {
                                      response.Add(new TopBottomMerchantData{
                                                                                MerchantName = t.DimensionName,
                                                                                SalesValue = t.SalesValue
                                                                            });
                                  });
            return this.Ok(response);
        }

        [HttpGet]
        [Route("operators/topbottombyvalue")]
        public async Task<IActionResult> GetTopBottomOperatorsByValue([FromHeader] Guid estateId, [FromQuery] TopBottom topOrBottom, [FromQuery] Int32 count, CancellationToken cancellationToken){
            Models.TopBottom modelTopBottom = Enum.Parse<Models.TopBottom>(topOrBottom.ToString());

            List<Models.TopBottomData> topbottomData = await this.ReportingManager.GetTopBottomData(estateId, modelTopBottom, count, Models.Dimension.Operator, cancellationToken);


            List<TopBottomOperatorData> response = new List<TopBottomOperatorData>();
            topbottomData.ForEach(t => {
                                      response.Add(new TopBottomOperatorData{
                                                                                OperatorName = t.DimensionName,
                                                                                SalesValue = t.SalesValue
                                                                            });
                                  });
            return this.Ok(response);
        }

        [HttpGet]
        [Route("merchants/performance")]
        public async Task<IActionResult> GetMerchantPerformance([FromHeader] Guid estateId, [FromQuery] DateTime comparisonDate, [FromQuery] string? merchantIds, CancellationToken cancellationToken){

            List<Int32> merchantIdFilter = new List<Int32>();
            if (String.IsNullOrEmpty(merchantIds) == false){
                List<String> merchantListStrings = merchantIds.Split(',').ToList();
                foreach (String merchantListString in merchantListStrings){
                    merchantIdFilter.Add(Int32.Parse(merchantListString));
                }
            }

            Models.TodaysSales model = await this.ReportingManager.GetMerchantPerformance(estateId, comparisonDate, merchantIdFilter, cancellationToken);

            TodaysSales response = new TodaysSales
            {
                ComparisonSalesCount = model.ComparisonSalesCount,
                ComparisonSalesValue = model.ComparisonSalesValue,
                TodaysSalesCount = model.TodaysSalesCount,
                TodaysSalesValue = model.TodaysSalesValue,
                ComparisonAverageSalesValue = model.ComparisonAverageSalesValue,
                TodaysAverageSalesValue = model.TodaysAverageSalesValue,
            };

            return this.Ok(response);
        }

        [HttpGet]
        [Route("products/performance")]
        public async Task<IActionResult> GetProductPerformance([FromHeader] Guid estateId, [FromQuery] DateTime comparisonDate, [FromQuery] string? productIds, CancellationToken cancellationToken)
        {

            List<Int32> productIdFilter = new List<Int32>();
            if (String.IsNullOrEmpty(productIds) == false)
            {
                List<String> productListStrings = productIds.Split(',').ToList();
                foreach (String productListString in productListStrings)
                {
                    productIdFilter.Add(Int32.Parse(productListString));
                }
            }

            Models.TodaysSales model = await this.ReportingManager.GetProductPerformance(estateId, comparisonDate, productIdFilter, cancellationToken);

            TodaysSales response = new TodaysSales
                                   {
                                       ComparisonSalesCount = model.ComparisonSalesCount,
                                       ComparisonSalesValue = model.ComparisonSalesValue,
                                       TodaysSalesCount = model.TodaysSalesCount,
                                       TodaysSalesValue = model.TodaysSalesValue,
                                       ComparisonAverageSalesValue = model.ComparisonAverageSalesValue,
                                       TodaysAverageSalesValue = model.TodaysAverageSalesValue,
                                   };

            return this.Ok(response);
        }

        [HttpGet]
        [Route("operators/performance")]
        public async Task<IActionResult> GetOperatorPerformance([FromHeader] Guid estateId, [FromQuery] DateTime comparisonDate, [FromQuery] string? operatorIds, CancellationToken cancellationToken)
        {

            List<Int32> operatorIdFilter = new List<Int32>();
            if (String.IsNullOrEmpty(operatorIds) == false)
            {
                List<String> productListStrings = operatorIds.Split(',').ToList();
                foreach (String productListString in productListStrings)
                {
                    operatorIdFilter.Add(Int32.Parse(productListString));
                }
            }
            
            Models.TodaysSales model = await this.ReportingManager.GetOperatorPerformance(estateId, comparisonDate, operatorIdFilter, cancellationToken);

            TodaysSales response = new TodaysSales
                                   {
                                       ComparisonSalesCount = model.ComparisonSalesCount,
                                       ComparisonSalesValue = model.ComparisonSalesValue,
                                       TodaysSalesCount = model.TodaysSalesCount,
                                       TodaysSalesValue = model.TodaysSalesValue,
                                       ComparisonAverageSalesValue = model.ComparisonAverageSalesValue,
                                       TodaysAverageSalesValue = model.TodaysAverageSalesValue,
                                   };

            return this.Ok(response);
        }

        [HttpGet]
        [Route("search")]
        public async Task<IActionResult> TransactionSearch([FromHeader] Guid estateId, [FromBody] TransactionSearchRequest request,
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
            
            List<Models.TransactionResult> result = await this.ReportingManager.TransactionSearch(estateId, searchModel, pagingRequest, sortingRequest, cancellationToken);
            
            List<TransactionResult> response = new List<TransactionResult>();

            result.ForEach(t => {
                               response.Add(new TransactionResult{
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


            return this.Ok(response);
        }

        private static PagingRequest CreatePagingRequest(int? page, int? pageSize) => new(page, pageSize);

        private static SortingRequest CreateSortingRequest(SortField? sortField, SortDirection? sortDirection){
            Models.SortField modelSortField = Models.SortField.TransactionAmount;
            Models.SortDirection modelSortDirection = Models.SortDirection.Ascending;
            if (sortField != null){
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
