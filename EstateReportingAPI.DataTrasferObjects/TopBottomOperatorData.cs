namespace EstateReportingAPI.DataTransferObjects{
    using System;

    /// <summary>API data model describing top bottom operator data information.</summary>

    public class TopBottomOperatorData
    {
        /// <summary>The operator name value.</summary>
        public String OperatorName { get; set; }
        /// <summary>The sales value value.</summary>
        public Decimal SalesValue { get; set; }
    }
}