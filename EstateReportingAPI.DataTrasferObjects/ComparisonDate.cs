namespace EstateReportingAPI.DataTrasferObjects{
    using System;

    /// <summary>API data model describing comparison date information.</summary>

    public class ComparisonDate{
        /// <summary>The order value value.</summary>
        public Int32 OrderValue{ get; set; }
        /// <summary>The date value.</summary>
        public DateOnly Date { get; set; }
        /// <summary>The description value.</summary>
        public String Description { get; set; }
    }
}
