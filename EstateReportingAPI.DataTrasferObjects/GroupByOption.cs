namespace EstateReportingAPI.DataTransferObjects;

/// <summary>API data model describing group by option information.</summary>

public enum GroupByOption{
    /// <summary>Group transactions by operator.</summary>
    Operator,
    /// <summary>Group transactions by merchant.</summary>
    Merchant,
    /// <summary>Group transactions by product.</summary>
    Product
}
