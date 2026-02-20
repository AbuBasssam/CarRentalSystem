using ApplicationLayer.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Application.Features.Branches;

public class UpdateBranchCommandValidator : AbstractValidator<UpdateBranchCommand>
{
    public UpdateBranchCommandValidator(IStringLocalizer<SharedResources> localizer)
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage(localizer[SharedResourcesKeys.InvalidId]);

        RuleFor(x => x.Dto)
            .NotNull().WithMessage(localizer[SharedResourcesKeys.RequestPayloadRequired])
            .SetValidator(new UpdateBranchDTO.Validator(localizer));
    }
}
