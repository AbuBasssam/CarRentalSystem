namespace Application.Models;

public class CursorPaginatedResult<T> where T : class
{

    /// <summary>
    /// Pass this as ?cursor= in the next request.
    /// Null means no more pages.
    /// </summary>
    public int? NextCursor { get; set; }

    public bool HasNextPage => NextCursor.HasValue;
    public int Count => Items.Count;
    public List<T> Items { get; set; } = new();

}