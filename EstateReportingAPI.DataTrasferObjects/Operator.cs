namespace EstateReportingAPI.DataTrasferObjects{
    using System;

    public class Operator{
        public Int32 EstateReportingId { get; set; }
        public Guid OperatorId{ get; set; }
        public String Name { get; set; }
    }
}