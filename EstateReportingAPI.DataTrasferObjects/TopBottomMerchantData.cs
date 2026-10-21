namespace EstateReportingAPI.DataTransferObjects{
    using System;

    /// <summary>API data model describing top bottom merchant data information.</summary>

    public class TopBottomMerchantData
    {
        /// <summary>The merchant name value.</summary>
        public String MerchantName { get; set; }
        /// <summary>The sales value value.</summary>
        public Decimal SalesValue { get; set; }
    }
}