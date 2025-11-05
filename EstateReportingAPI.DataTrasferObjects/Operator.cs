using Newtonsoft.Json;

namespace EstateReportingAPI.DataTrasferObjects{
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
    }
}