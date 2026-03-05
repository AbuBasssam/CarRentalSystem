using Domain.HelperClasses;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace Presentation;

public class FuelTypeModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context.Metadata.ModelType == typeof(FuelType))
        {
            return new BinderTypeModelBinder(typeof(FuelTypeModelBinder));
        }

        return null;
    }
}