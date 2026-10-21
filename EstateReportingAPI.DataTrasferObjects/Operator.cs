namespace EstateReportingAPI.DataTransferObjects{
    using System;

    /// <summary>API data model describing operator information.</summary>

    public class Operator{
        /// <summary>The estate reporting id value.</summary>
        public Int32 EstateReportingId { get; set; }
        /// <summary>Unique identifier of the operator.</summary>
        public Guid OperatorId{ get; set; }
        /// <summary>The name value.</summary>
        public String Name { get; set; }
        /// <summary>The operator reporting id value.</summary>
        public Int32 OperatorReportingId { get; set; }
        /// <summary>The require custom merchant number value.</summary>
        public Boolean RequireCustomMerchantNumber { get; set; }
        /// <summary>The require custom terminal number value.</summary>
        public Boolean RequireCustomTerminalNumber { get; set; }
    }
}