using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JasperFx.Core;
using Microsoft.Data.SqlClient;
using Shared.Logger;
using TransactionProcessor.Database.Contexts;
using TransactionProcessor.Database.Entities;

namespace EstateReportingAPI.IntegrationTests;

using System.Reflection;
using System.Text;
using Microsoft.EntityFrameworkCore;

public class DatabaseHelper{
    private readonly EstateManagementGenericContext Context;

    public DatabaseHelper(EstateManagementGenericContext context){
        this.Context = context;
    }

    public async Task DeleteAllMerchants(){
        List<Merchant> merchants = await this.Context.Merchants.ToListAsync();
        this.Context.RemoveRange(merchants);
        await this.Context.SaveChangesAsync();
    }

    public async Task DeleteAllContracts(){
        List<Contract> contracts = await this.Context.Contracts.ToListAsync();
        List<ContractProduct> contractProducts = await this.Context.ContractProducts.ToListAsync();
        this.Context.RemoveRange(contractProducts);
        this.Context.RemoveRange(contracts);
        await this.Context.SaveChangesAsync();
    }

    public async Task AddResponseCode(Int32 code, String description){
        await this.Context.Database.OpenConnectionAsync(CancellationToken.None);
        try{
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("SET IDENTITY_INSERT dbo.ResponseCodes ON");
            String sql = $"insert into dbo.ResponseCodes(ResponseCode, Description) SELECT {code}, '{description}'";
            builder.AppendLine(sql);
            builder.AppendLine("SET IDENTITY_INSERT dbo.ResponseCodes OFF");
            await this.Context.Database.ExecuteSqlRawAsync(builder.ToString(), CancellationToken.None);
        }
        finally{
            await this.Context.Database.CloseConnectionAsync();
        }
    }

    public async Task AddCalendarYear(Int32 year){
        List<DateTime> datesInYear = this.GetDatesForYear(year);
        await this.AddCalendarDates(datesInYear);
    }

    public async Task AddCalendarDates(List<DateTime> dates){
        foreach (DateTime dateTime in dates){
            Calendar c = dateTime.ToCalendar();
            await this.Context.Calendar.AddAsync(c);
        }

        await this.Context.SaveChangesAsync();
    }

    public async Task AddCalendarDate(DateTime date){
        Calendar c = date.ToCalendar();
        await this.Context.Calendar.AddAsync(c);
        await this.Context.SaveChangesAsync();
    }

    public List<DateTime> GetDatesForYear(Int32 year){
        List<DateTime> datesInYear = new List<DateTime>();
        DateTime startDate = new DateTime(year, 1, 1);
        DateTime endDate = new DateTime(year, 12, 31);

        for (DateTime currentDate = startDate; currentDate <= endDate; currentDate = currentDate.AddDays(1)){
            datesInYear.Add(currentDate);
        }

        return datesInYear;
    }

