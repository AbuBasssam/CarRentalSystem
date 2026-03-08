using ApplicationLayer.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Application.Features.Cars;

public class CreateCarCommandValidator : AbstractValidator<CreateCarCommand>
{
    public CreateCarCommandValidator(IStringLocalizer<SharedResources> localizer)
    {
        RuleFor(x => x.Dto)
            .NotNull().WithMessage(localizer[SharedResourcesKeys.RequestPayloadRequired])
            .SetValidator(new CreateCarDto.Validator(localizer));
    }
}