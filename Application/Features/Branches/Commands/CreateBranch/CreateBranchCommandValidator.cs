using ApplicationLayer.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Application.Features.Branches;

public class CreateBranchCommandValidator : AbstractValidator<CreateBranchCommand>
{
    public CreateBranchCommandValidator(IStringLocalizer<SharedResources> localizer)
    {
        RuleFor(x => x.Dto)
            .NotNull().WithMessage(localizer[SharedResourcesKeys.RequestPayloadRequired])
            .SetValidator(new CreateBranchDTO.Validator(localizer));
    }
}
