using System;
using System.Collections.Generic;

namespace EstateReportingAPI.DataTransferObjects;

public class TransactionMixSummaryResponse
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public TransactionMixBreakdown Breakdown { get; set; }
    public TransactionMixMeasure Measure { get; set; }
    public int TotalCount { get; set; }
    public decimal TotalValue { get; set; }
    public List<TransactionMixSummaryGroup> Groups { get; set; } = [];
    public List<TransactionMixSummaryTransaction> Transactions { get; set; } = [];
}

public class TransactionMixSummaryGroup
{
    public string? GroupKey { get; set; }
    public string? GroupName { get; set; }
    public int TransactionCount { get; set; }
    public decimal TransactionValue { get; set; }
}

public class TransactionMixSummaryTransaction
{
    public Guid Id { get; set; }
    public DateTime DateTime { get; set; }
    public string? Merchant { get; set; }
    public Guid MerchantId { get; set; }
    public int MerchantReportingId { get; set; }
    public string? Operator { get; set; }
    public Guid OperatorId { get; set; }
    public int OperatorReportingId { get; set; }
    public string? Product { get; set; }
    public Guid ProductId { get; set; }
    public int ProductReportingId { get; set; }
    public string? Type { get; set; }
    public string? Status { get; set; }
    public decimal Value { get; set; }
    public decimal TotalFees { get; set; }
    public string? SettlementReference { get; set; }
    public int TransactionNumber { get; set; }
}
