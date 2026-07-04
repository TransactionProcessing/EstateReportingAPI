using System.Text.Json.Serialization;

namespace EstateReportingAPI.Models;

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
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public TransactionMixBreakdown Breakdown { get; set; }
    public TransactionMixMeasure Measure { get; set; }
    public int TopN { get; set; } = 5;
}
