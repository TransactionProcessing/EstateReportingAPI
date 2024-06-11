CREATE OR ALTER   PROCEDURE [dbo].[spBuildHistoricTransactions] @date date
AS

insert into TransactionHistory(MerchantReportingId, ContractProductReportingId,ContractReportingId,OperatorReportingId,
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
select t.MerchantReportingId,
t.ContractProductReportingId,
t.ContractReportingId,
t.OperatorReportingId,
t.TransactionId,
t.AuthorisationCode,
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
t.Hour
from TodayTransactions t
where t.TransactionDate = @date

delete from TodayTransactions where TransactionDate = @date