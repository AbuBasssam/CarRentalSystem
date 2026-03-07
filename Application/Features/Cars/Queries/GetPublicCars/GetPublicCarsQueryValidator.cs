using Application.Validations;
using FluentValidation;

namespace Application.Features.Cars;

public class GetPublicCarsQueryValidator : CursorPaginationValidator<GetPublicCarsQuery>
{


    public GetPublicCarsQueryValidator()
    {
        // 1. Brand & Model Validation
        RuleFor(x => x.Filters.Brand)
            .MaximumLength(50).WithMessage("Brand name cannot exceed 50 characters.")
            .When(x => x.Filters.Brand is not null);

        RuleFor(x => x.Filters.Model)
            .MaximumLength(50).WithMessage("Model name cannot exceed 50 characters.")
            .When(x => x.Filters.Model is not null);

        // 2. Daily Rate Validation (Cross-property validation)
        RuleFor(x => x.Filters.MinDailyRate)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum daily rate cannot be negative.")
            .When(x => x.Filters.MinDailyRate.HasValue);

        RuleFor(x => x.Filters.MaxDailyRate)
            .GreaterThanOrEqualTo(0).WithMessage("Maximum daily rate cannot be negative.")
            .Must((query, maxRate) => !query.Filters.MinDailyRate.HasValue || maxRate >= query.Filters.MinDailyRate)
            .WithMessage("Maximum daily rate must be greater than or equal to minimum daily rate.")
            .When(x => x.Filters.MaxDailyRate.HasValue);

    }
}