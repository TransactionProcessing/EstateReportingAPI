using EstateReportingAPI.BusinessLogic.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.DynamicLinq;
using SimpleResults;
using System;
using System.Linq;
using System.Threading;
using TransactionProcessor.Database.Contexts;
using TransactionProcessor.Database.Entities;
using TransactionProcessor.Database.Entities.Summary;
using Shared.EntityFramework;
using Shared.Results;
using EstateReportingAPI.Models;
using Contract = EstateReportingAPI.Models.Contract;
using Merchant = EstateReportingAPI.Models.Merchant;
using MerchantContract = EstateReportingAPI.Models.MerchantContract;
using MerchantDevice = EstateReportingAPI.Models.MerchantDevice;
using MerchantOperator = EstateReportingAPI.Models.MerchantOperator;
using Operator = EstateReportingAPI.Models.Operator;
using ContractProductTransactionFee = EstateReportingAPI.Models.ContractProductTransactionFee;
using MerchantBalanceProjectionState = TransactionProcessor.ProjectionEngine.Database.Database.Entities.MerchantBalanceProjectionState;

namespace EstateReportingAPI.BusinessLogic.Services;

public interface IMerchantReportingService
{
    Task<Result<List<Contract>>> GetRecentContracts(ContractQueries.GetRecentContractsQuery request, CancellationToken cancellationToken);
    Task<Result<List<Contract>>> GetContracts(ContractQueries.GetContractsQuery request, CancellationToken cancellationToken);
    Task<Result<Contract>> GetContract(ContractQueries.GetContractQuery request, CancellationToken cancellationToken);
    Task<Result<List<Merchant>>> GetRecentMerchants(MerchantQueries.GetRecentMerchantsQuery request, CancellationToken cancellationToken);
    Task<Result<MerchantKpi>> GetMerchantsTransactionKpis(MerchantQueries.GetTransactionKpisQuery request, CancellationToken cancellationToken);
    Task<Result<List<Operator>>> GetOperators(OperatorQueries.GetOperatorsQuery request, CancellationToken cancellationToken);
    Task<Result<List<MerchantOpeningHour>>> GetMerchantOpeningHours(MerchantQueries.GetMerchantOpeningHoursQuery request, CancellationToken cancellationToken);
    Task<Result<Operator>> GetOperator(OperatorQueries.GetOperatorQuery request, CancellationToken cancellationToken);
    Task<Result<List<Merchant>>> GetMerchants(MerchantQueries.GetMerchantsQuery request, CancellationToken cancellationToken);
    Task<Result<Merchant>> GetMerchant(MerchantQueries.GetMerchantQuery request, CancellationToken cancellationToken);
    Task<Result<List<MerchantOperator>>> GetMerchantOperators(MerchantQueries.GetMerchantOperatorsQuery request, CancellationToken cancellationToken);
    Task<Result<List<MerchantContract>>> GetMerchantContracts(MerchantQueries.GetMerchantContractsQuery request, CancellationToken cancellationToken);
    Task<Result<List<MerchantDevice>>> GetMerchantDevices(MerchantQueries.GetMerchantDevicesQuery request, CancellationToken cancellationToken);
    Task<Result<MerchantScheduleResponse>> GetMerchantSchedule(MerchantQueries.GetMerchantScheduleQuery request, CancellationToken cancellationToken);
}
public sealed class MerchantReportingService : IMerchantReportingService
{
    private readonly IDbContextResolver<EstateManagementContext> Resolver;
    private const string EstateManagementDatabaseName = "TransactionProcessorReadModel";

    public MerchantReportingService(IDbContextResolver<EstateManagementContext> resolver)
    {
        Resolver = resolver;
    }

    private static async Task<Result<T>> ExecuteQuerySafeSum<T>(IQueryable query,
                                                                CancellationToken cancellationToken,
                                                                string contextMessage = null) {
        try {
            T item = await query.SumAsync(cancellationToken);
            return Result.Success(item);
        }
        catch (Exception ex) {
            string msg = contextMessage == null ? $"Error executing query: {ex.Message}" : $"{contextMessage}: {ex.Message}";
            return Result.Failure(msg);
        }
    }

    private static async Task<Result<List<T>>> ExecuteQuerySafeToList<T>(IQueryable<T> query,
                                                                         CancellationToken cancellationToken,
                                                                         string contextMessage = null) {
        try {
            List<T> items = await query.ToListAsync(cancellationToken);
            return Result.Success(items);
        }
        catch (Exception ex) {
            string msg = contextMessage == null ? $"Error executing query: {ex.Message}" : $"{contextMessage}: {ex.Message}";
            return Result.Failure(msg);
        }
    }

    private static async Task<Result<int>> ExecuteQuerySafeCount(IQueryable query,
                                                                 CancellationToken cancellationToken,
                                                                 string contextMessage = null) {
        try {
            int count = await query.CountAsync(cancellationToken);
            return Result.Success(count);
        }
        catch (Exception ex) {
            string msg = contextMessage == null ? $"Error executing query: {ex.Message}" : $"{contextMessage}: {ex.Message}";
            return Result.Failure(msg);
        }
    }

