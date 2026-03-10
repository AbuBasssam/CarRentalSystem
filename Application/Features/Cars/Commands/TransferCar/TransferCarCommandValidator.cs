using ApplicationLayer.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Application.Features.Cars;

public class TransferCarCommandValidator : AbstractValidator<TransferCarCommand>
{
    public TransferCarCommandValidator(IStringLocalizer<SharedResources> localizer)
    {
        RuleFor(x => x.Dto)
            .NotNull().WithMessage(localizer[SharedResourcesKeys.RequestPayloadRequired])
            .SetValidator(new TransferCarDto.Validator());
    }
}
