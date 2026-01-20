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

        services.AddHostedService<AuthTokenCleanupService>();

        ConfigureTokenCleanupService(services, configuration);

        services.AddDbContext<AppDbContext>(options =>
        {
            var connectionstring = configuration.GetConnectionString("DefaultConnection");
            options.UseSqlServer(connectionstring);
        }
        );

        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;

    }

    private static void ConfigureTokenCleanupService(IServiceCollection services, IConfiguration configuration)
    {
        var TokenCleanupSection = configuration.GetSection("BackgroundServices:AuthTokenCleanup");
        services.Configure<AuthTokenCleanupOptions>(TokenCleanupSection);


        var TokenCleanupOptions = new AuthTokenCleanupOptions();


        configuration.GetSection(nameof(TokenCleanupOptions)).Bind(TokenCleanupOptions);

        services.AddSingleton(TokenCleanupOptions);
    }
}