    private static async Task<Result<T>> ExecuteQuerySafeSingleOrDefault<T>(IQueryable<T> query,
                                                                            CancellationToken cancellationToken,
                                                                            string contextMessage = null) {
        try {
            T item = await query.SingleOrDefaultAsync(cancellationToken);

            if (EqualityComparer<T>.Default.Equals(item, default(T)))
                return Result.NotFound(contextMessage);

            return Result.Success(item);
        }
        catch (Exception ex) {
            string msg = contextMessage == null ? $"Error executing query: {ex.Message}" : $"{contextMessage}: {ex.Message}";
            return Result.Failure(msg);
        }
    }

    public async Task<Result<List<Contract>>> GetContracts(ContractQueries.GetContractsQuery request,
                                                           CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        var baseContractsQuery = (from c in context.Contracts
            join o in context.Operators on c.OperatorId equals o.OperatorId into ops
            from o in ops
            join e in context.Estates on c.EstateId equals e.EstateId into estates
            from e in estates
            select new ContractBaseData {
                EstateReportingId = e.EstateReportingId,
                ContractId = c.ContractId,
                ContractReportingId = c.ContractReportingId,
                Description = c.Description,
                EstateId = c.EstateId,
                OperatorId = c.OperatorId,
                OperatorReportingId = o.OperatorReportingId,
                OperatorName = o.Name
            }).OrderByDescending(x => x.Description);

        var baseContractsResult = await ExecuteQuerySafeToList(baseContractsQuery, cancellationToken, "Error retrieving contracts - Step 1");
        if (baseContractsResult.IsFailed)
            return ResultHelpers.CreateFailure(baseContractsResult);

        var baseContracts = baseContractsResult.Data;
        if (!baseContracts.Any())
            return new List<Contract>();

        var contractIds = baseContracts.Select(b => b.ContractId).ToList();
        var productsResult = await LoadProductsAsync(context, contractIds, cancellationToken, "Error retrieving contracts - Step 2");
        if (productsResult.IsFailed)
            return ResultHelpers.CreateFailure(productsResult);

        var productIds = productsResult.Data.Select(p => p.ContractProductId).ToList();
        var feesResult = await LoadFeesAsync(context, productIds, cancellationToken, "Error retrieving contracts - Step 3");
        if (feesResult.IsFailed)
            return ResultHelpers.CreateFailure(feesResult);

        return Result.Success(BuildContracts(baseContracts, productsResult.Data, feesResult.Data));
    }

    public async Task<Result<Contract>> GetContract(ContractQueries.GetContractQuery request,
                                                    CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        // Step 1: load contract with operator name
        var baseContractQuery = (from c in context.Contracts
        join o in context.Operators on c.OperatorId equals o.OperatorId into ops
        from o in ops
        join e in context.Estates on c.EstateId equals e.EstateId into estates
        from e in estates
        where c.ContractId == request.ContractId
        select new {
            e.EstateReportingId,
            c.ContractId,
            c.ContractReportingId,
            c.Description,
            c.EstateId,
            c.OperatorId,
            o.OperatorReportingId,
            OperatorName = o.Name }).OrderByDescending(x => x.Description);

        var baseContractQueryResult = await ExecuteQuerySafeSingleOrDefault(baseContractQuery, cancellationToken, "Error retrieving contract - Step 1");

        if (baseContractQueryResult.IsFailed)
            return ResultHelpers.CreateFailure(baseContractQueryResult);

        var baseContract = baseContractQueryResult.Data;

        // Steps 2 & 3: load products and fees, then assemble
        var productsResult = await this.LoadProductsWithFees(context, baseContract.ContractId, cancellationToken);

        if (productsResult.IsFailed)
            return ResultHelpers.CreateFailure(productsResult);

        return Result.Success(new Contract {
            EstateReportingId = baseContract.EstateReportingId,
            ContractId = baseContract.ContractId,
            ContractReportingId = baseContract.ContractReportingId,
            Description = baseContract.Description,
            EstateId = baseContract.EstateId,
            OperatorName = baseContract.OperatorName,
            OperatorId = baseContract.OperatorId,
            OperatorReportingId = baseContract.OperatorReportingId,
            Products = productsResult.Data
        });
    }

