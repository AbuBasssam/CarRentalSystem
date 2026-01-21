using Domain.Entities;
using Implementations;
using Infrastructure.Repositories;
using Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection registerInfrastructureDependencies(this IServiceCollection services, IConfiguration configuration)
    {

        DbContextRegisteration(services, configuration);

        IdentityRegisteration(services);

        //RepsitoriesRegisteration(services);

        AutoRegisterRepositories(services);





        return services;
    }
    private static void DbContextRegisteration(IServiceCollection services, IConfiguration configuration)
    {

        services.AddDbContext<AppDbContext>(options =>
        options
        .UseLazyLoadingProxies()
        .UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
        );
    }
    private static void IdentityRegisteration(IServiceCollection services)
    {
        services.AddIdentity<User, Role>(options =>
        {
            // Password settings
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredUniqueChars = 1;

            // Lockout settings
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // User settings
            options.User.RequireUniqueEmail = true;

            options.SignIn.RequireConfirmedEmail = true;
            options.User.AllowedUserNameCharacters =
         "abcdefghijklmnopqrstuvwxyz" +
         "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
         "0123456789" +
         "-._@+\r\n\r\n";


        })
        //.AddUserManager<UserManager<User>>()
        //.AddRoles<Role>()
        //.AddRoleManager<RoleManager<Role>>()
        .AddEntityFrameworkStores<AppDbContext>();
    }
    private static void RepsitoriesRegisteration(IServiceCollection services)
    {
        // Register the repository and Unit of Work
        services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IUserTokenRepository, UserTokenRepository>();

        services.AddScoped<IOtpRepository, OtpRepository>();



    }
    /// <summary>
    /// Auto-register all repositories using Scrutor based on marker interfaces
    /// </summary>
    private static void AutoRegisterRepositories(IServiceCollection services)
    {
        var assembly = typeof(AssemblyReference).Assembly;

        // Register Scoped services (Repositories, UnitOfWork, etc.)
        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(classes => classes.AssignableTo<IScopedService>())
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        // Register Transient services (if any in Infrastructure)
        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(classes => classes.AssignableTo<ITransientService>())
            .AsImplementedInterfaces()
            .WithTransientLifetime());

        // Register Singleton services (if any in Infrastructure)
        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(classes => classes.AssignableTo<ISingletonService>())
            .AsImplementedInterfaces()
            .WithSingletonLifetime());

        // Note: Generic repositories like IGenericRepository<,> need manual registration
        // Add them here if Scrutor doesn't pick them up automatically
        // services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));
    }


}
