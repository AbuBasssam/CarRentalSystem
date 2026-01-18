using Application.Validations;
using ApplicationLayer.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Application.Features.AuthFeature;

public class AuthorizeUserValidator : AbstractValidator<AuthorizeUserQuery>
{
    private readonly IStringLocalizer<SharedResources> _localizer;
    public AuthorizeUserValidator(IStringLocalizer<SharedResources> localizer)
    {
        _localizer = localizer;

        ApplyValidationrules();
    }

    public void ApplyValidationrules()
    {
        RuleFor(x => x.AccessToken)
            .ApplyNotEmptyRule(_localizer[SharedResourcesKeys.PropertyCannotBeEmpty])
            .ApplyNotNullableRule(_localizer[SharedResourcesKeys.PropertyCannotBeNull]);
    }
}