    private async Task<Result<List<Models.ContractProduct>>> LoadProductsWithFees(EstateManagementContext context,
                                                                                  Guid contractId,
                                                                                  CancellationToken cancellationToken) {
        var productsQuery = context.ContractProducts.Where(cp => cp.ContractId == contractId).Select(cp => new {
            cp.ContractProductId,
            cp.ContractProductReportingId,
            cp.ContractId,
            cp.DisplayText,
            cp.ProductName,
            cp.ProductType,
            cp.Value
        });

        var productsQueryResult = await ExecuteQuerySafeToList(productsQuery, cancellationToken, "Error retrieving contract - Step 2");

        if (productsQueryResult.IsFailed)
            return ResultHelpers.CreateFailure(productsQueryResult);

        var products = productsQueryResult.Data;
        var productIds = products.Select(p => p.ContractProductId).ToList();

        var feesQuery = context.ContractProductTransactionFees.Where(tf => productIds.Contains(tf.ContractProductId)).Select(tf => new {
            tf.CalculationType,
            tf.ContractProductTransactionFeeId,
            tf.ContractProductTransactionFeeReportingId,
            tf.FeeType,
            tf.Value,
            tf.ContractProductId,
            tf.Description
        });

        var feesQueryResult = await ExecuteQuerySafeToList(feesQuery, cancellationToken, "Error retrieving contract - Step 3");

        if (feesQueryResult.IsFailed)
            return ResultHelpers.CreateFailure(feesQueryResult);

        var fees = feesQueryResult.Data;
        var feesLookup = fees.ToLookup(f => f.ContractProductId);

        return Result.Success(products.Select(p => new Models.ContractProduct {
            ContractId = p.ContractId,
            ProductId = p.ContractProductId,
            DisplayText = p.DisplayText,
            ProductName = p.ProductName,
            ProductType = p.ProductType,
            Value = p.Value,
            ContractProductReportingId = p.ContractProductReportingId,
            TransactionFees = feesLookup[p.ContractProductId].Select(f => new ContractProductTransactionFee {
                Description = f.Description,
                Value = f.Value,
                CalculationType = f.CalculationType,
                FeeType = f.FeeType,
                TransactionFeeId = f.ContractProductTransactionFeeId,
                ContractProductTransactionFeeReportingId = f.ContractProductTransactionFeeReportingId
            }).ToList()
        }).ToList());
    }

    public async Task<Result<List<Contract>>> GetRecentContracts(ContractQueries.GetRecentContractsQuery request,
                                                                 CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        var contractsQuery = (from c in context.Contracts
            join o in context.Operators on c.OperatorId equals o.OperatorId into ops
            from o in ops
            join e in context.Estates on c.EstateId equals e.EstateId into estates
            from e in estates
            select new
            {
                e.EstateReportingId,
                c.ContractId,
                c.ContractReportingId,
                c.Description,
                c.EstateId,
                c.OperatorId,
                o.OperatorReportingId,
                OperatorName = o.Name
            }).OrderByDescending(x => x.ContractReportingId).Take(3);

        var result = await ExecuteQuerySafeToList(contractsQuery, cancellationToken, "Error retrieving recent contracts");

        if (result.IsFailed)
            return ResultHelpers.CreateFailure(result);

        List<Contract> response = new List<Contract>();
        foreach (var contract in result.Data) {
            response.Add(new Contract {
                EstateId = contract.EstateId,
                EstateReportingId = contract.EstateReportingId,
                ContractId = contract.ContractId,
                ContractReportingId = contract.ContractReportingId,
                Description = contract.Description,
                OperatorName = contract.OperatorName,
                OperatorId = contract.OperatorId,
                OperatorReportingId = contract.OperatorReportingId,
            });
        }

        return response;
    }


    public async Task<Result<List<Merchant>>> GetRecentMerchants(MerchantQueries.GetRecentMerchantsQuery request,
                                                                 CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        var merchantsQuery = context.Merchants.Select(m => new {
            MerchantReportingId = m.MerchantReportingId,
            Name = m.Name,
            CreatedDateTime = m.CreatedDateTime,
            MerchantId = m.MerchantId,
            Reference = m.Reference,
            AddressInfo = context.MerchantAddresses.Where(ma => ma.MerchantId == m.MerchantId)
                .OrderByDescending(ma => ma.CreatedDateTime)
                .Select(ma => new { ma.AddressId, ma.AddressLine1, ma.AddressLine2, ma.Country, ma.PostalCode, ma.Region, ma.Town })
                .FirstOrDefault(),
            ContactInfo = context.MerchantContacts.Where(mc => mc.MerchantId == m.MerchantId)
                .OrderByDescending(mc => mc.CreatedDateTime)
                .Select(mc => new { mc.ContactId, mc.Name, mc.EmailAddress, mc.PhoneNumber })
                .FirstOrDefault(),
            BalanceState = context.MerchantBalanceProjectionState.Where(mb => mb.MerchantId == m.MerchantId).SingleOrDefault()
        }).OrderByDescending(m => m.CreatedDateTime).Take(3);

        var recentMerchantsResult = await ExecuteQuerySafeToList(merchantsQuery, cancellationToken, "Error retrieving recent merchants");
        if (recentMerchantsResult.IsFailed)
            return ResultHelpers.CreateFailure(recentMerchantsResult);

        List<Merchant> merchantList = recentMerchantsResult.Data.Select(merchant => {
            Merchant model = new() {
                MerchantId = merchant.MerchantId,
                Name = merchant.Name,
                Reference = merchant.Reference,
                MerchantReportingId = merchant.MerchantReportingId,
                CreatedDateTime = merchant.CreatedDateTime,
                Balance = merchant.BalanceState.Balance
            };
            if (merchant.AddressInfo != null) {
                model.AddressId = merchant.AddressInfo.AddressId;
                model.AddressLine1 = merchant.AddressInfo.AddressLine1;
                model.AddressLine2 = merchant.AddressInfo.AddressLine2;
                model.Country = merchant.AddressInfo.Country;
                model.PostCode = merchant.AddressInfo.PostalCode;
                model.Town = merchant.AddressInfo.Town;
                model.Region = merchant.AddressInfo.Region;
            }
            if (merchant.ContactInfo != null) {
                model.ContactId = merchant.ContactInfo.ContactId;

                model.ContactName = merchant.ContactInfo.Name;
                model.ContactEmail = merchant.ContactInfo.EmailAddress;
                model.ContactPhone = merchant.ContactInfo.PhoneNumber;
            }
            return model;
        }).ToList();

        return Result.Success(merchantList);
    }

