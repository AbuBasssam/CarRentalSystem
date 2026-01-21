using Implementations;
using Infrastructure;
using Infrastructure.Repositories;
using Interfaces;
using Microsoft.EntityFrameworkCore;
using Worker.BackgroundServices;
using Worker.Configuration;

namespace Worker;
public static class DependencyInjection
{
    public static IServiceCollection registerDependencies(this IServiceCollection services, IConfiguration configuration)
    {

        BackgroundServicesRegistration(services, configuration);

        ConfigureTokenCleanupService(services, configuration);

        ConfigurePasswordResetTokenCleanupService(services, configuration);

        ConfigureUnverifiedUserCleanupService(services, configuration);

        ConfigureOtpCleanupService(services, configuration);

        ServicesRegistration(services, configuration);

        DbContextRegistration(services, configuration);

        return services;

    }

    private static void ConfigureTokenCleanupService(IServiceCollection services, IConfiguration configuration)
    {
        var TokenCleanupSectionName = "BackgroundServices:AuthTokenCleanup";

        var TokenCleanupSection = configuration.GetSection(TokenCleanupSectionName);

        services.Configure<AuthTokenCleanupOptions>(TokenCleanupSection);


        var TokenCleanupOptions = new AuthTokenCleanupOptions();


        configuration.GetSection(nameof(TokenCleanupOptions)).Bind(TokenCleanupOptions);

        services.AddSingleton(TokenCleanupOptions);
    }

    private static void ConfigurePasswordResetTokenCleanupService(IServiceCollection services, IConfiguration configuration)
    {
        var PasswordResetTokenCleanupSectionName = "BackgroundServices:PasswordResetTokenCleanup";

        var passwordResetCleanupSection = configuration.GetSection(PasswordResetTokenCleanupSectionName);

        services.Configure<PasswordResetTokenCleanupOptions>(passwordResetCleanupSection);

        var passwordResetCleanupOptions = new PasswordResetTokenCleanupOptions();

        configuration.GetSection(PasswordResetTokenCleanupSectionName).Bind(passwordResetCleanupOptions);

        services.AddSingleton(passwordResetCleanupOptions);
    }

    private static void ConfigureUnverifiedUserCleanupService(IServiceCollection services, IConfiguration configuration)
    {
        var unverifiedUserCleanupSection = configuration.GetSection("BackgroundServices:UnverifiedUserCleanup");
        services.Configure<UnverifiedUserCleanupOptions>(unverifiedUserCleanupSection);

        var unverifiedUserCleanupOptions = new UnverifiedUserCleanupOptions();
        configuration.GetSection("BackgroundServices:UnverifiedUserCleanup").Bind(unverifiedUserCleanupOptions);

        services.AddSingleton(unverifiedUserCleanupOptions);
    }

    private static void ConfigureOtpCleanupService(IServiceCollection services, IConfiguration configuration)
    {
        var OtpCleanupSectionName = "BackgroundServices:OtpCleanup";


        var OtpCleanupSection = configuration.GetSection(OtpCleanupSectionName);

        services.Configure<OtpCleanupOptions>(OtpCleanupSection);

        var OtpCleanupOptions = new OtpCleanupOptions();

        configuration.GetSection(OtpCleanupSectionName).Bind(OtpCleanupOptions);

        services.AddSingleton(OtpCleanupOptions);
    }

    private static void BackgroundServicesRegistration(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHostedService<AuthTokenCleanupService>();

        services.AddHostedService<PasswordResetTokenCleanupService>();

        services.AddHostedService<UnverifiedUserCleanupService>();

        services.AddHostedService<OtpCleanupService>();

    }

    private static void ServicesRegistration(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IUserTokenRepository, UserTokenRepository>();

        services.AddScoped<IUserRepository, UserRepository>();

        services.AddScoped<IOtpRepository, OtpRepository>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
    }

    private static void DbContextRegistration(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            var connectionstring = configuration.GetConnectionString("DefaultConnection");
            options.UseSqlServer(connectionstring);
        }, ServiceLifetime.Scoped
        );


    }

}