    public async Task<Transaction> AddTransactionOld(DateTime dateTime,
                                                  String merchantName,
                                                  String contractName,
                                                  String contractProductName,
                                                  String responseCode,
                                                  Decimal? transactionAmount = null,
                                                  Int32? transactionReportingId = null,
                                                  String authCode=null){

        var merchant = await this.Context.Merchants.SingleOrDefaultAsync(m => m.Name == merchantName);
        if (merchant == null){
            throw new Exception($"No Merchant found with name {merchantName}");
        }

        var contract = await this.Context.Contracts.SingleOrDefaultAsync(c => c.Description == contractName);

        if (contract == null){
            throw new Exception($"No Contact record found with description {contractName}");
        }

        var @operator = await this.Context.Operators.SingleOrDefaultAsync(o => o.OperatorId == contract.OperatorId);

        if (@operator == null){
            throw new Exception($"No Operator record found with for contract {contractName}");
        }

        var contractProduct = await this.Context.ContractProducts.SingleOrDefaultAsync(cp => cp.ContractId == contract.ContractId &&
                                                                                             cp.ProductName == contractProductName);

        if (contractProduct == null){
            throw new Exception($"No Contact Product record found with description {contractProductName} for contract {contractName}");
        }

        if (contractProduct.Value.HasValue){
            transactionAmount = contractProduct.Value.GetValueOrDefault();
        }
        else{
            if (transactionAmount.HasValue == false){
                transactionAmount = 75.00m;
            }
        }

        Transaction transaction = new Transaction
        {
            MerchantId = merchant.MerchantId,
            TransactionDate = dateTime.Date,
            TransactionDateTime = dateTime,
            IsCompleted = true,
            AuthorisationCode = "ABCD1234",
            DeviceIdentifier = "testdevice1",
            //EstateOperatorReportingId = estateOperator.OperatorReportingId,
            OperatorId = @operator.OperatorId,
            ContractId = contract.ContractId,
            ContractProductId = contractProduct.ContractProductId,
            IsAuthorised = responseCode == "0000",
            ResponseCode = responseCode,
            TransactionAmount = transactionAmount.GetValueOrDefault(),
            TransactionId = Guid.NewGuid(),
            TransactionTime = dateTime.TimeOfDay,
            TransactionType = "Sale",
            ResponseMessage = responseCode == "0000" ? "SUCCESS" : "FAILED",
            TransactionNumber = "0001",
            TransactionReference = "Ref1",
            TransactionSource = 1
        };

        if (transactionReportingId.HasValue){
            transaction.TransactionReportingId = transactionReportingId.Value;
            transaction.TransactionNumber = transactionReportingId.Value.ToString("0000");
        }

        if (String.IsNullOrEmpty(authCode) == false){
            transaction.AuthorisationCode = authCode;
        }

        await this.Context.Transactions.AddAsync(transaction, CancellationToken.None);
        if (transactionReportingId.HasValue){
            await this.Context.SaveChangesWithIdentityInsert<Transaction>();
        }
        else{
            await this.Context.SaveChangesAsync(CancellationToken.None);
        }

        return transaction;
    }

    public async Task<Transaction> BuildTransactionX(DateTime dateTime,
                                                  Guid merchantId,
                                                  Guid operatorId,
                                                  Guid contractId,
                                                  Guid contractProductId,
                                                  String responseCode,
                                                  Decimal? transactionAmount = null,
                                                  Int32? transactionReportingId = null,
                                                  String authCode = null)
    {

        //var contract = await this.Context.Contracts.SingleOrDefaultAsync(c => c.Description == contractName);

        //if (contract == null)
        //{
        //    throw new Exception($"No Contact record found with description {contractName}");
        //}

        //var @operator = await this.Context.Operators.SingleOrDefaultAsync(o => o.OperatorId == contract.OperatorId);

        //if (@operator == null)
        //{
        //    throw new Exception($"No Operator record found with for contract {contractName}");
        //}

        //var contractProduct = await this.Context.ContractProducts.SingleOrDefaultAsync(cp => cp.ContractId == contract.ContractId &&
        //                                                                                     cp.ProductName == contractProductName);

        //if (contractProduct == null)
        //{
        //    throw new Exception($"No Contact Product record found with description {contractProductName} for contract {contractName}");
        //}

        //if (contractProduct.Value.HasValue)
        //{
        //    transactionAmount = contractProduct.Value.GetValueOrDefault();
        //}
        //else
        //{
        if (transactionAmount.HasValue == false)
        {
            transactionAmount = 75.00m;
        }
        //}

        Transaction transaction = new Transaction
        {
            MerchantId = merchantId,
            TransactionDate = dateTime.Date,
            TransactionDateTime = dateTime,
            IsCompleted = true,
            AuthorisationCode = "ABCD1234",
            DeviceIdentifier = "testdevice1",
            OperatorId = operatorId,
            ContractId = contractId,
            ContractProductId = contractProductId,
            IsAuthorised = responseCode == "0000",
            ResponseCode = responseCode,
            TransactionAmount = transactionAmount.GetValueOrDefault(),
            TransactionId = Guid.NewGuid(),
            TransactionTime = dateTime.TimeOfDay,
            TransactionType = "Sale",
            ResponseMessage = responseCode == "0000" ? "SUCCESS" : "FAILED",
            TransactionNumber = "0001",
            TransactionReference = "Ref1",
            TransactionSource = 1
        };

        if (transactionReportingId.HasValue)
        {
            transaction.TransactionReportingId = transactionReportingId.Value;
            transaction.TransactionNumber = transactionReportingId.Value.ToString("0000");
        }

        if (String.IsNullOrEmpty(authCode) == false)
        {
            transaction.AuthorisationCode = authCode;
        }
        
        return transaction;
    }

