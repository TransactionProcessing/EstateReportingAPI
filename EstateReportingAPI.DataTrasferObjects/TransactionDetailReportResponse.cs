using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace EstateReportingAPI.DataTransferObjects;

public class TransactionDetailReportResponse
{
    [JsonProperty("transactions")]
    public List<TransactionDetail> Transactions { get; set; }
    [JsonProperty("summary")]
    public TransactionDetailSummary Summary { get; set; }
}

public class ProductPerformanceResponse {
    [JsonProperty("product_details")]
    public List<ProductPerformanceDetail> ProductDetails { get; set; }
    [JsonProperty("summary")]
    public ProductPerformanceSummary Summary { get; set; }
}

public class ProductPerformanceDetail {
    [JsonProperty("product_name")]
    public String ProductName { get; set; }
    [JsonProperty("product_id")]
    public Guid ProductId { get; set; }
    [JsonProperty("product_reporting_id")]
    public Int32 ProductReportingId { get; set; }
    [JsonProperty("contract_id")]
    public Guid ContractId { get; set; }
    [JsonProperty("contract_reporting_id")]
    public Int32 ContractReportingId { get; set; }
    [JsonProperty("transaction_count")]
    public Int32 TransactionCount { get; set; }
    [JsonProperty("transaction_value")]
    public Decimal TransactionValue { get; set; }
    [JsonProperty("percentage_of_total")]
    public Decimal PercentageOfTotal { get; set; }
}

public class ProductPerformanceSummary {
    [JsonProperty("total_products")]
    public Int32 TotalProducts { get; set; }
    [JsonProperty("total_count")]
    public Int32 TotalCount { get; set; }
    [JsonProperty("total_value")]
    public Decimal TotalValue { get; set; }
    [JsonProperty("average_per_product")]
    public Decimal AveragePerProduct { get; set; }
}