    public async Task<Result<MerchantKpi>> GetMerchantsTransactionKpis(MerchantQueries.GetTransactionKpisQuery request,
                                                                       CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        var merchantsQuery = context.MerchantBalanceProjectionState.Select(m => new { m.MerchantName, m.LastSale });
        var merchantsQueryResult = await ExecuteQuerySafeToList(merchantsQuery, cancellationToken, "Error retrieving merchants for KPI's");
        if (merchantsQueryResult.IsFailed)
            return ResultHelpers.CreateFailure(merchantsQueryResult);

        var merchants = merchantsQueryResult.Data;

        DateTime now = DateTime.Now;

        Int32 merchantsWithSaleInLastHour = (from m in merchants where m.LastSale >= now.AddHours(-1) && m.LastSale <= now select m).Count();

        Int32 merchantsWithNoSaleToday = (from m in merchants where m.LastSale >= now.AddDays(-7) && m.LastSale <= now.AddDays(-1) select m).Count();

        Int32 merchantsWithNoSaleInLast7Days = (from m in merchants where m.LastSale <= now.AddDays(-7) select m).Count();

        MerchantKpi response = new() { MerchantsWithSaleInLastHour = merchantsWithSaleInLastHour, MerchantsWithNoSaleToday = merchantsWithNoSaleToday, MerchantsWithNoSaleInLast7Days = merchantsWithNoSaleInLast7Days };

        return Result.Success(response);
    }

    public async Task<Result<List<Operator>>> GetOperators(OperatorQueries.GetOperatorsQuery request,
                                                           CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        var operatorQuery = (from o in context.Operators
        select new {
            Name = o.Name,
            EstateReportingId = context.Estates.Single(e => e.EstateId == o.EstateId).EstateReportingId,
            OperatorId = o.OperatorId,
            OperatorReportingId = o.OperatorReportingId,
            RequireCustomMerchantNumber = o.RequireCustomMerchantNumber,
            RequireCustomTerminalNumber = o.RequireCustomTerminalNumber
        });

        var operatorResult = await ExecuteQuerySafeToList(operatorQuery, cancellationToken, "Error retrieving operator");

        if (operatorResult.IsFailed)
            return ResultHelpers.CreateFailure(operatorResult);

        List<Operator> operators = new List<Operator>();
        foreach (var op in operatorResult.Data) {
            operators.Add(new Operator {
                Name = op.Name,
                EstateReportingId = op.EstateReportingId,
                OperatorId = op.OperatorId,
                OperatorReportingId = op.OperatorReportingId,
                RequireCustomMerchantNumber = op.RequireCustomMerchantNumber,
                RequireCustomTerminalNumber = op.RequireCustomTerminalNumber
            });
        }

        return Result.Success(operators);
    }

    public async Task<Result<List<MerchantOpeningHour>>> GetMerchantOpeningHours(MerchantQueries.GetMerchantOpeningHoursQuery request,
                                                                                 CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        var openingHoursQuery = (from mo in context.MerchantOpeningHours
        where mo.MerchantId == request.MerchantId
        select new {
            SundayOpen = mo.SundayOpening,
            SundayClose = mo.SundayClosing,
            MondayOpen = mo.MondayOpening,
            MondayClose = mo.MondayClosing,
            TuesdayOpen = mo.TuesdayOpening,
            TuesdayClose = mo.TuesdayClosing,
            WednesdayOpen = mo.WednesdayOpening,
            WednesdayClose = mo.WednesdayClosing,
            ThursdayOpen = mo.ThursdayOpening,
            ThursdayClose = mo.ThursdayClosing,
            FridayOpen = mo.FridayOpening,
            FridayClose = mo.FridayClosing,
            SaturdayOpen = mo.SaturdayOpening,
            SaturdayClose = mo.SaturdayClosing,
        });

        var openingHoursResult = await ExecuteQuerySafeSingleOrDefault(openingHoursQuery, cancellationToken, "Error retrieving merchant opening hours");

        if (openingHoursResult.IsFailed)
            return ResultHelpers.CreateFailure(openingHoursResult);

        List<MerchantOpeningHour> openingHours = new List<MerchantOpeningHour>();
        openingHours.Add(new MerchantOpeningHour {OpeningTime = openingHoursResult.Data.SundayOpen, ClosingTime = openingHoursResult.Data.SundayClose, DayOfWeek = DayOfWeek.Sunday, MerchantId = request.MerchantId});
        openingHours.Add(new MerchantOpeningHour { OpeningTime = openingHoursResult.Data.MondayOpen, ClosingTime = openingHoursResult.Data.MondayClose, DayOfWeek = DayOfWeek.Monday, MerchantId = request.MerchantId });
        openingHours.Add(new MerchantOpeningHour { OpeningTime = openingHoursResult.Data.TuesdayOpen, ClosingTime = openingHoursResult.Data.TuesdayClose, DayOfWeek = DayOfWeek.Tuesday, MerchantId = request.MerchantId });
        openingHours.Add(new MerchantOpeningHour { OpeningTime = openingHoursResult.Data.WednesdayOpen, ClosingTime = openingHoursResult.Data.WednesdayClose, DayOfWeek = DayOfWeek.Wednesday, MerchantId = request.MerchantId });
        openingHours.Add(new MerchantOpeningHour { OpeningTime = openingHoursResult.Data.ThursdayOpen, ClosingTime = openingHoursResult.Data.ThursdayClose, DayOfWeek = DayOfWeek.Thursday, MerchantId = request.MerchantId });
        openingHours.Add(new MerchantOpeningHour { OpeningTime = openingHoursResult.Data.FridayOpen, ClosingTime = openingHoursResult.Data.FridayClose, DayOfWeek = DayOfWeek.Friday, MerchantId = request.MerchantId });
        openingHours.Add(new MerchantOpeningHour { OpeningTime = openingHoursResult.Data.SaturdayOpen, ClosingTime = openingHoursResult.Data.SaturdayClose, DayOfWeek = DayOfWeek.Saturday, MerchantId = request.MerchantId });

        return Result.Success(openingHours);
    }

