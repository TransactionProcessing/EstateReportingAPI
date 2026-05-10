using System.Collections.Generic;

namespace EstateReportingAPI.DataTransferObjects;
public class TransactionSummaryByOperatorResponse
{
    public List<OperatorDetail> Operators { get; set; }
    public OperatorDetailSummary Summary { get; set; }
}