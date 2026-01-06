using ApplicationLayer.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Application.Features.AuthFeature;

public class SendResetCodeCommandValidator : AbstractValidator<SendResetCodeCommand>
{
    #region Field(s)

    private readonly IStringLocalizer<SharedResources> _Localizer;

    #endregion

    #region Constructor(s)

    public SendResetCodeCommandValidator(IStringLocalizer<SharedResources> Localizer)
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
            .SetValidator(new SendResetCodeDTO.Validator(_Localizer));
    }
    #endregion
}
