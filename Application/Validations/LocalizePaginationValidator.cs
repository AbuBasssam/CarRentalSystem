using Application.Abstracts;
using FluentValidation;

namespace Application.Validations;

public abstract class LocalizePaginationValidator<T> : AbstractValidator<T> where T : LocalizePaginationQuery
{
    protected LocalizePaginationValidator()
    {
        RuleFor(x => x.Lang)
            .NotEmpty().WithMessage("Language is required")
            .Must(l => l == "ar" || l == "en")
            .WithMessage("Language must be 'ar' or 'en'.");

        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage("Page number must be greater than 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100.");
    }
}
