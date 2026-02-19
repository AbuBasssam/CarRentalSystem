using Application.Abstracts;
using FluentValidation;

namespace Application.Validations;

public abstract class LocalizePaginationValidator<T> : AbstractValidator<T> where T : LocalizePaginationQuery
{
    protected LocalizePaginationValidator()
    {


        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage("Page number must be greater than 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 30)
            .WithMessage("Page size must be between 1 and 30.");
    }
}