    public async Task AddTransactionsX(List<Transaction> transactions) {

        foreach (Transaction transaction in transactions) {
            await this.Context.Transactions.AddAsync(transaction, CancellationToken.None);
        }
        
        if (transactions.Any(t => t.TransactionReportingId> 0))
        {
            await this.Context.SaveChangesWithIdentityInsert<Transaction>();
        }
        else
        {
            await this.Context.SaveChangesAsync(CancellationToken.None);
        }
    }

    public async Task<Int32> AddEstate(String estateName, String reference){
        Estate estate = new(){
                                 CreatedDateTime = DateTime.Now,
                                 EstateId = Guid.NewGuid(),
                                 Name = estateName,
                                 Reference = reference
                             };
        await this.Context.Estates.AddAsync(estate);
        await this.Context.SaveChangesAsync(CancellationToken.None);

        return estate.EstateReportingId;
    }

    public async Task AddOperators(String estateName, List<String> operators)
    {
        Estate? estate = await this.Context.Estates.SingleOrDefaultAsync(e => e.Name == estateName);

        if (estate == null)
        {
            throw new Exception($"No estate found with name {estateName}");
        }

        foreach (String operatorName in operators) {
            Operator @operator = new Operator {
                EstateId = estate.EstateId,
                Name = operatorName,
                OperatorId = Guid.NewGuid(),
                RequireCustomMerchantNumber = false,
                RequireCustomTerminalNumber = false
            };

            await this.Context.Operators.AddAsync(@operator);
        }

        await this.Context.SaveChangesAsync(CancellationToken.None);
    }

    public async Task<Int32> AddOperator(String estateName, String operatorName)
    {
        Estate? estate = await this.Context.Estates.SingleOrDefaultAsync(e => e.Name == estateName);

        if (estate == null)
        {
            throw new Exception($"No estate found with name {estateName}");
        }

        Operator @operator = new Operator
                                        {
                                            EstateId = estate.EstateId,
                                            Name = operatorName,
                                            OperatorId = Guid.NewGuid(),
                                            RequireCustomMerchantNumber = false,
                                            RequireCustomTerminalNumber = false
                                        };

        await this.Context.Operators.AddAsync(@operator);
        await this.Context.SaveChangesAsync(CancellationToken.None);

        return @operator.OperatorReportingId;
    }
    
    public async Task<Int32> AddMerchant(String estateName, String merchantName, DateTime lastSaleDateTime, 
                                         List<(String addressLine1, String town, String postCode, String region)> addressList=null){
        Estate? estate = await this.Context.Estates.SingleOrDefaultAsync(e => e.Name == estateName);

        if (estate == null){
            throw new Exception($"No estate found with name {estateName}");
        }

        Merchant merchant = new Merchant{
                                            EstateId = estate.EstateId,
                                            MerchantId = Guid.NewGuid(),
                                            Name = merchantName,
                                            LastSaleDate = lastSaleDateTime.Date,
                                            LastSaleDateTime = lastSaleDateTime
                                        };

        await this.Context.Merchants.AddAsync(merchant);
        await this.Context.SaveChangesAsync(CancellationToken.None);

        if (addressList != null){
            var savedMerchant = await this.Context.Merchants.SingleOrDefaultAsync(m => m.MerchantId == merchant.MerchantId);

            if (savedMerchant == null){
                throw new Exception($"Error saving merchant {merchant.Name}");
            }

            foreach ((String addressLine1, String town, String postCode, String region) address in addressList){
                await this.Context.MerchantAddresses.AddAsync(new MerchantAddress{
                                                                                     AddressId = Guid.NewGuid(),
                                                                                     AddressLine1 = address.addressLine1,
                                                                                     Region = address.region,
                                                                                     Town = address.town,
                                                                                     PostalCode = address.postCode,
                                                                                     MerchantId = savedMerchant.MerchantId
                                                                                 });
            }
        }

        return merchant.MerchantReportingId;
    }

