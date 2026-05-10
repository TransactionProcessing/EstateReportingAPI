using System;
using System.Collections.Generic;

namespace EstateReportingAPI.DataTransferObjects;

public class TransactionSearchRequest{

    public List<Int32>? Operators{ get; set; }

    public ValueRange? ValueRange{ get; set; }

    public List<Int32>? Merchants{ get; set; }
        
    public DateTime QueryDate{ get; set; }

    public String? ResponseCode{ get; set; }

    public String? AuthCode{ get; set; }

    public String? TransactionNumber{ get; set; }
}