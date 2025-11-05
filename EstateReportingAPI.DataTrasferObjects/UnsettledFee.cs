using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace EstateReportingAPI.DataTransferObjects
{
    public class UnsettledFee
    {
        [JsonProperty("dimension_name")]
        public String DimensionName { get; set; }
        [JsonProperty("fees_value")]
        public Decimal FeesValue { get; set; }
        [JsonProperty("fees_count")]
        public Int32 FeesCount { get; set; }
    }
}
