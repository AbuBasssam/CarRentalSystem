namespace Application.Abstracts;

public abstract class LocalizePaginationQuery
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public string Lang { get; set; } = "en";

}