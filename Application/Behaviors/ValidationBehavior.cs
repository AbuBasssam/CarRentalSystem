using ApplicationLayer.Resources;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Behaviors;

/// <summary>
/// Unified Validation Behavior
/// Provides consistent validation error handling across all requests
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly IStringLocalizer<SharedResources> _localizer;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators, IStringLocalizer<SharedResources> localizer)
    {
        _validators = validators;
        _localizer = localizer;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
        var failures = validationResults
                            .SelectMany(r => r.Errors)
                            .Where(f => f != null).ToList();

        if (failures.Count != 0)
        {

            // Create field-specific validation errors dictionary
            var validationErrors = new Dictionary<string, string>();

            foreach (var failure in failures)
            {
                var rawPropertyName = failure.PropertyName.Split('.').Last();

                var propertyName = ToCamelCase(rawPropertyName);
                var errorMessage = _localizer[failure.ErrorMessage];

                if (validationErrors.ContainsKey(propertyName))
                {
                    validationErrors[propertyName] += $", {errorMessage}";
                }
                else
                {
                    validationErrors[propertyName] = errorMessage;
                }
            }

            // Create a custom exception with structured validation errors
            var exception = new ValidationException(failures);
            exception.Data["ValidationErrors"] = validationErrors;

            throw exception;

        }

        return await next();
    }
    /// <summary>
    /// Convert PascalCase to camelCase for consistent JSON property names
    /// </summary>
    private static string ToCamelCase(string str)
    {
        if (string.IsNullOrEmpty(str) || char.IsLower(str[0]))
            return str;

        return char.ToLowerInvariant(str[0]) + str.Substring(1);
    }

}

