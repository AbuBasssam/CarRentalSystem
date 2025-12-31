using System.Net;

namespace Application.Models;

// The Builder class
public class ResponseBuilder<T>
{
    private readonly Response<T> _response;

    public ResponseBuilder()
    {
        _response = new Response<T>();
    }

    public ResponseBuilder<T> WithStatusCode(HttpStatusCode statusCode)
    {
        _response.StatusCode = statusCode;
        return this;
    }

    public ResponseBuilder<T> WithSuccess(bool succeeded)
    {
        _response.Succeeded = succeeded;
        return this;
    }

    public ResponseBuilder<T> WithMessage(string message)
    {
        _response.Message = message;
        return this;
    }

    public ResponseBuilder<T> WithData(T data)
    {
        _response.Data = data;
        return this;
    }

    public ResponseBuilder<T> WithMeta(object? meta)
    {
        _response.Meta = meta;
        return this;
    }

    public ResponseBuilder<T> WithErrors(List<string> errors)
    {
        _response.Errors = errors;
        return this;
    }

    public Response<T> Build()
    {
        return _response;
    }
}