    public async Task AddMerchants(String estateName,
                                   List<(String merchantName, DateTime lastSaleDateTime, List<(String addressLine1, String town, String postCode, String region)> addressList)> merchants) {
        Estate? estate = await this.Context.Estates.SingleOrDefaultAsync(e => e.Name == estateName);

        if (estate == null) {
            throw new Exception($"No estate found with name {estateName}");
        }

        foreach (var merchant1 in merchants) {
            Merchant merchant = new Merchant {
                EstateId = estate.EstateId,
                MerchantId = Guid.NewGuid(),
                Name = merchant1.merchantName,
                LastSaleDate = merchant1.lastSaleDateTime.Date,
                LastSaleDateTime = merchant1.lastSaleDateTime
            };
            await this.Context.Merchants.AddAsync(merchant);

            if (merchant1.addressList != null) {
                foreach ((String addressLine1, String town, String postCode, String region) address in merchant1.addressList) {
                    await this.Context.MerchantAddresses.AddAsync(new MerchantAddress {
                        AddressId = Guid.NewGuid(),
                        AddressLine1 = address.addressLine1,
                        Region = address.region,
                        Town = address.town,
                        PostalCode = address.postCode,
                        MerchantId = merchant.MerchantId
                    });
                }
            }

            await this.Context.SaveChangesAsync(CancellationToken.None);

        }
    }

    public async Task<Int32> AddContract(String estateName, String contractName, String operatorName){
        Estate? estate = await this.Context.Estates.SingleOrDefaultAsync(e => e.Name == estateName);

        if (estate == null){
            throw new Exception($"No estate found with name {estateName}");
        }

        // Look up operator
        var @operator = await this.Context.Operators.SingleOrDefaultAsync(eo => eo.Name == operatorName);

        if (@operator == null){
            throw new Exception($"No Operator found with name {operatorName}");
        }

        Contract contract = new Contract{
                                            EstateId = estate.EstateId,
                                            ContractId = Guid.NewGuid(),
                                            Description = contractName,
                                            OperatorId = @operator.OperatorId,
                                        };

        await this.Context.Contracts.AddAsync(contract);
        await this.Context.SaveChangesAsync(CancellationToken.None);

        return contract.ContractReportingId;
    }

    public async Task AddContractWithProducts(String estateName, String contractName, String operatorName, List<(String productName, Int32 productType, Decimal? value)> products){
        await this.AddContract(estateName, contractName, operatorName);

        foreach ((String productName, Int32 productType, Decimal? value) product in products){
            await this.AddContractProduct(contractName, product.productName, product.productType, product.value);
        }
    }

    public async Task AddContractProduct(String contractName, String productName, Int32 productType, Decimal? value){
        Contract? contract = await this.Context.Contracts.SingleOrDefaultAsync(c => c.Description == contractName);

        if (contract == null){
            throw new Exception($"No Contact record found with description {contractName}");
        }

        ContractProduct contractProduct = new ContractProduct{
                                                                 ContractId = contract.ContractId,
                                                                 DisplayText = productName,
                                                                 ContractProductId = Guid.NewGuid(),
                                                                 ProductName = productName,
                                                                 ProductType = productType,
                                                                 Value = value
                                                             };
        
        await this.Context.ContractProducts.AddAsync(contractProduct);
        await this.Context.SaveChangesAsync(CancellationToken.None);

        ContractProductTransactionFee fee = new ContractProductTransactionFee
                                            {
                                                CalculationType = 0,
                                                ContractProductId = contractProduct.ContractProductId,
                                                Description = "Merchant Commission",
                                                FeeType = 0,
                                                IsEnabled = true,
                                                ContractProductTransactionFeeId = Guid.NewGuid(),
                                                Value = 0.5m
                                            };
        await this.Context.ContractProductTransactionFees.AddAsync(fee);
        await this.Context.SaveChangesAsync(CancellationToken.None);
    }

