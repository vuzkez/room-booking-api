using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RoomBookingApi.Api.Middleware;
using RoomBookingApi.Application.Interfaces.Repositories;
using RoomBookingApi.Infrastructure.Services;
using RoomBookingApi.Application.Interfaces.Services;
using RoomBookingApi.Application.Interfaces.Strategies;
using RoomBookingApi.Application.Options;
using RoomBookingApi.Application.Services;
using RoomBookingApi.Application.Strategies;
using RoomBookingApi.Domain.Entities;
using RoomBookingApi.Infrastructure.Data;
using RoomBookingApi.Infrastructure.Repositories;

namespace RoomBookingApi.Api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            var serverVersion = new MySqlServerVersion(new Version(8, 0));

            builder.Services.AddDbContext<AppDbContext>(options => 
                options.UseMySql(connectionString,serverVersion,
                    optionsMySql => optionsMySql.MigrationsAssembly("RoomBookingApi.Infrastructure")));

            builder.Services.AddIdentity<UserApplication, RoleApplication>(options =>
            {
                options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+" +
                    "абвгдеёжзийклмнопрстуфхцчшщъыьэюяАБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ";
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true;
            }).AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

            builder.Services.AddOptions<SeedDataSettings>()
                .Bind(builder.Configuration.GetSection("SeedData"))
                .Validate(x =>
                    x.Roles != null &&
                    x.Roles.Any() &&
                    x.Roles.All(role => role == "Admin" || role == "User"),
                    "Error configuration: Roles can be only 'Admin' or 'User', and list cannot be null.")
                .Validate(x => x.AdminPassword.Length >= 8,
                    "Admin password must be 8 or higher symbols.")
                .ValidateDataAnnotations()
                .ValidateOnStart();

            builder.Services.AddOptions<JwtSettings>()
                .Bind(builder.Configuration.GetSection("Jwt"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            builder.Services.AddOptions<EmailSettings>()
                .Bind(builder.Configuration.GetSection("EmailSetting"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            builder.Services.AddOptions<TelegramSettings>()
                .Bind(builder.Configuration.GetSection("TelegramSetting"));

            var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>();
            var encodedKey = Encoding.UTF8.GetBytes(jwtSettings!.Key);

            builder.Services.AddAuthorization();
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(encodedKey),
                    ClockSkew = TimeSpan.FromMinutes(5),
                    ValidateLifetime = true
                };
            });
            builder.Services.AddControllers();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "Room Booking API V.1", Version = "v1", Description = "API for booking rooms." });

                var securitySchema = new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Description = "Enter: Bearer {your_token_jwt}",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT"
                };
                options.AddSecurityDefinition("Bearer", securitySchema);
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });
            builder.Services.AddProblemDetails();
            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
            builder.Services.AddHttpClient();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("Anything", policy =>
                {
                    policy.WithOrigins("http://localhost:3000")   
                          .AllowAnyMethod() 
                          .AllowAnyHeader();
                });
            });


            builder.Services.AddScoped<IPricingStrategy,WeekdayPricingStrategy>();
            builder.Services.AddScoped<IPricingStrategy, WeekendPricingStrategy>();
            builder.Services.AddScoped<IEmailConfirmOrChange, EmailConfirmOrChangeService>();
            builder.Services.AddScoped<PricingService>();
            builder.Services.AddScoped<IAvailabilityChecker, AvailabilityChecker>();
            builder.Services.AddScoped<IJwtService, JwtService>();
            builder.Services.AddScoped<IUnitOfWork,UnitOfWork>();
            builder.Services.AddScoped<IEmailCodeGenerator, EmailCodeGeneratorService>();
            builder.Services.AddScoped<IEmailSender,SmtpEmailSenderService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IRoomService, RoomService>();
            builder.Services.AddScoped<IBookingService, BookingService>();
            builder.Services.AddScoped<ITelegramNotifier, TelegramNotifier>();
            builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();

            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.AddPolicy("BookingLimit", httpContext =>
                {
                    var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";

                    return RateLimitPartition.GetFixedWindowLimiter(userId, partition => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    });
                });

                options.AddPolicy("ResendCodeLimit", httpContext =>
                {
                    string clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip";

                    return RateLimitPartition.GetFixedWindowLimiter(clientIp, partition => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 2,
                        Window = TimeSpan.FromMinutes(3),
                        QueueLimit = 0
                    });
                });

                options.AddPolicy("ConfirmEmailLimit", httpContext =>
                {
                    string clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip";

                    return RateLimitPartition.GetFixedWindowLimiter(clientIp, partition => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 4,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    });
                });

                options.AddPolicy("RegisterLimit", httpContext =>
                {
                    string clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip";

                    return RateLimitPartition.GetFixedWindowLimiter(clientIp, partition => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(2),
                        QueueLimit = 0
                    });
                });

                options.AddPolicy("LoginLimit", httpContext =>
                {
                    string clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip";

                    return RateLimitPartition.GetFixedWindowLimiter(clientIp, partition => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(3),
                        QueueLimit = 0
                    });
                });

                options.AddPolicy("EmailChangeLimit", httpContext =>
                {
                    var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
                    return RateLimitPartition.GetFixedWindowLimiter($"change-{userId}", partition => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 3,
                        Window = TimeSpan.FromMinutes(10),
                        QueueLimit = 0
                    });
                });
            });

            var app = builder.Build();

            app.UseExceptionHandler();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseCors("Anything");

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseRateLimiter();

            app.MapControllers();

            app.MapHealthChecks("/health");

            if (app.Environment.IsDevelopment())
            {
                using (var scope = app.Services.CreateScope())
                {
                    var services = scope.ServiceProvider;
                    await DbInitializer.Initialize(services);
                }
            }

            app.Run();
        }
    }
}
