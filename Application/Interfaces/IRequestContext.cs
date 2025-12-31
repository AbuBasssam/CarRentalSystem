namespace Interfaces;

public interface IRequestContext:IScopedService
{
    string? AuthToken { get; }
    string? ClientIP { get; }
    string Language { get; }
}