    public async Task AddMerchantSettlementFee(Guid settlementId, DateTime feeCalculatedDateTime, Guid merchantId, Guid operatorId, Guid contractId, Guid contractProductId, CancellationToken cancellationToken, Boolean isSettled = false){
        
        var productTransactionFee = await this.Context.ContractProductTransactionFees.SingleOrDefaultAsync(f => f.ContractProductId == contractProductId, cancellationToken);
        
        Transaction transaction = new(){
                                           ContractProductId = contractProductId,
                                           MerchantId = merchantId,
                                           OperatorId = operatorId,
                                           ContractId = contractId,
                                           IsAuthorised = true,
                                           IsCompleted = true,
                                           TransactionAmount = 1.00m,
                                           TransactionDate = feeCalculatedDateTime.Date,
                                           TransactionDateTime = feeCalculatedDateTime,
                                           TransactionSource = 1,
                                           TransactionTime = feeCalculatedDateTime.TimeOfDay,
                                           TransactionId = Guid.NewGuid(),
                                       };
        await this.Context.Transactions.AddAsync(transaction,cancellationToken);
        await this.Context.SaveChangesAsync(cancellationToken);

        MerchantSettlementFee fee = new(){
                                             FeeCalculatedDateTime = feeCalculatedDateTime,
                                             MerchantId = merchantId,
                                             CalculatedValue = productTransactionFee.Value,
                                             FeeValue = productTransactionFee.Value,
                                             IsSettled = isSettled,
                                             SettlementId = settlementId,
                                             ContractProductTransactionFeeId = productTransactionFee.ContractProductTransactionFeeId,
                                             TransactionId = transaction.TransactionId
        };
        await this.Context.MerchantSettlementFees.AddAsync(fee, cancellationToken);
        await this.Context.SaveChangesAsync(cancellationToken);
    }

    public async Task<(Decimal settledTransactions, Decimal pendingSettlementTransactions, Decimal settlementFees, Decimal pendingSettlementFees)> AddSettlementRecord(Guid estateId, Guid merchantId, Guid operatorId, DateTime settlementDate, Int32 settledTransactionCount, Int32 pendingSettlementTransactionCount){
        List<Transaction> settledTransactions = new();
        List<Transaction> pendingSettlementTransactions = new();
        List<(Decimal feeValue, Decimal calulatedValue, Guid contractProductTransactionFeeId, Boolean isSettled, Guid transactionId)> settlementFees = new();
        List<(Decimal feeValue, Decimal calulatedValue, Guid contractProductTransactionFeeId, Boolean isSettled, Guid transactionId)> pendingSettlementFees = new();
        var feeId = Guid.NewGuid();

        //var @operator = await this.Context.Operators.SingleOrDefaultAsync(o => o.Name == operatorName);
        var contractProducts = await this.Context.ContractProducts.Join(
                                                                        this.Context.Contracts,
                                                                        c => c.ContractId,
                                                                        o => o.ContractId,
                                                                        (cp, c) => new{
                                                                                          cp,
                                                                                          c
                                                                                      }
                                                                       )
            .Join(this.Context.ContractProductTransactionFees,
                c => c.cp.ContractProductId,
                f => f.ContractProductId,
                (c, f) => new 
                {
                    c.cp,
                    c.c,
                    f
                }).Where(x => x.c.OperatorId == operatorId).FirstAsync();


        for (int i = 1; i <= settledTransactionCount; i++){
            Transaction transaction = await this.BuildTransactionX(settlementDate.AddDays(-1), merchantId, operatorId, contractProducts.c.ContractId,
                contractProducts.cp.ContractProductId,"0000", null);
            settledTransactions.Add(transaction);
            settlementFees.Add((0.5m, 0.5m * i, contractProducts.f.ContractProductTransactionFeeId, true, transaction.TransactionId));
        }
        
        for (int i = 1; i <= pendingSettlementTransactionCount; i++){
            Transaction transaction = await this.BuildTransactionX(settlementDate.AddDays(-1), merchantId, operatorId, contractProducts.c.ContractId,
                contractProducts.cp.ContractProductId, "0000", null);
            pendingSettlementTransactions.Add(transaction);
            pendingSettlementFees.Add((0.5m, 0.5m * i, contractProducts.f.ContractProductTransactionFeeId, false, transaction.TransactionId));
        }
        await this.AddTransactionsX(settledTransactions);
        await this.AddTransactionsX(pendingSettlementTransactions);
        await this.AddSettlementRecordWithFees(settlementDate, estateId, merchantId, pendingSettlementFees, settlementFees);

        return (settledTransactions.Sum(s => s.TransactionAmount), pendingSettlementTransactions.Sum(p => p.TransactionAmount),
            settlementFees.Sum(s => s.calulatedValue), pendingSettlementFees.Sum(p => p.calulatedValue));
    }

