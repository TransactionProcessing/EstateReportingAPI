CREATE OR ALTER   PROCEDURE [dbo].[spBuildTodaysTransactions] @date date
AS

insert into TodayTransactions(MerchantReportingId, ContractProductReportingId,ContractReportingId,OperatorReportingId,
TransactionId,
AuthorisationCode,
DeviceIdentifier,
IsAuthorised,
IsCompleted,
ResponseCode,
ResponseMessage,
TransactionDate,
TransactionDateTime, 
TransactionNumber,
TransactionReference,
TransactionTime,
TransactionSource,
TransactionType,
TransactionReportingId,
TransactionAmount,
Hour)
select merchant.MerchantReportingId,
ISNULL(contractproduct.ContractProductReportingId,0) as ContractProductReportingId,
ISNULL(contract.ContractReportingId,0) as ContractReportingId,
ISNULL(operator.OperatorReportingId,0) as OperatorReportingId,
t.TransactionId,
ISNULL(t.AuthorisationCode,'') as AuthorisationCode,
t.DeviceIdentifier,
t.IsAuthorised,
t.IsCompleted,
t.ResponseCode,
t.ResponseMessage,
t.TransactionDate,
t.TransactionDateTime, 
t.TransactionNumber,
t.TransactionReference,
t.TransactionTime,
t.TransactionSource,
t.TransactionType,
t.TransactionReportingId,
t.TransactionAmount,
DATEPART(HH, t.transactiondatetime) as Hour
from [transaction] t
inner join merchant on merchant.MerchantId = t.MerchantId
left outer join contractproduct on contractproduct.ContractProductId = t.ContractProductId
left outer join contract on contract.ContractId = t.ContractId
left outer join operator on operator.OperatorId= t.OperatorId
where transactiondate = @date
and TransactionReportingId not in (select distinct TransactionReportingId from TodayTransactions)