    public async Task<Result<Operator>> GetOperator(OperatorQueries.GetOperatorQuery request,
                                                    CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        var operatorQuery = (from o in context.Operators
        where o.OperatorId == request.OperatorId
        select new {
            Name = o.Name,
            EstateReportingId = context.Estates.Single(e => e.EstateId == o.EstateId).EstateReportingId,
            OperatorId = o.OperatorId,
            OperatorReportingId = o.OperatorReportingId,
            RequireCustomMerchantNumber = o.RequireCustomMerchantNumber,
            RequireCustomTerminalNumber = o.RequireCustomTerminalNumber
        });

        var operatorResult = await ExecuteQuerySafeSingleOrDefault(operatorQuery, cancellationToken, "Error retrieving operator");

        if (operatorResult.IsFailed)
            return ResultHelpers.CreateFailure(operatorResult);

        var @operator = new Operator {
            Name = operatorResult.Data.Name,
            EstateReportingId = operatorResult.Data.EstateReportingId,
            OperatorId = operatorResult.Data.OperatorId,
            OperatorReportingId = operatorResult.Data.OperatorReportingId,
            RequireCustomMerchantNumber = operatorResult.Data.RequireCustomMerchantNumber,
            RequireCustomTerminalNumber = operatorResult.Data.RequireCustomTerminalNumber
        };

        return Result.Success(@operator);
    }

    public async Task<Result<List<Merchant>>> GetMerchants(MerchantQueries.GetMerchantsQuery request,
                                                           CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        var merchantWithAddresses = ApplyMerchantFilters(BuildMerchantWithAddressQuery(context, request.EstateId), request.QueryOptions);

        var queryResults = await ExecuteQuerySafeToList(merchantWithAddresses, cancellationToken, "Error retrieving merchants");

        if (queryResults.IsFailed)
            return ResultHelpers.CreateFailure(queryResults);

        var merchants = queryResults.Data;

        var merchantBalancesQuery  = context.MerchantBalanceProjectionState.Where(mb => merchants.Select(m => m.Merchant.MerchantId).Contains(mb.MerchantId));

        var balanceQueryResults = await ExecuteQuerySafeToList(merchantBalancesQuery, cancellationToken, "Error retrieving merchant balances");

        if (balanceQueryResults.IsFailed)
            return ResultHelpers.CreateFailure(balanceQueryResults);

        try {
            Dictionary<Guid, decimal> balanceLookup = BuildMerchantBalanceLookup(balanceQueryResults.Data);
            List<Merchant> response = merchants.Select(merchant =>
                                                           ModelFactory.ConvertFrom(merchant, GetMerchantBalance(balanceLookup, merchant.Merchant.MerchantId)))
                                               .ToList();

            return Result.Success(response);
        }
        catch (InvalidOperationException ex) {
            return Result.Failure(ex.Message);
        }
    }

