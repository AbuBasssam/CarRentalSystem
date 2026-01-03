using Application;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Seeder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using Presentation;
using PresentationLayer.Middleware;
using Serilog;
using System.Globalization;
var builder = WebApplication.CreateBuilder(args);



// 1. ÊÍãíá .env ÃæáÇð
DotNetEnv.Env.Load(@"C:/Users/Hp/source/repos/CarRentalSystem/.env");

// 2. ÏãÌ ÇáãÊÛíÑÇÊ ÇáÈíÆíÉ ãÚ Configuration
builder.Configuration.AddEnvironmentVariables();

// 3. Override Þíã appsettings.json ÈÞíã ãä .env
builder.Configuration["ConnectionStrings:DefaultConnection"] =
    Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");

builder.Configuration["JwtSettings:Secret"] =
    Environment.GetEnvironmentVariable("JWT_SECRET_KEY");

builder.Configuration["JwtSettings:Issuer"] =
    Environment.GetEnvironmentVariable("JWT_ISSUER");

builder.Configuration["JwtSettings:Audience"] =
    Environment.GetEnvironmentVariable("JWT_AUDIENCE");

builder.Configuration["EmailSettings:host"] =
    Environment.GetEnvironmentVariable("SMTP_HOST");

builder.Configuration["EmailSettings:FromEmail"] =
    Environment.GetEnvironmentVariable("SMTP_USERNAME");

builder.Configuration["EmailSettings:password"] =
    Environment.GetEnvironmentVariable("SMTP_PASSWORD");

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();



#region Register Presentation Layer Controllers

var presentationAssembly = typeof(Presentation.AssemblyReference).Assembly;

builder.Services
    .AddControllers()
    .AddApplicationPart(presentationAssembly);

#endregion

#region Dependency injections

builder.Services
.registerInfrastructureDependencies(builder.Configuration)
.registerApplicationDependencies(builder.Configuration)
.registerPresentationDependencies(builder.Configuration);


#endregion

#region Serilog

Log.Logger = new LoggerConfiguration().ReadFrom
      .Configuration(builder.Configuration)
      .CreateLogger();
builder.Services.AddSerilog();

#endregion

#region Localization

builder.Services.AddControllersWithViews();
builder.Services.AddLocalization(opt =>
{
    opt.ResourcesPath = "";
});

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    List<CultureInfo> supportedCultures = new List<CultureInfo>
    {
        new CultureInfo("en"),
        new CultureInfo("ar")
    };

    options.DefaultRequestCulture = new RequestCulture("en");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

#endregion
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

#region Seeder
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
    var DbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await RoleSeeder.SeedAsync(roleManager);
    await UserSeeder.SeedAsync(userManager);



}

#endregion

#region Localization Middleware

var options = app.Services.GetService<IOptions<RequestLocalizationOptions>>();
app.UseRequestLocalization(options!.Value);

#endregion

app.UseMiddleware<GlobalRateLimitingMiddleware>();

app.UseMiddleware<SensitiveRateLimitingMiddleware>();

app.UseMiddleware<ErrorHandlerMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();

