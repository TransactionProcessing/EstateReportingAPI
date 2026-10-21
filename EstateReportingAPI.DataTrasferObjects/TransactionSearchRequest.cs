using System;
using System.Collections.Generic;

namespace EstateReportingAPI.DataTransferObjects;

/// <summary>Request criteria for transaction search.</summary>

public class TransactionSearchRequest{

    /// <summary>Optional operator reporting identifiers to include.</summary>

    public List<Int32>? Operators{ get; set; }

    /// <summary>Optional inclusive transaction value range.</summary>

    public ValueRange? ValueRange{ get; set; }

    /// <summary>Optional merchant reporting identifiers to include.</summary>

    public List<Int32>? Merchants{ get; set; }
        
    /// <summary>The query date value.</summary>
        
    public DateTime QueryDate{ get; set; }

    /// <summary>The response code value.</summary>

    public String? ResponseCode{ get; set; }

    /// <summary>The auth code value.</summary>

    public String? AuthCode{ get; set; }

    /// <summary>The transaction number value.</summary>

    public String? TransactionNumber{ get; set; }
}