    public async Task<Result<Merchant>> GetMerchant(MerchantQueries.GetMerchantQuery request, CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        IQueryable<MerchantData> merchantQuery = BuildMerchantQuery(context, request.MerchantId);
        Result<MerchantData> merchantQueryResult = await ExecuteQuerySafeSingleOrDefault(merchantQuery, cancellationToken, "Error getting merchant");
        if (merchantQueryResult.IsFailed) {
            return ResultHelpers.CreateFailure(merchantQueryResult);
        }

        Result<List<MerchantOpeningHours>> openingHoursQueryResult = await ExecuteQuerySafeToList(context.MerchantOpeningHours.Where(m => m.MerchantId == request.MerchantId), cancellationToken, "Error getting merchant opening hours");
        if (openingHoursQueryResult.IsFailed) return ResultHelpers.CreateFailure(openingHoursQueryResult);

        Result<MerchantBalanceProjectionState> merchantStateQueryResult = await ExecuteQuerySafeSingleOrDefault(context.MerchantBalanceProjectionState.Where(ms => ms.MerchantId == request.MerchantId), cancellationToken, "Error getting merchant state");
        if (merchantStateQueryResult.IsFailed) return ResultHelpers.CreateFailure(merchantStateQueryResult);

        return Result.Success(ModelFactory.ConvertFrom(merchantQueryResult.Data, merchantStateQueryResult.Data.Balance, openingHoursQueryResult.Data));
    }

    private static IQueryable<MerchantData> BuildMerchantQuery(EstateManagementContext context,
                                                               Guid merchantId) {
        return context.Merchants
                      .Select(m => new MerchantData {
                          MerchantReportingId = m.MerchantReportingId,
                          Name = m.Name,
                          CreatedDateTime = m.CreatedDateTime,
                          MerchantId = m.MerchantId,
                          Reference = m.Reference,
                          SettlementSchedule = m.SettlementSchedule,
                          AddressInfo = context.MerchantAddresses.Where(ma => ma.MerchantId == m.MerchantId)
                                               .OrderByDescending(ma => ma.CreatedDateTime)
                                               .Select(ma => new MerchantAddressData {
                                                   AddressId = ma.AddressId,
                                                   AddressLine1 = ma.AddressLine1,
                                                   AddressLine2 = ma.AddressLine2,
                                                   Country = ma.Country,
                                                   PostalCode = ma.PostalCode,
                                                   Region = ma.Region,
                                                   Town = ma.Town
                                               })
                                               .FirstOrDefault(),
                          ContactInfo = context.MerchantContacts.Where(mc => mc.MerchantId == m.MerchantId)
                                               .OrderByDescending(mc => mc.CreatedDateTime)
                                               .Select(mc => new MerchantContactData {
                                                   ContactId = mc.ContactId,
                                                   Name = mc.Name,
                                                   EmailAddress = mc.EmailAddress,
                                                   PhoneNumber = mc.PhoneNumber
                                               })
                                               .FirstOrDefault()
                      })
                      .Where(m => m.MerchantId == merchantId);
    }

    private static IQueryable<MerchantWithAddressAndContactData> BuildMerchantWithAddressQuery(EstateManagementContext context,
                                                                                               Guid estateId) {
        return context.Merchants
                      .Where(m => m.EstateId == estateId)
                      .Select(m => new MerchantWithAddressAndContactData {
                          Merchant = m,
                          MerchantAddress = context.MerchantAddresses.Where(ma => ma.MerchantId == m.MerchantId)
                                                      .OrderByDescending(ma => ma.CreatedDateTime)
                                                      .First(),
                          MerchantContact = context.MerchantContacts.Where(mc => mc.MerchantId == m.MerchantId)
                                                     .OrderByDescending(mc => mc.CreatedDateTime)
                                                     .First()
                      });
    }

    private static IQueryable<MerchantWithAddressAndContactData> ApplyMerchantFilters(IQueryable<MerchantWithAddressAndContactData> query,
                                                                                     MerchantQueries.MerchantQueryOptions queryOptions) {
        if (String.IsNullOrEmpty(queryOptions.Name) == false)
            query = query.Where(m => m.Merchant.Name.Contains(queryOptions.Name));

        if (String.IsNullOrEmpty(queryOptions.Reference) == false)
            query = query.Where(m => m.Merchant.Reference == queryOptions.Reference);

        if (queryOptions.SettlementSchedule > 0)
            query = query.Where(m => m.Merchant.SettlementSchedule == queryOptions.SettlementSchedule);

        if (String.IsNullOrEmpty(queryOptions.Region) == false)
            query = query.Where(m => m.MerchantAddress.Region.Contains(queryOptions.Region));

        if (String.IsNullOrEmpty(queryOptions.PostCode) == false)
            query = query.Where(m => m.MerchantAddress.PostalCode == queryOptions.PostCode);

        return query;
    }

    private static Dictionary<Guid, decimal> BuildMerchantBalanceLookup(List<MerchantBalanceProjectionState> balances) {
        Dictionary<Guid, decimal> balanceLookup = new();
        foreach (var balance in balances) {
            if (balanceLookup.TryAdd(balance.MerchantId, balance.Balance) == false)
                throw new InvalidOperationException($"Duplicate balance entry found for merchant {balance.MerchantId}");
        }

        return balanceLookup;
    }

    private static decimal GetMerchantBalance(Dictionary<Guid, decimal> balanceLookup,
                                              Guid merchantId) {
        if (balanceLookup.TryGetValue(merchantId, out var balance))
            return balance;

        throw new InvalidOperationException($"No balance entry found for merchant {merchantId}");
    }