    public async Task AddSettlementRecordWithFees(DateTime dateTime,
                                                  Guid estateId,
                                                  Guid merchantId,
                                                  List<(Decimal feeValue, Decimal calulatedValue, Guid contractProductTransactionFeeId, Boolean isSettled, Guid transactionId)> pendingFeesList,
                                                  List<(Decimal feeValue, Decimal calulatedValue, Guid contractProductTransactionFeeId, Boolean isSettled, Guid transactionId)> settledFeesList){

        Settlement settlementRecord = new Settlement{
                                                        ProcessingStarted = true,
                                                        IsCompleted = pendingFeesList.Count == 0,
                                                        EstateId = estateId,
                                                        MerchantId = merchantId,
                                                        ProcessingStartedDateTIme = dateTime,
                                                        SettlementDate = dateTime,
                                                        SettlementId = Guid.NewGuid(),
                                                    };
        await this.Context.Settlements.AddAsync(settlementRecord, CancellationToken.None);

        await this.Context.SaveChangesAsync(CancellationToken.None);
        
        foreach ((Decimal feeValue, Decimal calulatedValue, Guid contractProductTransactionFeeId, Boolean isSettled, Guid transactionId) fee in pendingFeesList){
            MerchantSettlementFee merchantSettlementFee = new MerchantSettlementFee{
                MerchantId = merchantId,
                                                                                       SettlementId = settlementRecord.SettlementId,
                                                                                       IsSettled = fee.isSettled,
                                                                                       FeeCalculatedDateTime = dateTime,
                                                                                       TransactionId = fee.transactionId,
                                                                                       ContractProductTransactionFeeId = fee.contractProductTransactionFeeId,
                                                                                       CalculatedValue = fee.calulatedValue,
                                                                                       FeeValue = fee.feeValue
                                                                                   };
            await this.Context.MerchantSettlementFees.AddAsync(merchantSettlementFee, CancellationToken.None);
        }

        foreach ((Decimal feeValue, Decimal calulatedValue, Guid contractProductTransactionFeeId, Boolean isSettled, Guid transactionId) fee in settledFeesList)
        {
            MerchantSettlementFee merchantSettlementFee = new MerchantSettlementFee
            {
                MerchantId = merchantId,
                SettlementId = settlementRecord.SettlementId,
                IsSettled = fee.isSettled,
                FeeCalculatedDateTime = dateTime,
                TransactionId = fee.transactionId,
                ContractProductTransactionFeeId = fee.contractProductTransactionFeeId,
                CalculatedValue = fee.calulatedValue,
                FeeValue = fee.feeValue
            };
            await this.Context.MerchantSettlementFees.AddAsync(merchantSettlementFee, CancellationToken.None);
        }

        await this.Context.SaveChangesAsync(CancellationToken.None);
    }

