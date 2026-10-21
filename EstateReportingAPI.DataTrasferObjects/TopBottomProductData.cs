namespace EstateReportingAPI.DataTransferObjects{
    using System;

    /// <summary>API data model describing top bottom product data information.</summary>

    public class TopBottomProductData
    {
        /// <summary>The product name value.</summary>
        public String ProductName { get; set; }
        /// <summary>The sales value value.</summary>
        public Decimal SalesValue { get; set; }
    }
}