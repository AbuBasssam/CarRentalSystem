using ApplicationLayer.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Application.Features.AuthFeature;

public class ResendResetCodeValidator : AbstractValidator<ResendResetCodeCommand>
{
    #region Field(s)

    private readonly IStringLocalizer<SharedResources> _Localizer;

    #endregion

    #region Constructor(s)

    public ResendResetCodeValidator(IStringLocalizer<SharedResources> Localizer)
    {
        _Localizer = Localizer;
        ApplyValidations();

    }
    #endregion

    #region Method(s)

    private void ApplyValidations()
    {
        RuleFor(x => x.dto)
            .NotNull()
            .WithMessage(_Localizer[SharedResourcesKeys.RequestPayloadRequired])
            .SetValidator(new ResendCodeDTO.Validator(_Localizer));
    }
    #endregion

}