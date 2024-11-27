using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EstateReportingAPI.BusinessLogic.Queries;
using EstateReportingAPI.Models;
using MediatR;
using SimpleResults;

namespace EstateReportingAPI.BusinessLogic.RequestHandlers
{
    public class CalendarRequestHandler : IRequestHandler<CalendarQueries.GetAllDatesQuery, Result<List<Calendar>>>,
        IRequestHandler<CalendarQueries.GetComparisonDatesQuery, Result<List<Calendar>>>,
        IRequestHandler<CalendarQueries.GetYearsQuery, Result<List<Int32>>> {
        private readonly IReportingManager Manager;
        public CalendarRequestHandler(IReportingManager manager) {
            this.Manager = manager;
        }
        public async Task<Result<List<Calendar>>> Handle(CalendarQueries.GetAllDatesQuery request,
                                                         CancellationToken cancellationToken) {
            List<Calendar> result = await this.Manager.GetCalendarDates(request.EstateId, cancellationToken);

            if (result.Any() == false) {
                return Result.NotFound("No calendar dates found");
            }

            return Result.Success(result);

        }

        public async Task<Result<List<Calendar>>> Handle(CalendarQueries.GetComparisonDatesQuery request,
                                                         CancellationToken cancellationToken) {
            List<Calendar> result = await this.Manager.GetCalendarComparisonDates(request.EstateId, cancellationToken);
            if (result.Any() == false)
            {
                return Result.NotFound("No calendar comparison dates found");
            }

            return Result.Success(result);
        }

        public async Task<Result<List<Int32>>> Handle(CalendarQueries.GetYearsQuery request,
                                                      CancellationToken cancellationToken) {
            List<Int32> result = await this.Manager.GetCalendarYears(request.EstateId, cancellationToken);
            if (result.Any() == false)
            {
                return Result.NotFound("No calendar years found");
            }

            return Result.Success(result);
        }
    }

    public class MerchantRequestHandler :IRequestHandler<MerchantQueries.GetMerchantsQuery, Result<List<Merchant>>>
    {
        private readonly IReportingManager Manager;
        public MerchantRequestHandler(IReportingManager manager)
        {
            this.Manager = manager;
        }
        public async Task<Result<List<Merchant>>> Handle(MerchantQueries.GetMerchantsQuery request,
                                                         CancellationToken cancellationToken) {
            List<Merchant> result = await this.Manager.GetMerchants(request.EstateId, cancellationToken);
            if (result.Any() == false)
            {
                return Result.NotFound("No merchants found");
            }

            return Result.Success(result);
        }
    }

    public class OperatorRequestHandler : IRequestHandler<OperatorQueries.GetOperatorsQuery, Result<List<Operator>>>
    {
        private readonly IReportingManager Manager;
        public OperatorRequestHandler(IReportingManager manager)
        {
            this.Manager = manager;
        }
        public async Task<Result<List<Operator>>> Handle(OperatorQueries.GetOperatorsQuery request,
                                                         CancellationToken cancellationToken)
        {
            List<Operator> result = await this.Manager.GetOperators(request.EstateId, cancellationToken);
            if (result.Any() == false)
            {
                return Result.NotFound("No operators found");
            }

            return Result.Success(result);
        }
    }

    public class ResponseCodeRequestHandler : IRequestHandler<ResponseCodeQueries.GetResponseCodesQuery, Result<List<ResponseCode>>> {
        private readonly IReportingManager Manager;

        public ResponseCodeRequestHandler(IReportingManager manager) {
            this.Manager = manager;
        }

        public async Task<Result<List<ResponseCode>>> Handle(ResponseCodeQueries.GetResponseCodesQuery request,
                                                             CancellationToken cancellationToken) {
            List<ResponseCode> result = await this.Manager.GetResponseCodes(request.EstateId, cancellationToken);
            if (result.Any() == false) {
                return Result.NotFound("No response codes found");
            }

            return Result.Success(result);
        }
    }

    public class SettlementRequestHandler : IRequestHandler<SettlementQueries.GetTodaysSettlementQuery, Result<TodaysSettlement>>,
        IRequestHandler<SettlementQueries.GetLastSettlementQuery, Result<LastSettlement>>,
        IRequestHandler<SettlementQueries.GetUnsettledFeesQuery, Result<List<UnsettledFee>>> {
        private readonly IReportingManager Manager;
        public SettlementRequestHandler(IReportingManager manager)
        {
            this.Manager = manager;
        }

        public async Task<Result<TodaysSettlement>> Handle(SettlementQueries.GetTodaysSettlementQuery request,
                                                           CancellationToken cancellationToken) {
            Models.TodaysSettlement model = await this.Manager.GetTodaysSettlement(request.EstateId, request.MerchantReportingId, request.OperatorReportingId, request.ComparisonDate, cancellationToken);

            return Result.Success(model);
        }

        public async Task<Result<LastSettlement>> Handle(SettlementQueries.GetLastSettlementQuery request,
                                                         CancellationToken cancellationToken)
        {
            LastSettlement model = await this.Manager.GetLastSettlement(request.EstateId, cancellationToken);

            return Result.Success(model);
        }

        public async Task<Result<List<UnsettledFee>>> Handle(SettlementQueries.GetUnsettledFeesQuery request,
                                                             CancellationToken cancellationToken) {
            List<UnsettledFee> model = await this.Manager.GetUnsettledFees(request.EstateId, request.StartDate, request.EndDate, request.MerchantIdFilter, request.OperatorIdFilter, request.ProductIdFilter, request.GroupByOption, cancellationToken);
            return Result.Success(model);
        }
    }
}
