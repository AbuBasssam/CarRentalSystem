using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Presentation.Filters;
/// <summary>
/// CSRF Validation Attribute for API endpoints
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class ValidateCsrfTokenAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var antiforgery = context.HttpContext.RequestServices
            .GetRequiredService<IAntiforgery>();

        try
        {
            // Validate the CSRF token
            await antiforgery.ValidateRequestAsync(context.HttpContext);
            await next();
        }
        catch (AntiforgeryValidationException)
        {
            // return 403 Forbidden if CSRF validation fails
            context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
        }
    }
}
