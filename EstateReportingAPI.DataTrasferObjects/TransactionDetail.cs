using System;
using Newtonsoft.Json;

namespace EstateReportingAPI.DataTransferObjects;

public class TransactionDetail
{
    [JsonProperty("id")]
    public Guid Id { get; set; }
    [JsonProperty("date_time")]
    public DateTime DateTime { get; set; }
    [JsonProperty("merchant")]
    public String Merchant { get; set; }
    [JsonProperty("merchant_id")]
    public Guid MerchantId { get; set; }
    [JsonProperty("merchant_reporting_id")]
    public Int32 MerchantReportingId { get; set; }
    [JsonProperty("operator")]
    public String Operator { get; set; }
    [JsonProperty("operator_id")]
    public Guid OperatorId { get; set; }
    [JsonProperty("operator_reporting_id")]
    public Int32 OperatorReportingId { get; set; }
    [JsonProperty("product")]
    public String Product { get; set; }
    [JsonProperty("product_id")]
    public Guid ProductId { get; set; }
    [JsonProperty("product_reporting_id")]
    public Int32 ProductReportingId { get; set; }
    [JsonProperty("type")]
    public String Type { get; set; }
    [JsonProperty("status")]
    public String Status { get; set; }
    [JsonProperty("value")]
    public Decimal Value { get; set; }
    [JsonProperty("total_fees")]
    public Decimal TotalFees { get; set; }
    [JsonProperty("settlement_reference")]
    public String SettlementReference { get; set; }
}