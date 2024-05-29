using System;
using System.Collections.Generic;
using System.Text;

namespace EstateReportingAPI.DataTransferObjects{
    using Newtonsoft.Json;

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

}
