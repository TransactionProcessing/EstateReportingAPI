using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace EstateReportingAPI.DataTransferObjects;

public class Contract
{
    #region Properties
    [JsonProperty("estate_id")]
    public Guid EstateId { get; set; }
    [JsonProperty("estate_reporting_id")]
    public Int32 EstateReportingId { get; set; }
    [JsonProperty("contract_id")]
    public Guid ContractId { get; set; }
    [JsonProperty("contract_reporting_id")]
    public Int32 ContractReportingId { get; set; }
    [JsonProperty("description")]
    public String Description { get; set; }
    [JsonProperty("operator_name")]
    public String OperatorName { get; set; }
    [JsonProperty("operator_id")]
    public Guid OperatorId { get; set; }
    [JsonProperty("operator_reporting_id")]
    public Int32 OperatorReportingId { get; set; }
    [JsonProperty("products")]
    public List<ContractProduct> Products { get; set; }
    #endregion
}

public class ContractProduct
{
    [JsonProperty("contract_id")]
    public Guid ContractId { get; set; }
    [JsonProperty("product_id")]
    public Guid ProductId { get; set; }
    [JsonProperty("product_name")]
    public String ProductName { get; set; }
    [JsonProperty("display_text")]
    public String DisplayText { get; set; }
    [JsonProperty("product_type")]
    public Int32 ProductType { get; set; }
    [JsonProperty("value")]
    public Decimal? Value { get; set; }
    [JsonProperty("transaction_fees")]
    public List<ContractProductTransactionFee> TransactionFees { get; set; }
}

public class ContractProductTransactionFee {
    [JsonProperty("transaction_fee_id")] 
    public Guid TransactionFeeId { get; set; }
    [JsonProperty("description")] 
    public string? Description { get; set; }
    [JsonProperty("calculation_type")] 
    public Int32 CalculationType { get; set; }
    [JsonProperty("fee_type")] 
    public Int32 FeeType { get; set; }
    [JsonProperty("value")] 
    public Decimal Value { get; set; }
}


public class Estate
{
    [JsonProperty("estate_id")]
    public Guid EstateId { get; set; }
    [JsonProperty("estate_name")]
    public string? EstateName { get; set; }
    [JsonProperty("reference")]
    public string? Reference { get; set; }
    [JsonProperty("operators")]
    public List<EstateOperator>? Operators { get; set; }
    [JsonProperty("merchants")]
    public List<EstateMerchant>? Merchants { get; set; }
    [JsonProperty("contracts")]
    public List<EstateContract>? Contracts { get; set; }
    [JsonProperty("users")]
    public List<EstateUser>? Users { get; set; }
}

public class EstateUser
{
    [JsonProperty("user_id")]
    public Guid UserId { get; set; }
    [JsonProperty("email_address")]
    public string? EmailAddress { get; set; }
    [JsonProperty("created_date_time")]
    public DateTime CreatedDateTime { get; set; }
}

public class EstateOperator
{
    [JsonProperty("operator_id")]
    public Guid OperatorId { get; set; }
    [JsonProperty("name")]
    public string? Name { get; set; }
    [JsonProperty("require_custom_merchant_number")]
    public bool RequireCustomMerchantNumber { get; set; }
    [JsonProperty("require_custom_terminal_number")]
    public bool RequireCustomTerminalNumber { get; set; }
    [JsonProperty("created_date_time")]
    public DateTime CreatedDateTime { get; set; }
}

public class EstateContract
{
    [JsonProperty("operator_id")]
    public Guid OperatorId { get; set; }
    [JsonProperty("contract_id")]
    public Guid ContractId { get; set; }
    [JsonProperty("name")]
    public string? Name { get; set; }
    [JsonProperty("operator_name")]
    public string? OperatorName { get; set; }
}


public class EstateMerchant
{
    [JsonProperty("merchant_id")]
    public Guid MerchantId { get; set; }
    [JsonProperty("name")]
    public string? Name { get; set; }
    [JsonProperty("reference")]
    public string? Reference { get; set; }
}