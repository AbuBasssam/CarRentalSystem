using FluentValidation;

namespace Application.Validations;

public static class ValidationRuleExtension
{
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



}
