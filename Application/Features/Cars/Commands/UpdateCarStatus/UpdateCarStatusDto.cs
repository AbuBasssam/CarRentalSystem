using Domain.Enums;
using FluentValidation;

namespace Application.Features.Cars;

public class UpdateCarStatusDto
{
    public bool? IsActive { get; set; }
    public enFleetConditionStatus? FleetConditionStatus { get; set; }

    public class Validator : AbstractValidator<UpdateCarStatusDto>
    {
        public Validator()
        {
            string strFleetConditionStatus = Helpers.FormatEnumComment<enFleetConditionStatus>();
            RuleFor(x => x)
                .Must(x => x.IsActive.HasValue || x.FleetConditionStatus.HasValue)
                .WithMessage("At least one of IsActive or FleetConditionStatus must be provided.");

            RuleFor(x => x.FleetConditionStatus)
                .IsInEnum()
                .When(x => x.FleetConditionStatus.HasValue)
                .WithMessage($"FleetConditionStatus must be {strFleetConditionStatus}.");
        }
    }
}
