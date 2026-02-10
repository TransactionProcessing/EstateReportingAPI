using System;
using System.Collections.Generic;
using System.Text;

namespace EstateReportingAPI.DataTransferObjects{
    using Newtonsoft.Json;

    public class TransactionDetailReportRequest
    {
        [JsonProperty("operators")]
        public List<Int32>? Operators { get; set; }
        [JsonProperty("merchants")]
        public List<Int32>? Merchants { get; set; }
        [JsonProperty("products")]
        public List<Int32>? Products { get; set; }
        [JsonProperty("start_date")]
        public DateTime StartDate { get; set; }
        [JsonProperty("end_date")]
        public DateTime EndDate { get; set; }
    }

    public class TransactionResult{
        [JsonProperty("transaction_amount")]
        public Decimal TransactionAmount{ get; set; }
        [JsonProperty("transaction_id")]
        public Guid TransactionId{ get; set; }
        [JsonProperty("transaction_reporting_id")]
        public Int32 TransactionReportingId{ get; set; }
        [JsonProperty("is_authorised")]
        public Boolean IsAuthorised{ get; set; }
        [JsonProperty("response_code")]
        public String ResponseCode{ get; set; }
        [JsonProperty("response_message")]
        public String ResponseMessage{ get; set; }
        [JsonProperty("transaction_date_time")]
        public DateTime TransactionDateTime{ get; set; }
        [JsonProperty("transaction_source")]
        public String TransactionSource{ get; set; }
        [JsonProperty("operator_name")]
        public String OperatorName{ get; set; }
        [JsonProperty("operator_reporting_id")]
        public Int32 OperatorReportingId{ get; set; }
        [JsonProperty("product")]
        public String Product{ get; set; }
        [JsonProperty("product_reporting_id")]
        public Int32 ProductReportingId{ get; set; }
        [JsonProperty("merchant_name")]
        public String MerchantName{ get; set; }
        [JsonProperty("merchant_reporting_id")]
        public Int32 MerchantReportingId{ get; set; }
    }

    public class TransactionSearchRequest{

        [JsonProperty("operators")]
        public List<Int32>? Operators{ get; set; }

        [JsonProperty("value_range")]
        public ValueRange? ValueRange{ get; set; }

        [JsonProperty("merchants")]
        public List<Int32>? Merchants{ get; set; }
        
        [JsonProperty("query_date")]
        public DateTime QueryDate{ get; set; }

        [JsonProperty("response_code")]
        public String? ResponseCode{ get; set; }

        [JsonProperty("auth_code")]
        public String? AuthCode{ get; set; }

        [JsonProperty("transaction_number")]
        public String? TransactionNumber{ get; set; }
    }

    public class ValueRange{
        [JsonProperty("start_value")]
        public Decimal StartValue{ get; set; }
        [JsonProperty("end_value")]
        public Decimal EndValue{ get; set; }
    }

    public enum SortField{
        TransactionAmount = 1,

        MerchantName = 2,

        OperatorName = 3
    }

    public enum SortDirection{
        Ascending,

        Descending,
    }

    public enum GroupByOption{
        Operator,
        Merchant,
        Product
    }

    public class TransactionDetailReportResponse
    {
        [JsonProperty("transactions")]
        public List<TransactionDetail> Transactions { get; set; }
        [JsonProperty("summary")]
        public TransactionDetailSummary Summary { get; set; }
    }

    public class TransactionDetailSummary
    {
        [JsonProperty("total_value")]
        public Decimal TotalValue { get; set; }
        [JsonProperty("total_fees")]
        public Decimal TotalFees { get; set; }
        [JsonProperty("transaction_count")]
        public Int32 TransactionCount { get; set; }
    }

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

}
