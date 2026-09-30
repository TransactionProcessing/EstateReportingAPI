using System;
using System.Collections.Generic;

namespace EstateReportingAPI.DataTransferObjects;
public class TransactionSummaryByOperatorRequest
{
    public List<Int32>? Operators { get; set; }
    public List<Int32>? Merchants { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
