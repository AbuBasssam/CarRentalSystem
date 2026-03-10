using FluentValidation;

namespace Application.Features.Cars;

public class TransferCarDto
{
    public int ToBranchId { get; set; }
    public string? Reason { get; set; }

    public class Validator : AbstractValidator<TransferCarDto>
    {
        public Validator()
        {
            RuleFor(x => x.ToBranchId)
                .GreaterThan(0).WithMessage("ToBranchId is required.");

            RuleFor(x => x.Reason)
                .MaximumLength(250)
                .When(x => !string.IsNullOrEmpty(x.Reason))
                .WithMessage("Reason cannot exceed 500 characters.");
        }
    }
}