    public async Task<Result<List<MerchantOperator>>> GetMerchantOperators(MerchantQueries.GetMerchantOperatorsQuery request,
                                                                             CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        var merchantOperatorsQuery = context.MerchantOperators.Where(mo => mo.MerchantId == request.MerchantId && mo.IsDeleted == false);

        var merchantOperatorsQueryResult = await ExecuteQuerySafeToList(merchantOperatorsQuery, cancellationToken, "Error getting merchant devices");

        if (merchantOperatorsQueryResult.IsFailed)
            return ResultHelpers.CreateFailure(merchantOperatorsQueryResult);

        var merchantOperators = merchantOperatorsQueryResult.Data;

        List<MerchantOperator> result = new();
        foreach (TransactionProcessor.Database.Entities.MerchantOperator merchantOperator in merchantOperators) {
            result.Add(new MerchantOperator {
                OperatorId = merchantOperator.OperatorId,
                IsDeleted = merchantOperator.IsDeleted,
                MerchantId = merchantOperator.MerchantId,
                MerchantNumber = merchantOperator.MerchantNumber,
                OperatorName = merchantOperator.Name,
                TerminalNumber = merchantOperator.TerminalNumber
            });
        }

        return Result.Success(result);
    }

    public async Task<Result<List<MerchantContract>>> GetMerchantContracts(MerchantQueries.GetMerchantContractsQuery request,
                                                                           CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        var merchantContractsQuery = context.MerchantContracts.Where(mo => mo.MerchantId == request.MerchantId && mo.IsDeleted == false).Select(mc => new {
            mc.ContractId,
            mc.IsDeleted,
            mc.MerchantId,
            ContractInfo = context.Contracts.Where(c => c.ContractId == mc.ContractId).Select(ma => new {
                ma.Description,
                OperatorName = context.Operators.Where(o => o.OperatorId == ma.OperatorId).Select(p => p.Name).Single(),
                Products = context.ContractProducts.Where(cp => cp.ContractId == mc.ContractId).Select(cp => new {
                    cp.DisplayText,
                    cp.ProductName,
                    cp.ContractProductId,
                    cp.ProductType,
                    cp.Value
                }).ToList()
                // Add more properties as needed
            }).SingleOrDefault()
        });

        var merchantContractsQueryResult = await ExecuteQuerySafeToList(merchantContractsQuery, cancellationToken, "Error getting merchant devices");

        if (merchantContractsQueryResult.IsFailed)
            return ResultHelpers.CreateFailure(merchantContractsQueryResult);

        var merchantContracts = merchantContractsQueryResult.Data;

        List<MerchantContract> result = new();
        foreach (var merchantContract in merchantContracts) {
            var c = new MerchantContract {
                ContractId = merchantContract.ContractId,
                ContractName = merchantContract.ContractInfo.Description,
                IsDeleted = merchantContract.IsDeleted,
                MerchantId = merchantContract.MerchantId,
                OperatorName = merchantContract.ContractInfo.OperatorName,
                ContractProducts = new List<MerchantContractProduct>()
            };

            foreach (var product in merchantContract.ContractInfo.Products) {
                c.ContractProducts.Add(new MerchantContractProduct {
                    ContractId = merchantContract.ContractId,
                    DisplayText = product.DisplayText,
                    MerchantId = merchantContract.MerchantId,
                    ProductName = product.ProductName,
                    ProductId = product.ContractProductId,
                    ProductType = product.ProductType,
                    Value = product.Value
                });
            }

            result.Add(c);
        }

        return Result.Success(result);
    }


    public async Task<Result<List<MerchantDevice>>> GetMerchantDevices(MerchantQueries.GetMerchantDevicesQuery request,
                                                                       CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        var merchantDevicesQuery = context.MerchantDevices.Where(mo => mo.MerchantId == request.MerchantId);
        var merchantDevicesQueryResult = await ExecuteQuerySafeToList(merchantDevicesQuery, cancellationToken, "Error getting merchant devices");

        if (merchantDevicesQueryResult.IsFailed)
            return ResultHelpers.CreateFailure(merchantDevicesQueryResult);

        var merchantDevices = merchantDevicesQueryResult.Data;

        List<MerchantDevice> result = new();
        foreach (TransactionProcessor.Database.Entities.MerchantDevice merchantDevice in merchantDevices) {
            result.Add(new MerchantDevice { DeviceId = merchantDevice.DeviceId, DeviceIdentifier = merchantDevice.DeviceIdentifier, IsDeleted = false, MerchantId = merchantDevice.MerchantId });
        }

        return Result.Success(result);
    }

    public async Task<Result<MerchantScheduleResponse>> GetMerchantSchedule(MerchantQueries.GetMerchantScheduleQuery request,
                                                                            CancellationToken cancellationToken) {
        using ResolvedDbContext<EstateManagementContext>? resolvedContext = this.Resolver.Resolve(EstateManagementDatabaseName, request.EstateId.ToString());
        await using EstateManagementContext context = resolvedContext.Context;

        var merchantScheduleQuery = from s in context.MerchantSchedules
        join d in context.MerchantScheduleMonths on s.MerchantScheduleId equals d.MerchantScheduleId
        where s.MerchantId == request.MerchantId && s.Year == request.Year
                                    select new {
            s.MerchantId,
            s.MerchantScheduleId,
            s.Year,
            d.Month,
            d.ClosedDays
        };

        var merchantScheduleQueryResult = await ExecuteQuerySafeToList(merchantScheduleQuery, cancellationToken, "Error getting merchant schedule");
        if (merchantScheduleQueryResult.IsFailed)
            return ResultHelpers.CreateFailure(merchantScheduleQueryResult);

        MerchantScheduleResponse response = new MerchantScheduleResponse();
        foreach (var item in merchantScheduleQueryResult.Data) {

            List<Int32> closedDaysList = item.ClosedDays
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s, out var n) ? n : (int?)null)
                .Where(n => n.HasValue)
                .Select(n => n.Value)
                .ToList();

