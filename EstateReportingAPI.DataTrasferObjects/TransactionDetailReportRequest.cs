using System;
using System.Collections.Generic;

namespace EstateReportingAPI.DataTransferObjects;

public class TransactionDetailReportRequest
{
    public List<Int32>? Operators { get; set; }
    public List<Int32>? Merchants { get; set; }
    public List<Int32>? Products { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}
