using TransactionProcessing.UnionResults;

namespace EstateReportingAPI.BusinessLogic.Services;

internal static class ResultHelpers
{
    internal static ErrorList CreateFailure(IResultOutcome result) => result.GetOutcome() switch
    {
        { Kind: ResultKind.Error, Error: not null } outcome => new ErrorList([outcome.Error]),
        { Kind: ResultKind.ErrorList, Errors: not null } outcome => outcome.Errors,
        _ => throw new InvalidOperationException("Only a failed result can be propagated as a failure.")
    };
}
