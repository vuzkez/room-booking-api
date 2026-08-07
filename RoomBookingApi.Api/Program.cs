using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RoomBookingApi.Api.Middleware;
using RoomBookingApi.Application.Decorators;
using RoomBookingApi.Application.Interfaces.Repositories;
using RoomBookingApi.Application.Interfaces.Services;
using RoomBookingApi.Application.Interfaces.Strategies;
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
                options.Password.RequiredLength = 4;
                options.Password.RequireNonAlphanumeric = false;
            }).AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

            var jwtKey = builder.Configuration["Jwt:Key"];
            var issuer = builder.Configuration["Jwt:Issuer"];
            var audience = builder.Configuration["Jwt:Audience"];
            var encodedKey = Encoding.UTF8.GetBytes(jwtKey!);

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
                    ValidAudience = audience,
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(encodedKey),
                    ClockSkew = TimeSpan.FromMinutes(5),
                    ValidateLifetime = true
                };
            });
            builder.Services.AddMemoryCache();
            builder.Services.AddControllers();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1", Description = "API for booking rooms." });

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

            builder.Services.AddScoped<IPricingStrategy,WeekdayPricingStrategy>();
            builder.Services.AddScoped<IPricingStrategy, WeekendPricingStrategy>();
            builder.Services.AddScoped<PricingService>();
            builder.Services.AddScoped<IAvailabilityChecker, AvailabilityChecker>();
            builder.Services.Decorate<IAvailabilityChecker, CachedAvailabilityChecker>();
            builder.Services.AddScoped<IJwtService, JwtService>();
            builder.Services.AddScoped<IUnitOfWork,UnitOfWork>();
            builder.Services.AddScoped<IEmailCodeGenerator, EmailCodeGeneratorService>();
            builder.Services.AddScoped<IEmailSender,SmtpEmailSenderService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IRoomService, RoomService>();
            builder.Services.AddScoped<IBookingService, BookingService>();
            builder.Services.AddScoped<ITelegramNotifier, TelegramNotifier>();

            builder.Services.AddRateLimiter(options =>
            {
                options.AddFixedWindowLimiter("BookingLimit", opts =>
                {
                    opts.PermitLimit = 5;
                    opts.Window = TimeSpan.FromMinutes(1);
                    opts.QueueLimit = 0;
                });
            });

            var app = builder.Build();

            app.UseExceptionHandler();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseRateLimiter();

            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

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
