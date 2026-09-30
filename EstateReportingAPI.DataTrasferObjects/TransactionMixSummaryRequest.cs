using System;
using System.Text.Json.Serialization;

namespace EstateReportingAPI.DataTransferObjects;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TransactionMixBreakdown
{
    Product,
    TransactionType,
    Operator,
    Status
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TransactionMixMeasure
{
    Count,
    Value
}

public class TransactionMixSummaryRequest
{
    public int? MerchantReportingId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public TransactionMixBreakdown Breakdown { get; set; }
    public TransactionMixMeasure Measure { get; set; }
    public int TopN { get; set; } = 5;
}
