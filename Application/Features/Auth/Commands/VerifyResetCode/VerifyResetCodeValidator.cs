using ApplicationLayer.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Application.Features.AuthFeature;

public class VerifyResetCodeValidator : AbstractValidator<VerifyResetCodeCommand>
{
    #region Field(s)

    private readonly IStringLocalizer<SharedResources> _Localizer;

    #endregion

    #region Constructor(s)
    public VerifyResetCodeValidator(IStringLocalizer<SharedResources> localizer)
    {
        _Localizer = localizer;

        _ApplyValidations();
    }
    #endregion

    #region Method(s)
    private void _ApplyValidations()
    {
        RuleFor(x => x.DTO)
            .NotNull().WithMessage(_Localizer[SharedResourcesKeys.RequestPayloadRequired])
            .SetValidator(new VerificationDTO.Validator(_Localizer));

    }
    #endregion

}