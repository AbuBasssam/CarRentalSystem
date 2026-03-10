using ApplicationLayer.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Application.Features.Cars;

public class UpdateCarStatusCommandValidator : AbstractValidator<UpdateCarStatusCommand>
{
    public UpdateCarStatusCommandValidator(IStringLocalizer<SharedResources> localizer)
    {
        RuleFor(x => x.Dto)
            .NotNull().WithMessage(localizer[SharedResourcesKeys.RequestPayloadRequired])
            .SetValidator(new UpdateCarStatusDto.Validator());
    }
}
