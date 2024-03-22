namespace EstateReportingAPI.IntegrationTests;

using System.Text;
using EstateManagement.Database.Contexts;
using EstateManagement.Database.Entities;
using EstateManagement.Database.Migrations.MySql;
using k8s.KubeConfigModels;
using Microsoft.AspNetCore.Html;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1;

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

    public async Task DeleteAllEstateOperator(){
        List<EstateOperator> operators = await this.Context.EstateOperators.ToListAsync();
        this.Context.RemoveRange(operators);
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
            EstateManagement.Database.Entities.Calendar c = dateTime.ToCalendar();
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

    public async Task<Transaction> AddTransaction(DateTime dateTime,
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

        var estateOperator = await this.Context.EstateOperators.SingleOrDefaultAsync(eo => eo.OperatorId == contract.OperatorId);

        if (estateOperator == null){
            throw new Exception($"No Operator record found with for contract {contractName}");
        }

        var contractProduct = await this.Context.ContractProducts.SingleOrDefaultAsync(cp => cp.ContractReportingId == contract.ContractReportingId &&
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

        Transaction transaction = new Transaction{
                                                     MerchantReportingId = merchant.MerchantReportingId,
                                                     TransactionDate = dateTime.Date,
                                                     TransactionDateTime = dateTime,
                                                     IsCompleted = true,
                                                     AuthorisationCode = "ABCD1234",
                                                     DeviceIdentifier = "testdevice1",
                                                     EstateOperatorReportingId = estateOperator.EstateOperatorReportingId,
                                                     ContractReportingId = contract.ContractReportingId,
                                                     ContractProductReportingId = contractProduct.ContractProductReportingId,
                                                     IsAuthorised = responseCode == "0000",
                                                     ResponseCode = responseCode,
                                                     TransactionAmount = transactionAmount.GetValueOrDefault(),
                                                     TransactionId = Guid.NewGuid(),
                                                     TransactionTime = dateTime.TimeOfDay,
                                                     TransactionType = "Sale",
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

    public async Task<Int32> AddEstateOperator(String estateName, String operatorIdentifier){
        Estate? estate = await this.Context.Estates.SingleOrDefaultAsync(e => e.Name == estateName);

        if (estate == null){
            throw new Exception($"No estate found with name {estateName}");
        }

        EstateOperator estateOperator = new EstateOperator{
                                                              EstateReportingId = estate.EstateReportingId,
                                                              Name = operatorIdentifier,
                                                              OperatorId = Guid.NewGuid(),
                                                              RequireCustomMerchantNumber = false,
                                                              RequireCustomTerminalNumber = false
                                                          };

        await this.Context.EstateOperators.AddAsync(estateOperator);
        await this.Context.SaveChangesAsync(CancellationToken.None);

        return estateOperator.EstateReportingId;
    }

    public async Task<Int32> AddMerchant(String estateName, String merchantName, DateTime lastSaleDateTime, List<(String addressLine1, String town, String postCode, String region)> addressList = null){
        Estate? estate = await this.Context.Estates.SingleOrDefaultAsync(e => e.Name == estateName);

        if (estate == null){
            throw new Exception($"No estate found with name {estateName}");
        }

        Merchant merchant = new Merchant{
                                            EstateReportingId = estate.EstateReportingId,
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
                                                                                     MerchantReportingId = savedMerchant.MerchantReportingId
                                                                                 });
            }
        }

        return merchant.MerchantReportingId;
    }

    public async Task<Int32> AddContract(String estateName, String contractName, String operatorName){
        Estate? estate = await this.Context.Estates.SingleOrDefaultAsync(e => e.Name == estateName);

        if (estate == null){
            throw new Exception($"No estate found with name {estateName}");
        }

        // Look up operator
        EstateOperator? estateOperator = await this.Context.EstateOperators.SingleOrDefaultAsync(eo => eo.Name == operatorName);

        if (estateOperator == null){
            throw new Exception($"No Operator found with name {operatorName}");
        }

        Contract contract = new Contract{
                                            EstateReportingId = estate.EstateReportingId,
                                            ContractId = Guid.NewGuid(),
                                            Description = contractName,
                                            OperatorId = estateOperator.OperatorId,
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
                                                                 ContractReportingId = contract.ContractReportingId,
                                                                 DisplayText = productName,
                                                                 ProductId = Guid.NewGuid(),
                                                                 ProductName = productName,
                                                                 ProductType = productType,
                                                                 Value = value
                                                             };

        await this.Context.ContractProducts.AddAsync(contractProduct);
        await this.Context.SaveChangesAsync(CancellationToken.None);
    }

    public async Task<(Decimal settledTransactions, Decimal pendingSettlementTransactions, Decimal settlementFees, Decimal pendingSettlementFees)> AddSettlementRecord(String merchantName, String operatorName, DateTime settlementDate, Int32 settledTransactionCount, Int32 pendingSettlementTransactionCount){
        List<Transaction> settledTransactions = new();
        List<Transaction> pendingSettlementTransactions = new();
        List<(Decimal feeValue, Decimal calulatedValue, Int32 transactionFeeReportingId, Boolean isSettled)> settlementFees = new();
        List<(Decimal feeValue, Decimal calulatedValue, Int32 transactionFeeReportingId, Boolean isSettled)> pendingSettlementFees = new();

        var estateOperator = await this.Context.EstateOperators.SingleOrDefaultAsync(o => o.Name == operatorName);
        //var contract = await this.Context.Contracts.SingleOrDefaultAsync(c => c.OperatorId == estateOperator.OperatorId);
        //var contractProducts = await this.Context.ContractProducts.FirstAsync(cp => cp.ContractReportingId == contract.ContractReportingId);
        var contractProducts = await this.Context.ContractProducts.Join(
                                                                        this.Context.Contracts,
                                                                        c => c.ContractReportingId,
                                                                        o => o.ContractReportingId,
                                                                        (cp, c) => new{
                                                                                          cp,
                                                                                          c
                                                                                      }
                                                                       ).Where(x => x.c.OperatorId == estateOperator.OperatorId).FirstAsync();

        for (int i = 1; i <= settledTransactionCount; i++){
            Transaction transaction = await this.AddTransaction(settlementDate, merchantName, contractProducts.c.Description, contractProducts.cp.ProductName, "0000", i);
            settledTransactions.Add(transaction);
            settlementFees.Add((0.5m, 0.5m * i, i, true));
        }

        await this.AddSettlementRecordWithFees(settlementDate, merchantName, settlementFees);

        for (int i = 1; i <= pendingSettlementTransactionCount; i++){
            Transaction transaction = await this.AddTransaction(DateTime.Now, merchantName, contractProducts.c.Description, contractProducts.cp.ProductName, "0000", i);
            pendingSettlementTransactions.Add(transaction);
            pendingSettlementTransactions.Add(transaction);
            pendingSettlementFees.Add((0.5m, 0.5m * i, i, false));
        }

        await this.AddSettlementRecordWithFees(settlementDate, merchantName, pendingSettlementFees);

        return (settledTransactions.Sum(s => s.TransactionAmount), pendingSettlementTransactions.Sum(p => p.TransactionAmount),
            settlementFees.Sum(s => s.calulatedValue), pendingSettlementFees.Sum(p => p.calulatedValue));
    }

    public async Task AddSettlementRecordWithFees(DateTime dateTime,
                                                  String merchantName,
                                                  List<(Decimal feeValue, Decimal calulatedValue, Int32 transactionFeeReportingId, Boolean isSettled)> feesList){
        var merchant = await this.Context.Merchants.SingleOrDefaultAsync(m => m.Name == merchantName);
        if (merchant == null){
            throw new Exception($"No Merchant found with name {merchantName}");
        }

        Settlement settlementRecord = new Settlement{
                                                        ProcessingStarted = true,
                                                        IsCompleted = true,
                                                        EstateReportingId = merchant.EstateReportingId,
                                                        MerchantReportingId = merchant.MerchantReportingId,
                                                        ProcessingStartedDateTIme = dateTime,
                                                        SettlementDate = dateTime,
                                                        SettlementId = Guid.NewGuid(),
                                                    };
        await this.Context.Settlements.AddAsync(settlementRecord, CancellationToken.None);

        await this.Context.SaveChangesAsync(CancellationToken.None);

        //var settlement = await this.Context.Settlements.Where(s => s.SettlementId == settlementRecord.SettlementId).SingleAsync(CancellationToken.None);

        Int32 counter = 1;
        foreach ((Decimal feeValue, Decimal calulatedValue, Int32 transactionFeeReportingId, Boolean isSettled) fee in feesList){
            MerchantSettlementFee merchantSettlementFee = new MerchantSettlementFee{
                                                                                       MerchantReportingId = merchant.MerchantReportingId,
                                                                                       SettlementReportingId = settlementRecord.SettlementReportingId,
                                                                                       IsSettled = fee.isSettled,
                                                                                       FeeCalculatedDateTime = dateTime,
                                                                                       TransactionReportingId = counter,
                                                                                       TransactionFeeReportingId = fee.transactionFeeReportingId,
                                                                                       CalculatedValue = fee.calulatedValue,
                                                                                       FeeValue = fee.feeValue
                                                                                   };
            counter++;
            await this.Context.MerchantSettlementFees.AddAsync(merchantSettlementFee, CancellationToken.None);
        }

        await this.Context.SaveChangesAsync(CancellationToken.None);
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