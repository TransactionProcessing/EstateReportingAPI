using Newtonsoft.Json;

namespace EstateReportingAPI.DataTransferObjects{
    using System;

    public class Operator{
        [JsonProperty("estate_reporting_id")]
        public Int32 EstateReportingId { get; set; }
        [JsonProperty("operator_id")]
        public Guid OperatorId{ get; set; }
        [JsonProperty("name")]
        public String Name { get; set; }
        [JsonProperty("operator_reporting_id")]
        public Int32 OperatorReportingId { get; set; }
        [JsonProperty("require_custom_merchant_number")]
        public Boolean RequireCustomMerchantNumber { get; set; }
        [JsonProperty("require_custom_terminal_number")]
        public Boolean RequireCustomTerminalNumber { get; set; }
    }
}