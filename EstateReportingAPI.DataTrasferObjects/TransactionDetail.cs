using System;

namespace EstateReportingAPI.DataTransferObjects;

/// <summary>API data model describing transaction detail information.</summary>

public class TransactionDetail
{
    /// <summary>The id value.</summary>
    public Guid Id { get; set; }
    /// <summary>Date and time of the transaction.</summary>
    public DateTimeOffset DateTime { get; set; }
    /// <summary>The merchant value.</summary>
    public String Merchant { get; set; }
    /// <summary>Unique identifier of the merchant.</summary>
    public Guid MerchantId { get; set; }
    /// <summary>Numeric reporting identifier used to filter or identify a merchant.</summary>
    public Int32 MerchantReportingId { get; set; }
    /// <summary>The operator value.</summary>
    public String Operator { get; set; }
    /// <summary>Unique identifier of the operator.</summary>
    public Guid OperatorId { get; set; }
    /// <summary>The operator reporting id value.</summary>
    public Int32 OperatorReportingId { get; set; }
    /// <summary>The product value.</summary>
    public String Product { get; set; }
    /// <summary>Unique identifier of the product.</summary>
    public Guid ProductId { get; set; }
    /// <summary>The product reporting id value.</summary>
    public Int32 ProductReportingId { get; set; }
    /// <summary>The type value.</summary>
    public String Type { get; set; }
    /// <summary>The status value.</summary>
    public String Status { get; set; }
    /// <summary>The value value.</summary>
    public Decimal Value { get; set; }
    /// <summary>The total fees value.</summary>
    public Decimal TotalFees { get; set; }
    /// <summary>The settlement reference value.</summary>
    public String SettlementReference { get; set; }
    /// <summary>The transaction number value.</summary>
    public Int32 TransactionNumber { get; set; }
}
