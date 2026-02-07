using System.Net;

namespace Application.Models;

public class Response<T>
{
    public HttpStatusCode StatusCode { get; set; }
    public object? Meta { get; set; }
    public bool Succeeded { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; } = null!;

    /// <summary>
    /// Field-specific validation errors
    /// Key: field name, Value: error message
    /// </summary>
    public Dictionary<string, string>? ValidationErrors { get; set; }
    public T Data { get; set; } = default!;


}
