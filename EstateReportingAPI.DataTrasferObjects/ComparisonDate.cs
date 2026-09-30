namespace EstateReportingAPI.DataTrasferObjects{
    using System;

    public class ComparisonDate{
        public Int32 OrderValue{ get; set; }
        public DateOnly Date { get; set; }
        public String Description { get; set; }
    }
}
