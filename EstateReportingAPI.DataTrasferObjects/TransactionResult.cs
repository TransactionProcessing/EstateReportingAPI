using System;

namespace EstateReportingAPI.DataTransferObjects{

    /// <summary>API data model describing transaction result information.</summary>

    public class TransactionResult{
        /// <summary>The transaction amount value.</summary>
        public Decimal TransactionAmount{ get; set; }
        /// <summary>The transaction id value.</summary>
        public Guid TransactionId{ get; set; }
        /// <summary>The transaction reporting id value.</summary>
        public Int32 TransactionReportingId{ get; set; }
        /// <summary>The is authorised value.</summary>
        public Boolean IsAuthorised{ get; set; }
        /// <summary>The response code value.</summary>
        public String ResponseCode{ get; set; }
        /// <summary>The response message value.</summary>
        public String ResponseMessage{ get; set; }
        /// <summary>Date and time of the transaction.</summary>
        public DateTime TransactionDateTime{ get; set; }
        /// <summary>The transaction source value.</summary>
        public String TransactionSource{ get; set; }
        /// <summary>The operator name value.</summary>
        public String OperatorName{ get; set; }
        /// <summary>The operator reporting id value.</summary>
        public Int32 OperatorReportingId{ get; set; }
        /// <summary>The product value.</summary>
        public String Product{ get; set; }
        /// <summary>The product reporting id value.</summary>
        public Int32 ProductReportingId{ get; set; }
        /// <summary>The merchant name value.</summary>
        public String MerchantName{ get; set; }
        /// <summary>Numeric reporting identifier used to filter or identify a merchant.</summary>
        public Int32 MerchantReportingId{ get; set; }
    }
}
