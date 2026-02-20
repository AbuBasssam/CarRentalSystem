using Application.Abstracts;
using FluentValidation;

namespace Application.Validations;

public abstract class FilterQueryValidator<T> : PaginationValidator<T> where T : FilterQuery
{
    protected abstract IReadOnlyCollection<string> AllowedSortFields { get; }

    protected FilterQueryValidator()
    {
        RuleFor(x => x.SortDir)
            .Must(s => s.ToLower() is "asc" or "desc")
            .WithMessage("sortDir must be 'asc' or 'desc'.");

        RuleFor(x => x.SortBy)
            .Must(s => s is null || AllowedSortFields.Contains(s.ToLower()))
            .WithMessage(x =>
                $"sortBy '{x.SortBy}' is not valid. Allowed values: {string.Join(", ", AllowedSortFields)}.");
    }
}