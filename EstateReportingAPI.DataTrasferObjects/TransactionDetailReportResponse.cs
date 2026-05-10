using System;
using System.Collections.Generic;

namespace EstateReportingAPI.DataTransferObjects;

public class TransactionDetailReportResponse
{
    public List<TransactionDetail> Transactions { get; set; }
    public TransactionDetailSummary Summary { get; set; }
}

public class ProductPerformanceResponse {
    public List<ProductPerformanceDetail> ProductDetails { get; set; }
    public ProductPerformanceSummary Summary { get; set; }
}

public class ProductPerformanceDetail {
    public String ProductName { get; set; }
    public Guid ProductId { get; set; }
    public Int32 ProductReportingId { get; set; }
    public Guid ContractId { get; set; }
    public Int32 ContractReportingId { get; set; }
    public Int32 TransactionCount { get; set; }
    public Decimal TransactionValue { get; set; }
    public Decimal PercentageOfTotal { get; set; }
}

public class ProductPerformanceSummary {
    public Int32 TotalProducts { get; set; }
    public Int32 TotalCount { get; set; }
    public Decimal TotalValue { get; set; }
    public Decimal AveragePerProduct { get; set; }
}