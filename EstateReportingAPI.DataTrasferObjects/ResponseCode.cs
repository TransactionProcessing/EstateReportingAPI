namespace EstateReportingAPI.DataTrasferObjects{
    using Newtonsoft.Json;
    using System;

    public class ResponseCode{
        [JsonProperty("code")]
        public Int32 Code { get; set; }
        [JsonProperty("description")]
        public String Description { get; set; }
    }
}