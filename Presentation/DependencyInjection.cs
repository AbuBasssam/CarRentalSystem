using Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Presentation;
public static class DependencyInjection
{
    public static IServiceCollection registerPresentationDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        AutoRegisterServices(services);

        return services;
    }

    /// <summary>
    /// Auto-register all services using Scrutor based on marker interfaces
    /// </summary>
    private static void AutoRegisterServices(IServiceCollection services)
    {
        var assembly = typeof(AssemblyReference).Assembly;

        // Register Scoped services
        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(classes => classes.AssignableTo<IScopedService>())
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        // Register Transient services
        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(classes => classes.AssignableTo<ITransientService>())
            .AsImplementedInterfaces()
            .WithTransientLifetime());

        // Register Singleton services
        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(classes => classes.AssignableTo<ISingletonService>())
            .AsImplementedInterfaces()
            .WithSingletonLifetime());
    }
}