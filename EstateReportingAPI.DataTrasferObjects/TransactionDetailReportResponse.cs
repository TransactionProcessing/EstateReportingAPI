using System;
using System.Collections.Generic;

namespace EstateReportingAPI.DataTransferObjects;

/// <summary>Response data for transaction detail report.</summary>

public class TransactionDetailReportResponse
{
    /// <summary>The transactions value.</summary>
    public List<TransactionDetail> Transactions { get; set; }
    /// <summary>The summary value.</summary>
    public TransactionDetailSummary Summary { get; set; }
    /// <summary>The pagination value.</summary>
    public PaginationMetadata Pagination { get; set; } = new();
}

/// <summary>Response data for product performance.</summary>

public class ProductPerformanceResponse {
    /// <summary>The product details value.</summary>
    public List<ProductPerformanceDetail> ProductDetails { get; set; }
    /// <summary>The summary value.</summary>
    public ProductPerformanceSummary Summary { get; set; }
    /// <summary>The pagination value.</summary>
    public PaginationMetadata Pagination { get; set; } = new();
}

/// <summary>API data model describing product performance detail information.</summary>

public class ProductPerformanceDetail {
    /// <summary>The product name value.</summary>
    public String ProductName { get; set; }
    /// <summary>Unique identifier of the product.</summary>
    public Guid ProductId { get; set; }
    /// <summary>The product reporting id value.</summary>
    public Int32 ProductReportingId { get; set; }
    /// <summary>Unique identifier of the contract.</summary>
    public Guid ContractId { get; set; }
    /// <summary>The contract reporting id value.</summary>
    public Int32 ContractReportingId { get; set; }
    /// <summary>Number of transactions represented by this result.</summary>
    public Int32 TransactionCount { get; set; }
    /// <summary>Total monetary value of the transactions represented by this result.</summary>
    public Decimal TransactionValue { get; set; }
    /// <summary>The percentage of total value.</summary>
    public Decimal PercentageOfTotal { get; set; }
}

/// <summary>API data model describing product performance summary information.</summary>

public class ProductPerformanceSummary {
    /// <summary>The total products value.</summary>
    public Int32 TotalProducts { get; set; }
    /// <summary>The total count value.</summary>
    public Int32 TotalCount { get; set; }
    /// <summary>Total monetary value represented by this result.</summary>
    public Decimal TotalValue { get; set; }
    /// <summary>The average per product value.</summary>
    public Decimal AveragePerProduct { get; set; }
}
