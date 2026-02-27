namespace Application.Abstracts;

/// <summary>
/// Base class for all paginated + sorted queries.
/// Inherit this in every feature's FilterQuery.
/// </summary>
public abstract class FilterQuery : PaginationQuery
{


    // --- Sorting ---
    // Usage: ?sortBy=name&sortDir=desc
    public string? SortBy { get; set; }
    public string SortDir { get; set; } = "asc";

    public bool IsDescending() => SortDir.Equals("desc", StringComparison.OrdinalIgnoreCase);
}