            MerchantScheduleMonthResponse? monthSchedule = new() { ClosedDays = closedDaysList, Month = item.Month };

            response.Months.Add(monthSchedule);
        }
        response.Year = request.Year;

        return response;
    }

        private static async Task<Result<List<ContractProductData>>> LoadProductsAsync(EstateManagementContext context,
                                                                                         List<Guid> contractIds,
                                                                                         CancellationToken cancellationToken,
                                                                                         string stepName) {
            var query = context.ContractProducts.Where(cp => contractIds.Contains(cp.ContractId)).Select(cp => new ContractProductData {
                ContractProductId = cp.ContractProductId,
                ContractProductReportingId = cp.ContractProductReportingId,
                ContractId = cp.ContractId,
                DisplayText = cp.DisplayText,
                ProductName = cp.ProductName,
                ProductType = cp.ProductType,
                Value = cp.Value
            });
            return await ExecuteQuerySafeToList(query, cancellationToken, stepName);
        }

        private static async Task<Result<List<ContractFeeData>>> LoadFeesAsync(EstateManagementContext context,
                                                                                List<Guid> productIds,
                                                                                CancellationToken cancellationToken,
                                                                                string stepName) {
            var query = context.ContractProductTransactionFees.Where(tf => productIds.Contains(tf.ContractProductId)).Select(tf => new ContractFeeData {
                ContractProductTransactionFeeId = tf.ContractProductTransactionFeeId,
                ContractProducTransactionFeeReportingId = tf.ContractProductTransactionFeeReportingId,
                ContractProductId = tf.ContractProductId,
                Description = tf.Description,
                CalculationType = tf.CalculationType,
                FeeType = tf.FeeType,
                Value = tf.Value,
                IsEnabled = tf.IsEnabled
            });
            return await ExecuteQuerySafeToList(query, cancellationToken, stepName);
        }

        private static List<Contract> BuildContracts(List<ContractBaseData> baseContracts,
                                                     List<ContractProductData> products,
                                                     List<ContractFeeData> fees) {
            return baseContracts.Select(b => new Contract {
                ContractId = b.ContractId,
                ContractReportingId = b.ContractReportingId,
                Description = b.Description,
                EstateId = b.EstateId,
                EstateReportingId = b.EstateReportingId,
                OperatorName = b.OperatorName,
                OperatorId = b.OperatorId,
                OperatorReportingId = b.OperatorReportingId,
                Products = products.Where(p => p.ContractId == b.ContractId).Select(p => new Models.ContractProduct {
                    ContractId = p.ContractId,
                    ProductId = p.ContractProductId,
                    ContractProductReportingId = p.ContractProductReportingId,
                    DisplayText = p.DisplayText,
                    ProductName = p.ProductName,
                    ProductType = p.ProductType,
                    Value = p.Value,
                    TransactionFees = fees.Where(f => f.ContractProductId == p.ContractProductId && f.IsEnabled).Select(f => new ContractProductTransactionFee {
                        Description = f.Description,
                        Value = f.Value,
                        CalculationType = f.CalculationType,
                        FeeType = f.FeeType,
                        TransactionFeeId = f.ContractProductTransactionFeeId,
                        ContractProductTransactionFeeReportingId = f.ContractProducTransactionFeeReportingId
                    }).ToList()
                }).ToList()
            }).ToList();
        }

        private sealed class ContractBaseData {
            public Guid ContractId { get; init; }
            public int ContractReportingId { get; init; }
            public string? Description { get; init; }
            public Guid EstateId { get; init; }
        public int EstateReportingId { get; init; }
        public Guid OperatorId { get; init; }
        public int OperatorReportingId { get; init; }
        public string? OperatorName { get; init; }
        }

        private sealed class ContractProductData {
            public Guid ContractProductId { get; init; }
            public int ContractProductReportingId { get; init; }
            public Guid ContractId { get; init; }
            public string? DisplayText { get; init; }
            public string? ProductName { get; init; }
            public int ProductType { get; init; }
            public decimal? Value { get; init; }
        }

        private sealed class ContractFeeData {
            public Guid ContractProductTransactionFeeId { get; init; }
            public int ContractProducTransactionFeeReportingId { get; init; }
            public Guid ContractProductId { get; init; }
            public string? Description { get; init; }
            public int CalculationType { get; init; }
            public int FeeType { get; init; }
            public decimal Value { get; init; }
            public bool IsEnabled { get; init; }
        }


}

