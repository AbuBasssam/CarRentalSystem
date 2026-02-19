namespace Application.Models;

public class PaginatedResult<T> where T : class
{
    public bool Succeeded { get; set; }

    public int CurrentPage { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }

    public bool HasPreviousPage => CurrentPage > 1;

    public bool HasNextPage => CurrentPage < TotalPages;

    public List<string> Messages { get; set; } = new();

    public List<T> Data { get; set; }

    private PaginatedResult(bool succeeded, List<T> data = null!, int count = 0, int page = 1, int pageSize = 10)
    {
        Data = data;
        CurrentPage = page;
        Succeeded = succeeded;
        PageSize = pageSize;
        TotalPages = count > 0 ? (int)Math.Ceiling(count / (double)pageSize) : 0;
        TotalCount = count;
    }

    public static PaginatedResult<T> Success(List<T> data, int totalCount, int page, int pageSize)
    {
        return new PaginatedResult<T>(true, data, totalCount, page, pageSize);
    }

}