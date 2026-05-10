using System;

namespace EstateReportingAPI.DataTransferObjects{

    public class TransactionResult{
        public Decimal TransactionAmount{ get; set; }
        public Guid TransactionId{ get; set; }
        public Int32 TransactionReportingId{ get; set; }
        public Boolean IsAuthorised{ get; set; }
        public String ResponseCode{ get; set; }
        public String ResponseMessage{ get; set; }
        public DateTime TransactionDateTime{ get; set; }
        public String TransactionSource{ get; set; }
        public String OperatorName{ get; set; }
        public Int32 OperatorReportingId{ get; set; }
        public String Product{ get; set; }
        public Int32 ProductReportingId{ get; set; }
        public String MerchantName{ get; set; }
        public Int32 MerchantReportingId{ get; set; }
    }
}
