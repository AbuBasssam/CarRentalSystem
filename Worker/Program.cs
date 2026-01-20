using Serilog;
using Worker;

var builder = Host.CreateApplicationBuilder(args);

DotNetEnv.Env.Load(@"C:/Users/Hp/source/repos/CarRentalSystem/.env");

builder.Configuration.AddEnvironmentVariables();

builder.Configuration["ConnectionStrings:DefaultConnection"] =
    Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");

builder.Services.AddHostedService<CarRentalWorker>();

#region Serilog

Log.Logger = new LoggerConfiguration().ReadFrom
      .Configuration(builder.Configuration)
      .CreateLogger();
builder.Services.AddSerilog();

#endregion

#region Dependency injections

builder.Services.registerDependencies(builder.Configuration);

#endregion

var host = builder.Build();

// Log startup information
var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Worker Service starting up...");
logger.LogInformation("Environment: {Environment}", builder.Environment.EnvironmentName);

try
{
    await host.RunAsync();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Worker Service terminated unexpectedly");
    throw;
}
finally
{
    logger.LogInformation("Worker Service shut down complete");
    Log.CloseAndFlush();
}
