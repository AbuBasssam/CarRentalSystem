namespace Application.Abstracts;
/// <summary>Base query for all cursor-paginated listings.</summary>
public abstract class CursorPaginationQuery
{
    /// <summary>Last ID from previous page. Null for first page.</summary>
    public int? Cursor { get; set; }

    public int PageSize { get; set; }
}
