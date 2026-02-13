using System;
using Newtonsoft.Json;

namespace EstateReportingAPI.DataTransferObjects;

public class ValueRange{
    [JsonProperty("start_value")]
    public Decimal StartValue{ get; set; }
    [JsonProperty("end_value")]
    public Decimal EndValue{ get; set; }
}