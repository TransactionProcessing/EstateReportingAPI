namespace EstateReportingAPI.DataTransferObjects{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;

    public class Merchant{
        #region Properties

        [JsonProperty("merchant_id")]
        public Guid MerchantId{ get; set; }
        [JsonProperty("merchant_reporting_id")]
        public Int32 MerchantReportingId{ get; set; }
        [JsonProperty("name")]
        public String Name{ get; set; }
        [JsonProperty("reference")]
        public String Reference { get; set; }
        [JsonProperty("balance")]
        public Decimal Balance { get; set; }
        [JsonProperty("settlement_schedule")]
        public Int32 SettlementSchedule { get; set; }
        [JsonProperty("created_date_time")]
        public DateTime CreatedDateTime { get; set; }

        [JsonProperty("address_id")]
        public Guid AddressId { get; set; }
        [JsonProperty("address_line1")]
        public String AddressLine1 { get; set; }
        [JsonProperty("address_line2")]
        public String AddressLine2 { get; set; }
        [JsonProperty("town")]
        public String Town { get; set; }
        [JsonProperty("region")] 
        public String Region { get; set; }
        [JsonProperty("post_code")]
        public String PostCode{ get; set; }
        [JsonProperty("country")]
        public String Country { get; set; }

        [JsonProperty("contact_id")]
        public Guid ContactId { get; set; }
        [JsonProperty("contact_name")]
        public String ContactName { get; set; }
        [JsonProperty("contact_email")]
        public String ContactEmail { get; set; }
        [JsonProperty("contact_phone")]
        public String ContactPhone { get; set; }


        #endregion
    }

    public class MerchantOperator
    {
        [JsonProperty("merchant_id")]
        public Guid MerchantId { get; set; }
        [JsonProperty("operator_id")]
        public Guid OperatorId { get; set; }
        [JsonProperty("operator_name")]
        public String OperatorName { get; set; }
        [JsonProperty("merchant_number")]
        public String MerchantNumber { get; set; }
        [JsonProperty("terminal_number")]
        public String TerminalNumber { get; set; }
        [JsonProperty("is_deleted")]
        public Boolean IsDeleted { get; set; }
    }

    public class MerchantContract
    {
        [JsonProperty("merchant_id")]
        public Guid MerchantId { get; set; }
        [JsonProperty("contract_id")]
        public Guid ContractId { get; set; }
        [JsonProperty("contract_name")]
        public String ContractName { get; set; }
        [JsonProperty("operator_name")]
        public String OperatorName { get; set; }
        [JsonProperty("is_deleted")]
        public Boolean IsDeleted { get; set; }
        [JsonProperty("contract_products")]
        public List<MerchantContractProduct> ContractProducts { get; set; }
    }

    public class MerchantContractProduct
    {
        [JsonProperty("merchant_id")]
        public Guid MerchantId { get; set; }
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
    }

    public class MerchantDevice
    {
        [JsonProperty("merchant_id")]
        public Guid MerchantId { get; set; }
        [JsonProperty("device_id")]
        public Guid DeviceId { get; set; }
        [JsonProperty("device_identifier")]
        public String DeviceIdentifier { get; set; }
        [JsonProperty("is_deleted")]
        public Boolean IsDeleted { get; set; }
    }
}