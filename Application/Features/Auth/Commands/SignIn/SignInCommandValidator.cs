using Application.Validations;
using ApplicationLayer.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Application.Features.AuthFeature;

public class SignInCommandValidator : AbstractValidator<SignInCommand>
{
    private readonly IStringLocalizer<SharedResources> _Localizer;
    public SignInCommandValidator(IStringLocalizer<SharedResources> stringLocalizer)
    {
        _Localizer = stringLocalizer;

        RuleFor(x => x.Email)
           .ApplyEmailValidation(_Localizer);

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage(_Localizer[SharedResourcesKeys.PasswordRequired]);
    }
}
