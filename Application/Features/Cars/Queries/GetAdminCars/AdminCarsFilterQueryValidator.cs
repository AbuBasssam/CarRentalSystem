using Application.Validations;
using FluentValidation;

namespace Application.Features.Cars;

public class AdminCarsFilterQueryValidator : CursorPaginationValidator<GetAdminCarsQuery>
{


    public AdminCarsFilterQueryValidator()
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

        // 3. Enum Validation

        RuleFor(x => x.Filters.TransmissionType)
            .InclusiveBetween(1, 2)
            .When(x => x.Filters.TransmissionType.HasValue);

        RuleFor(x => x.Filters.FuelType)
            .InclusiveBetween(1, 4)
            .When(x => x.Filters.FuelType.HasValue);

        RuleFor(x => x.Filters.FleetConditionStatus)
            .InclusiveBetween(1, 3)
            .When(x => x.Filters.FleetConditionStatus.HasValue);
    }
}