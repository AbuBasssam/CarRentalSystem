using Application.Behaviors;
using Application.Models;
using ApplicationLayer.Resources;
using Domain.AppMetaData;
using Domain.Enums;
using Domain.HelperClasses;
using FluentValidation;
using Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using System.Text;


namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection registerApplicationDependencies(this IServiceCollection services, IConfiguration configuration, bool isDevelopment)
    {

        JWTAuthentication(services, configuration, isDevelopment);

        //ServicesRegisteration(services);

        AutoRegisterServices(services);

        EmailSetting(services, configuration);

        FluentValidatorConfiguration(services);

        // Configuration for MediaR
        services.AddMediatR(_getMediatRServiceConfiguration);

        //Configuration for AutoMapper
        services.AddAutoMapper(Assembly.GetExecutingAssembly());

        services.AddTransient<ResponseHandler>();


        //Localization
        services.AddLocalization(options => options.ResourcesPath = "Resources");
        services.AddScoped<IStringLocalizer<SharedResources>, StringLocalizer<SharedResources>>();

        services.AddSwaggerGen(_GetSecurityRequirement);




        return services;
    }

    private static void JWTAuthentication(IServiceCollection services, IConfiguration configuration, bool isDevelopment)
    {
        //JWT Authentication
        var jwtSection = configuration.GetSection("JwtSettings");
        services.Configure<JwtSettings>(jwtSection);


        var JwtSettings = new JwtSettings();


        configuration.GetSection(nameof(JwtSettings)).Bind(JwtSettings);

        services.AddSingleton(JwtSettings);

        services
            .AddAuthentication(_authenticationInfo)
            .AddJwtBearer(option => { _JwtBearerInfo(option, JwtSettings, isDevelopment); });
    }

    private static void _JwtBearerInfo(JwtBearerOptions option, JwtSettings JwtSettings, bool isDevelopment)
    {
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = JwtSettings.ValidateIssuer,
            ValidIssuers = new[] { JwtSettings.Issuer },
            ValidateAudience = JwtSettings.ValidateAudience,
            ValidAudience = JwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSettings.Secret!)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
        option.RequireHttpsMetadata = false;
        option.SaveToken = true;


        option.TokenValidationParameters = validationParameters;

        option.Events = CreateJwtBearerEvents(isDevelopment);
        // ============================================
        // 🍪 COOKIE CONFIGURATION 
        // ============================================
        option.SaveToken = false;  // Don't save in Properties
        option.RequireHttpsMetadata = !isDevelopment;  // HTTPS in production



    }
    private static JwtBearerEvents CreateJwtBearerEvents(bool isDevelopment)
    {
        return new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                // Use .Append() to avoid ArgumentException if the key already exists
                if (context.Exception is SecurityTokenExpiredException)
                {
                    context.Response.Headers["Token-Expired"] = "true";
                    context.HttpContext.Items[Keys.Auth_Error_Metadata_Key] = new
                    {
                        ErrorCode = enErrorCode.TokenExpired,
                        IsRecoverable = true // Frontend should attempt refresh
                    };
                    Console.WriteLine("🕐 JWT Token Expired - Recoverable");
                }
                else if (context.Exception is SecurityTokenInvalidSignatureException)
                {
                    context.Response.Headers["Token-Invalid-Signature"] = "true";


                    context.HttpContext.Items[Keys.Auth_Error_Metadata_Key] = new
                    {
                        ErrorCode = enErrorCode.InvalidToken,
                        IsRecoverable = false
                    };
                    Console.WriteLine("❌ JWT Invalid Signature - Not Recoverable");
                }
                else
                {
                    context.Response.Headers["Token-Invalid"] = "true";


                    context.HttpContext.Items[Keys.Auth_Error_Metadata_Key] = new
                    {
                        ErrorCode = enErrorCode.InvalidToken,
                        IsRecoverable = false
                    };
                    Console.WriteLine($"❌ JWT Error: {context.Exception.Message}");
                }

                return Task.CompletedTask;
            },

            OnChallenge = context =>
            {
                // Skip default 401 response to let CustomAuthorizationMiddlewareResultHandler handle it
                context.HandleResponse();

                if (!context.HttpContext.Items.ContainsKey(Keys.Auth_Error_Metadata_Key))
                {
                    context.HttpContext.Items[Keys.Auth_Error_Metadata_Key] = new
                    {
                        ErrorCode = enErrorCode.MissingToken,
                        IsRecoverable = false
                    };
                }

                return Task.CompletedTask;
            },

            OnForbidden = context =>
            {
                context.HttpContext.Items[Keys.Auth_Error_Metadata_Key] = new
                {
                    ErrorCode = enErrorCode.AccessDenied,
                    IsRecoverable = false
                };
                Console.WriteLine("🔓 Access Denied - Insufficient Permissions");
                return Task.CompletedTask;
            },

            OnMessageReceived = context =>
            {
                // 1. Try reading from Authorization Header (Standard/Swagger)
                var token = context.Request.Headers["Authorization"]
                    .FirstOrDefault()?
                    .Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);

                // 2. If not found, try reading from Cookie
                if (string.IsNullOrEmpty(token))
                {
                    token = context.Request.Cookies[Keys.Access_Token_Key];
                }

                if (!string.IsNullOrEmpty(token))
                {
                    context.Token = token;
                }

                return Task.CompletedTask;
            }
        };

        /*// OnTokenValidated Event
        option.Events = new JwtBearerEvents
        {
             OnTokenValidated = async context =>
             {
                 // الحصول على Repository من DI
                 var refreshTokenRepo = context.HttpContext
                     .RequestServices
                     .GetRequiredService<IUserTokenRepository>();

                 // استخراج JTI من التوكن
                 var jti = context.Principal?
                     .FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

                 if (string.IsNullOrEmpty(jti))
                 {
                     context.Fail("Token missing JTI");
                     return;
                 }

                 // التحقق من حالة التوكن في قاعدة البيانات
                 var token = await refreshTokenRepo
                     .GetTableNoTracking()
                     .Where(x =>
                         x.JwtId == jti &&
                         x.Type == enTokenType.AuthToken
                     )
                     .FirstOrDefaultAsync();

                 // إذا لم يوجد التوكن
                 if (token == null)
                 {
                     context.Fail("Token not found");
                     return;
                 }

                 // إذا كان التوكن مُبطل
                 if (token.IsRevoked)
                 {
                     context.Fail("Token has been revoked");
                     return;
                 }

                 // إذا كان التوكن مستخدم
                 if (token.IsUsed)
                 {
                     context.Fail("Token has been used");
                     return;
                 }

                 // إذا كان التوكن منتهي
                 if (token.IsExpired())
                 {
                     context.Fail("Token has expired");
                     return;
                 }

                 // التوكن صالح، يمكن المتابعة
             }
         }*/
    }

    private static void _authenticationInfo(AuthenticationOptions option)
    {
        option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
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




    private static void EmailSetting(IServiceCollection services, IConfiguration configuration)
    {
        //way 1: Using IOptions<T>
        /*
        var EmailSetting = configuration.GetSection("emailSettings");

        services.Configure<EmailSettings>(EmailSetting);

        services.AddSingleton(EmailSetting);
        */

        // way 2: Using Bind method
        var emailSetting = configuration.GetSection("EmailSettings");
        services.Configure<EmailSettings>(emailSetting);

        var EmailSetting = new EmailSettings();

        configuration.GetSection(nameof(EmailSettings)).Bind(EmailSetting);

        services.AddSingleton(EmailSetting);
    }


    private static void _getMediatRServiceConfiguration(MediatRServiceConfiguration config)
        => config.RegisterServicesFromAssemblies(Assembly.GetExecutingAssembly());

    private static void FluentValidatorConfiguration(IServiceCollection services)
    {
        //Configuration for FluentValidator

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    }

    private static void _GetSecurityRequirement(SwaggerGenOptions option)
    {
        var openApiReference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = JwtBearerDefaults.AuthenticationScheme }; // Should match the scheme name
        var securityScheme = new OpenApiSecurityScheme { Reference = openApiReference, Name = JwtBearerDefaults.AuthenticationScheme, In = ParameterLocation.Header };
        var securityRequirement = new OpenApiSecurityRequirement { { securityScheme, new List<string>() } };
        SwaggerGenInfo(option);
        option.AddSecurityRequirement(securityRequirement);// This is a list of scopes, which is empty in this case

    }
    private static void SwaggerGenInfo(SwaggerGenOptions option)
    {
        option.SwaggerDoc("v1", new OpenApiInfo { Title = "CarRentalSystem", Version = "v1" });
        option.EnableAnnotations();

        option.AddSecurityDefinition("CSRF", new OpenApiSecurityScheme
        {
            Description = "CSRF Token from cookie (X-XSRF-TOKEN header)",
            Name = "X-XSRF-TOKEN",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "CSRF"
        });
        option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "CSRF"
                }
            },
            Array.Empty<string>()
        }
    });


        option.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer schema (e.g. Bearer your token)",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = JwtBearerDefaults.AuthenticationScheme,
        });
    }


}
