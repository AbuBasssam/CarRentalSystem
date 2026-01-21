using Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Authorization.Handlers;
using Presentation.Authorization.Providers;
using Presentation.Authorization.Requirements;
using Presentation.Constants;

namespace Presentation;
public static class DependencyInjection
{
    public static IServiceCollection registerPresentationDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();

        AutoRegisterServices(services);


        // add authorization policies
        _AddPolicies(services);

        // Register the custom authorization handlers and providers
        AddHandlers(services);

        _AddCORS(services);

        return services;
    }

    private static void _AddCORS(IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(Policies.CORS, policy =>
            {
                policy
                    .WithOrigins("https://localhost:7137", "http://localhost:5013")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
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
    private static void _AddPolicies(this IServiceCollection services)
    {
        // add policies for authorization
        services.AddAuthorization(options =>
        {
            options.AddPolicy(Policies.ResetPassword, policy =>

                policy.Requirements.Add(new ResetPasswordRequirement())
            );

            options.AddPolicy(Policies.Logout, policy =>

                policy.Requirements.Add(new LogoutRequirement())
            );
            options.AddPolicy(Policies.ValidToken, policy =>

                policy.Requirements.Add(new ValidTokenRequirement())
            );


        });


    }
    private static void AddHandlers(IServiceCollection services)
    {

        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

        services.AddScoped<IAuthorizationHandler, ResetPasswordHandler>();

        services.AddScoped<IAuthorizationHandler, LogoutHandler>();

        services.AddScoped<IAuthorizationHandler, ValidTokenHandler>();

    }
}