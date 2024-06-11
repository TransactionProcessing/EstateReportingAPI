CREATE OR ALTER   PROCEDURE [dbo].[spBuildSettlementSummary] @date date
AS

declare @weekNumber int
declare @yearnumber int
declare @startDate date
declare @enddate date

SELECT @weekNumber = WeekNumber, @yearnumber = year from calendar where date = @date

SELECT @startDate = min(date), @enddate = max(date) from calendar where WeekNumber = @weekNumber and Year = @yearnumber and date < convert(date,getdate())

delete from settlementsummary where settlementdate BETWEEN @startDate and @enddate

insert into settlementsummary(IsCompleted, IsSettled, SettlementDate, merchantreportingid, operatorreportingid, contractproductreportingid,
salesvalue,feevalue, salescount, feecount)
select settlement.IsCompleted, IsSettled, SettlementDate, merchant.merchantreportingid, operator.operatorreportingid, contractproduct.contractproductreportingid,
sum([transaction].TransactionAmount) as salesvalue,sum(merchantsettlementfee.CalculatedValue) as feevalue, 
count([transaction].TransactionAmount) as salescount, count(merchantsettlementfee.CalculatedValue) as feecount
from settlement
inner join merchantsettlementfee on merchantsettlementfee.SettlementId = settlement.SettlementId
inner join [transaction] on [transaction].TransactionId = merchantsettlementfee.TransactionId
inner join contractproducttransactionfee on contractproducttransactionfee.ContractProductTransactionFeeId = merchantsettlementfee.ContractProductTransactionFeeId
inner join contractproduct on contractproducttransactionfee.ContractProductId = contractproduct.ContractProductId
inner join contract on contract.ContractId = contractproduct.ContractId
inner join merchant on merchant.MerchantId = merchantsettlementfee.MerchantId
inner join estate on estate.EstateId = merchant.EstateId
inner join operator on operator.OperatorId = contract.OperatorId
where settlementdate BETWEEN @startDate and @enddate
group by settlement.IsCompleted, IsSettled, SettlementDate, merchant.merchantreportingid, operator.operatorreportingid, contractproduct.contractproductreportingid