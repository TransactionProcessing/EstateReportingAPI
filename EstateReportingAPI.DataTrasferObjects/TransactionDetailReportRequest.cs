using System;
using System.Collections.Generic;

namespace EstateReportingAPI.DataTransferObjects;

public class TransactionDetailReportRequest
{
    public List<Int32>? Operators { get; set; }
    public List<Int32>? Merchants { get; set; }
    public List<Int32>? Products { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}