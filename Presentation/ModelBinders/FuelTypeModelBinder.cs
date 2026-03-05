using Domain.HelperClasses;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Presentation;

public class FuelTypeModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var value = bindingContext.ValueProvider
            .GetValue(bindingContext.ModelName)
            .FirstValue;

        if (string.IsNullOrWhiteSpace(value))
        {
            bindingContext.Result = ModelBindingResult.Success(null);
            return Task.CompletedTask;
        }

        var fuelType = FuelType.Parse(value);

        if (fuelType is null)
        {
            bindingContext.ModelState.AddModelError(
                bindingContext.ModelName,
                "Invalid fuel type value.");
            return Task.CompletedTask;
        }

        bindingContext.Result = ModelBindingResult.Success(fuelType);
        return Task.CompletedTask;
    }
}
