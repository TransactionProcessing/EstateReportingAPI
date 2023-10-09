namespace EstateReportingAPI.IntegrationTests;

using EstateManagement.Database.Contexts;
using EstateManagement.Database.Entities;
using EstateManagement.Database.Migrations.MySql;
using Microsoft.EntityFrameworkCore;

public class DatabaseHelper{
    private readonly EstateManagementGenericContext Context;

    public DatabaseHelper(EstateManagementGenericContext context){
        this.Context = context;
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

        for (DateTime currentDate = startDate; currentDate <= endDate; currentDate = currentDate.AddDays(1))
        {
            datesInYear.Add(currentDate);
        }
        return datesInYear;
    }

    public async Task AddSettlementRecordWithFees(DateTime dateTime, Int32 estateReportingId, Int32 merchantReportingId,
                                          List<(Decimal feeValue, Decimal calulatedValue, Int32 transactionFeeReportingId)> feesList){
        Settlement settlementRecord = new Settlement{
                                         ProcessingStarted = true,
                                         IsCompleted = true,
                                         EstateReportingId = estateReportingId,
                                         MerchantReportingId = merchantReportingId,
                                         ProcessingStartedDateTIme = dateTime,
                                         SettlementDate = dateTime,
                                         SettlementId = Guid.NewGuid(),
                                     };
        await this.Context.Settlements.AddAsync(settlementRecord, CancellationToken.None);

        await this.Context.SaveChangesAsync(CancellationToken.None);

        var settlement = await this.Context.Settlements.Where(s => s.SettlementId == settlementRecord.SettlementId).SingleAsync(CancellationToken.None);

        Int32 counter = 1;
        foreach ((Decimal feeValue, Decimal calulatedValue, Int32 transactionFeeReportingId) fee in feesList){
            MerchantSettlementFee merchantSettlementFee = new MerchantSettlementFee{
                                                                                       MerchantReportingId = merchantReportingId,
                                                                                       SettlementReportingId = settlement.SettlementReportingId,
                                                                                       IsSettled = true,
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

    public async Task<Transaction> AddTransaction(DateTime dateTime, Int32 merchantReportingId,String operatorIdentifier, Int32 contractProductReportingId,
                                                 String responseCode, Decimal transactionAmount){

        Transaction transaction = new Transaction{
                                                     MerchantReportingId = merchantReportingId,
                                                     TransactionDate = dateTime.Date,
                                                     TransactionDateTime = dateTime,
                                                     IsCompleted = true,
                                                     AuthorisationCode = "ABCD1234",
                                                     DeviceIdentifier = "testdevice1",
                                                     OperatorIdentifier = operatorIdentifier,
                                                     ContractReportingId = 1,
                                                     ContractProductReportingId = contractProductReportingId,
                                                     IsAuthorised = responseCode == "0000",
                                                     ResponseCode = responseCode,
                                                     TransactionAmount = transactionAmount,
                                                     TransactionId = Guid.NewGuid(),
                                                     TransactionTime = dateTime.TimeOfDay,
                                                     TransactionType = "Sale"
                                                 };
        await this.Context.Transactions.AddAsync(transaction, CancellationToken.None);
        await this.Context.SaveChangesAsync(CancellationToken.None);
        return transaction;
    }

    public async Task AddMerchant(Int32 estateReportingId, String merchantName, DateTime lastSaleDateTime, List<(String addressLine1,String town,String postCode, String region)> addressList = null){
        Merchant merchant = new Merchant{
                                            EstateReportingId = estateReportingId,
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
            foreach ((String addressLine1, String town, String postCode, String region) address in addressList)
            {
                await this.Context.MerchantAddresses.AddAsync(new MerchantAddress
                                                              {
                                                                  AddressId = Guid.NewGuid(),
                                                                  AddressLine1 = address.addressLine1,
                                                                  Region = address.region,
                                                                  Town = address.town,
                                                                  PostalCode = address.postCode,
                                                                  MerchantReportingId = savedMerchant.MerchantReportingId
                                                              });
            }
        }

    }

    public async Task AddContractProduct(String productName){
        ContractProduct contractProduct = new ContractProduct{
                                                                 ContractReportingId = 1,
                                                                 DisplayText = productName,
                                                                 ProductId = Guid.NewGuid(),
                                                                 ProductName = productName,
                                                                 ProductType = 1,
                                                                 Value = null
                                                             };

        await this.Context.ContractProducts.AddAsync(contractProduct);
        await this.Context.SaveChangesAsync(CancellationToken.None);
    }

    public async Task AddEstateOperator(String operatorIdentifier){
        EstateOperator estateOperator = new EstateOperator{
                                                              EstateReportingId = 1,
                                                              Name = operatorIdentifier,
                                                              OperatorId = Guid.NewGuid(),
                                                              RequireCustomMerchantNumber = false,
                                                              RequireCustomTerminalNumber = false
                                                          };

        await this.Context.EstateOperators.AddAsync(estateOperator);
        await this.Context.SaveChangesAsync(CancellationToken.None);
    }

}