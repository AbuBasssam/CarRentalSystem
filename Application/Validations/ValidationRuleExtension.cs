using ApplicationLayer.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Application.Validations;

public static class ValidationRuleExtension
{
    private const int EmailMaxLength = 256;

    public static IRuleBuilderOptions<T, TProperty> ApplyNotEmptyRule<T, TProperty>(this IRuleBuilder<T, TProperty> ruleBuilder, string message)
    {
        return ruleBuilder.NotEmpty().WithMessage(message);
    }
    public static IRuleBuilderOptions<T, string> ApplyEmailAddressRule<T>(this IRuleBuilder<T, string> ruleBuilder, string message)
    {
        return ruleBuilder.EmailAddress().WithMessage(message);
    }

    public static IRuleBuilderOptions<T, TProperty> ApplyNotNullableRule<T, TProperty>(this IRuleBuilder<T, TProperty> ruleBuilder, string message)
    {
        return ruleBuilder.NotNull().WithMessage(message);
    }

    public static IRuleBuilderOptions<T, string> ApplyMinLengthRule<T>(this IRuleBuilder<T, string> ruleBuilder, int minLength, string message)
    {
        return ruleBuilder.MinimumLength(minLength).WithMessage(message);
    }

    public static IRuleBuilderOptions<T, string> ApplyMaxLengthRule<T>(this IRuleBuilder<T, string> ruleBuilder, int maxLength, string message)
    {
        return ruleBuilder.MaximumLength(maxLength).WithMessage(message);
    }

    /// <summary>
    /// Apply standard email validation rules
    /// </summary>
    public static IRuleBuilderOptions<T, string> ApplyEmailValidation<T>(
        this IRuleBuilder<T, string> ruleBuilder,
        IStringLocalizer<SharedResources> localizer)
    {
        return ruleBuilder
            .NotEmpty()
                .WithMessage(localizer[SharedResourcesKeys.EmailRequired])
            .EmailAddress()
                .WithMessage(localizer[SharedResourcesKeys.InvalidEmail])
            .MaximumLength(EmailMaxLength)
                .WithMessage(string.Format(localizer[SharedResourcesKeys.MaxLength].Value, "Email", EmailMaxLength));
    }

    /// <summary>
    /// Apply standard OTP code validation rules (N digits)
    /// </summary>
    public static IRuleBuilderOptions<T, string> ApplyOtpCodeRules<T>(
      this IRuleBuilder<T, string> ruleBuilder, byte CodeLength,
      IStringLocalizer<SharedResources> localizer)
    {
        string regexPattern = $@"^\d{{{CodeLength}}}$";

        return ruleBuilder
            .NotEmpty()
                .WithMessage(localizer[SharedResourcesKeys.CodeRequired])

            .Length(6)
                .WithMessage(string.Format(localizer[SharedResourcesKeys.OtpCodeLength].Value, CodeLength))

            .Matches(regexPattern)
                .WithMessage(localizer[SharedResourcesKeys.InvalidCode]);
    }



}
