namespace EstateReportingAPI.DataTrasferObjects{
    using System;

    /// <summary>API data model describing response code information.</summary>

    public class ResponseCode{
        /// <summary>The code value.</summary>
        public Int32 Code { get; set; }
        /// <summary>The description value.</summary>
        public String Description { get; set; }
    }
}