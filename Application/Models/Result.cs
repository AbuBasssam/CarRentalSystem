namespace Application.Models;

public class Result<T>
{
    public bool IsSuccess { get; }
    public T Data { get; }
    public List<string> Errors { get; }

    private Result(bool isSuccess, T value, List<string> errors)
    {
        IsSuccess = isSuccess;
        Data = value;
        Errors = errors;
    }

    public static Result<T> Success(T value) => new Result<T>(true, value, []);
    public static Result<T> Failure(List<string> errors) => new Result<T>(false, default!, errors);
}
