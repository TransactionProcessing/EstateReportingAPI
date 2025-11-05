namespace EstateReportingAPI.DataTrasferObjects{
    using Newtonsoft.Json;
    using System;

    public class Merchant{
        #region Properties

        [JsonProperty("created_date_time")]
        public DateTime CreatedDateTime{ get; set; }
        [JsonProperty("estate_reporting_id")]
        public Int32 EstateReportingId{ get; set; }
        [JsonProperty("last_sale")]
        public DateTime LastSale{ get; set; }
        [JsonProperty("last_sale_date_time")]
        public DateTime LastSaleDateTime{ get; set; }
        [JsonProperty("last_statement")]
        public DateTime LastStatement{ get; set; }
        [JsonProperty("merchant_id")]
        public Guid MerchantId{ get; set; }
        [JsonProperty("merchant_reporting_id")]
        public Int32 MerchantReportingId{ get; set; }
        [JsonProperty("name")]
        public String Name{ get; set; }
        [JsonProperty("post_code")]
        public String PostCode{ get; set; }
        [JsonProperty("reference")]
        public String Reference{ get; set; }
        [JsonProperty("region")]
        public String Region{ get; set; }
        [JsonProperty("town")]
        public String Town{ get; set; }

        #endregion
    }
}