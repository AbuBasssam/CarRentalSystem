using Domain.Enums;
using Infrastructure.Security;
using Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Authorization.Handlers;
using Presentation.Authorization.Requirements;
using Presentation.Constants;

namespace Presentation;
public static class DependencyInjection
{
    public static IServiceCollection registerPresentationDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();

        AutoRegisterServices(services);

        services.AddScoped<IAuthorizationHandler, ResetPasswordHandler>();

        // add authorization policies
        _AddPolicies(services);

        // Register the custom authorization handlers and providers
        AddHandlers(services);

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
    private static void _AddPolicies(this IServiceCollection services)
    {
        // add policies for authorization
        services.AddAuthorization(options =>
        {
            options.AddPolicy(Policies.VerificationOnly, policy =>
                policy.Requirements.Add(new VerificationOnlyRequirement()));

            // إعداد سياسة المرحلة الأولى (انتظار التحقق)
            options.AddPolicy(Policies.AwaitVerification, policy =>
                policy.Requirements.Add(new ResetPasswordRequirement(enResetPasswordStage.AwaitingVerification)));


            // إعداد سياسة المرحلة الثانية (تم التحقق - جاهز لإعادة التعيين)
            options.AddPolicy(Policies.ResetPasswordVerified, policy =>
                policy.Requirements.Add(new ResetPasswordRequirement(enResetPasswordStage.Verified)));

        });


    }
    private static void AddHandlers(IServiceCollection services)
    {
        services.AddScoped<IAuthorizationHandler, VerificationOnlyHandler>();

        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
    }
}