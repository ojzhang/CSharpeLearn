using System.Text;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NodaTime;
using Npgsql;
using TodoList.API.MapperProfiles;
using TodoList.Core.Contexts;
using TodoList.Core.Interfaces;
using TodoList.Core.Models;
using TodoList.Core.Services;
// using TodoList.Core.Services;
using TodoList.API.Services;

namespace TodoList.API.Extensions
{
    public static class ServiceExtensions
    {
        /// <summary>
        /// Configures Entity Framework DbContext for the project.
        /// </summary>
        public static void ConfigureEntityFramework(this IServiceCollection services,
            IConfiguration configuration,
            ILogger logger)
        {
            // Prefer GetConnectionString helper; validate and fail fast if missing
            var connStr = configuration.GetConnectionString("PostgreSql");
            if (string.IsNullOrWhiteSpace(connStr))
            {
                logger.LogError("PostgreSql connection string is not configured. Check appsettings.json or environment variables.");
                throw new InvalidOperationException("Connection string 'PostgreSql' is not configured.");
            }

            var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connStr);
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(connectionStringBuilder.ConnectionString, x => x.MigrationsAssembly("TodoList.Data"));
            });

            logger.LogInformation("Configured EF.");
        }

        /// <summary>
        /// Configures API spec by using swagger.
        /// </summary>
        public static void ConfigureSwagger(this IServiceCollection services, ILogger logger)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "TodoList API", Version = "v1" });
                // Register operation filter that handles IFormFile parameters
                c.OperationFilter<SwaggerFileOperationFilter>();
            });

            logger.LogInformation("Configured Swagger and registered file upload OperationFilter.");
        }

        /// <summary>
        /// Configures JSON Web Token authentication scheme.
        /// </summary>
        public static void ConfigureJwtAuthentication(this IServiceCollection services, IConfiguration config, ILogger logger)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = config["Authentication:JWT:Issuer"],
                    ValidAudience = config["Authentication:JWT:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(config["Authentication:JWT:SecurityKey"]))
                };

                // 保留已存在的事件（如黑名单检测）
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var blacklistService = context.HttpContext.RequestServices.GetService<IJwtBlacklistService>();
                        if (blacklistService != null)
                        {
                            var tokenId = context.SecurityToken.Id;
                            if (await blacklistService.IsTokenBlacklistedAsync(tokenId))
                            {
                                context.Fail("Token has been revoked");
                            }
                        }
                    }
                };
            });

            services.AddAuthorization();
            logger.LogInformation("Configured JWT authentication scheme.");
        }

        /// <summary>
        /// Configures identity services.
        /// </summary>
        public static void ConfigureIdentity(this IServiceCollection services, ILogger logger)
        {
            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            logger.LogInformation("Configured Identity service.");
        }

        /// <summary>
        /// Configures repository service.
        /// </summary>
        public static void ConfigureRepository(this IServiceCollection services, ILogger logger)
        {
            services.AddSingleton<IClock>(SystemClock.Instance);
            services.AddScoped<ITodoItemServices, TodoItemService>();
            logger.LogInformation("Configured Repository services.");
        }

        /// <summary>
        /// Configures AutoMapper services.
        /// </summary>
        public static void ConfigureAutoMapper(this IServiceCollection services, ILogger logger)
        {
            services.AddAutoMapper(typeof(Startup), typeof(TodoItemProfile));
            logger.LogInformation("Configured AutoMapper service.");
        }

        /// <summary>
        /// Configures file storage service.
        /// </summary>
        public static void ConfigureStorage(this IServiceCollection services, IConfiguration configuration, ILogger logger)
        {
            var storageService = new LocalFileStorageService(configuration["LocalFileStorageBasePath"]);
            services.AddSingleton<IFileStorageService>(storageService);
            logger.LogInformation("Configured Storage service.");
        }

        /// <summary>
        /// Configures JWT blacklist service.
        /// </summary>
        public static void ConfigureJwtBlacklist(this IServiceCollection services, ILogger logger)
        {
            services.AddSingleton<IJwtBlacklistService, JwtBlacklistService>();
            logger.LogInformation("Configured JWT blacklist service.");
        }
    }
}