    public async Task RunTodaysTransactionsSummaryProcessing(DateTime date)
    {
        await this.Context.Database.ExecuteSqlAsync(
            $"exec spBuildTodaysTransactions @date={date.Date.ToString("yyyy-MM-dd")}", CancellationToken.None);
    }

    public async Task RunHistoricTransactionsSummaryProcessing(DateTime date)
    {
        await this.Context.Database.ExecuteSqlAsync(
            $"exec spBuildHistoricTransactions @date={date.Date.ToString("yyyy-MM-dd")}", CancellationToken.None);
    }

    public async Task RunSettlementSummaryProcessing(DateTime date)
    {
        await this.Context.Database.ExecuteSqlAsync(
            $"exec spBuildSettlementSummary @date={date.Date.ToString("yyyy-MM-dd")}", CancellationToken.None);
    }

    public async Task CreateStoredProcedures(CancellationToken cancellationToken)
    {
        String executingAssemblyLocation = Assembly.GetExecutingAssembly().Location;
        String executingAssemblyFolder = Path.GetDirectoryName(executingAssemblyLocation);

        String scriptsFolder = $@"{executingAssemblyFolder}/StoredProcedures";

        String[] directiories = Directory.GetDirectories(scriptsFolder);
        if (directiories.Length == 0)
        {
            var list = new List<string> { scriptsFolder };
            directiories = list.ToArray();
        }
        directiories = directiories.OrderBy(d => d).ToArray();

        foreach (String directiory in directiories)
        {
            String[] sqlFiles = Directory.GetFiles(directiory, "*.sql");
            foreach (String sqlFile in sqlFiles.OrderBy(x => x))
            {
                Logger.LogDebug($"About to create Stored Procedure [{sqlFile}]");
                String sql = System.IO.File.ReadAllText(sqlFile);

                // Check here is we need to replace a Database Name
                if (sql.Contains("{DatabaseName}"))
                {
                    sql = sql.Replace("{DatabaseName}", this.Context.Database.GetDbConnection().Database);
                }

                // Create the new view using the original sql from file
                await this.Context.Database.ExecuteSqlRawAsync(sql, cancellationToken);

                Logger.LogDebug($"Created Stored Procedure [{sqlFile}] successfully.");
            }
        }
    }

    public async Task<Guid?> GetOperatorId(Int32 operatorReportingId, CancellationToken cancellationToken) {
        return await this.Context.Operators.Where(o => o.OperatorReportingId == operatorReportingId).Select(o => o.OperatorId).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<Guid?> GetMerchantId(Int32 merchantReportingId, CancellationToken cancellationToken)
    {
        return await this.Context.Merchants.Where(o => o.MerchantReportingId == merchantReportingId).Select(o => o.MerchantId).SingleOrDefaultAsync(cancellationToken);
    }
}

public static class IdentityHelpers{
    public static Task EnableIdentityInsert<T>(this DbContext context) => SetIdentityInsert<T>(context, enable:true);
    public static Task DisableIdentityInsert<T>(this DbContext context) => SetIdentityInsert<T>(context, enable:false);

    private static Task SetIdentityInsert<T>(DbContext context, bool enable){
        var entityType = context.Model.FindEntityType(typeof(T));
        var value = enable ? "ON" : "OFF";
        String schema = entityType.GetSchema();
        return context.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT {schema}.[{entityType.GetTableName()}] {value}");
    }

    public static async Task SaveChangesWithIdentityInsert<T>(this DbContext context){
        using var transaction = context.Database.BeginTransaction();
        await context.EnableIdentityInsert<T>();
        await context.SaveChangesAsync();
        await context.DisableIdentityInsert<T>();
        await transaction.CommitAsync();
    }
}