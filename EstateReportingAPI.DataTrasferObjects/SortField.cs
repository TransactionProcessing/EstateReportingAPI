namespace EstateReportingAPI.DataTransferObjects;

/// <summary>API data model describing sort field information.</summary>

public enum SortField{
    /// <summary>Sort by transaction amount.</summary>
    TransactionAmount = 1,

    /// <summary>Sort by merchant name.</summary>
    MerchantName = 2,

    /// <summary>Sort by operator name.</summary>
    OperatorName = 3
}
