using ApplicationLayer.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Application.Features.AuthFeature;

public class ResendVerificationCodeValidator : AbstractValidator<ResendVerificationCodeCommand>
{
    #region Field(s)

    private readonly IStringLocalizer<SharedResources> _Localizer;

    #endregion

    #region Constructor(s)

    public ResendVerificationCodeValidator(IStringLocalizer<SharedResources> Localizer)
    {
        _Localizer = Localizer;
        ApplyValidations();

    }
    #endregion

    #region Method(s)

    private void ApplyValidations()
    {
        RuleFor(x => x.DTO)
            .NotNull()
            .WithMessage(_Localizer[SharedResourcesKeys.RequestPayloadRequired])
            .SetValidator(new ResendCodeDTO.Validator(_Localizer));
    }
    #endregion

}
