namespace EstateReportingAPI.DataTransferObjects;

/// <summary>API data model describing pagination metadata information.</summary>

public class PaginationMetadata
{
    /// <summary>One-based page number to return.</summary>
    public int PageNumber { get; set; }
    /// <summary>Maximum number of items to include on a page.</summary>
    public int PageSize { get; set; }
    /// <summary>Total number of matching items across all pages.</summary>
    public int TotalItems { get; set; }
    /// <summary>Total number of pages for the result set.</summary>
    public int TotalPages { get; set; }
}
