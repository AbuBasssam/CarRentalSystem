using Application.Validations;
using ApplicationLayer.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Application.Features.AuthFeature;

/// <summary>
/// Validator for RefreshTokenCommand
/// Ensures email is valid before processing refresh request
/// </summary>
public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    private readonly IStringLocalizer<SharedResources> _localizer;

    public RefreshTokenCommandValidator(IStringLocalizer<SharedResources> localizer)
    {
        _localizer = localizer;
        ApplyValidations();
    }

    private void ApplyValidations()
    {
        RuleFor(x => x.Email)
            .ApplyEmailValidation(_localizer);
    }
}