using Microsoft.Extensions.Logging;
using TodoList.API.Extensions;
using System.Security.Claims;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// create a logger for startup/config calls
var logger = LoggerFactory.Create(lb => lb.AddConsole()).CreateLogger("Startup");

// Configure services using the existing ServiceExtensions
builder.Services.AddSwaggerGen();
// Configure controllers and register NodaTime converters so Instant is serialized as ISO string
builder.Services.AddControllers().AddJsonOptions(opts =>
{
    opts.JsonSerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
});

// Configure CORS for frontend clients. Read allowed client URL(s) from configuration when available,
// otherwise fall back to common local React dev origin.
var clientOrigins = builder.Configuration.GetSection("ClientApp:AllowedOrigins").Get<string[]>();
if (clientOrigins == null || clientOrigins.Length == 0)
{
    clientOrigins = new[] { "http://localhost:3000" };
}
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalDev", policy =>
    {
        policy.WithOrigins(clientOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// call extension methods that require configuration and logger
// Determine if PostgreSql connection string exists
var connStr = builder.Configuration.GetConnectionString("PostgreSql");
var isDev = builder.Environment.IsDevelopment();
if (string.IsNullOrWhiteSpace(connStr))
{
    if (isDev)
    {
        logger.LogWarning("PostgreSql connection string is missing. Running in Development without EF/Identity/Repository registration to allow dev-only endpoints.");
        // Still register JWT, storage, swagger, automapper as they are useful for dev endpoints
        builder.Services.ConfigureJwtAuthentication(builder.Configuration, logger);
        builder.Services.ConfigureStorage(builder.Configuration, logger);
        builder.Services.ConfigureAutoMapper(logger);
        builder.Services.ConfigureSwagger(logger);
        builder.Services.ConfigureJwtBlacklist(logger);
    }
    else
    {
        // In non-development, fail fast via ConfigureEntityFramework which will throw a clearer error
        builder.Services.ConfigureEntityFramework(builder.Configuration, logger);
        builder.Services.ConfigureIdentity(logger);
        builder.Services.ConfigureJwtAuthentication(builder.Configuration, logger);
        builder.Services.ConfigureRepository(logger);
        builder.Services.ConfigureStorage(builder.Configuration, logger);
        builder.Services.ConfigureAutoMapper(logger);
        builder.Services.ConfigureSwagger(logger);
        builder.Services.ConfigureJwtBlacklist(logger);
    }
}
else
{
    builder.Services.ConfigureEntityFramework(builder.Configuration, logger);
    builder.Services.ConfigureIdentity(logger);
    builder.Services.ConfigureJwtAuthentication(builder.Configuration, logger);
    builder.Services.ConfigureRepository(logger);
    builder.Services.ConfigureStorage(builder.Configuration, logger);
    builder.Services.ConfigureAutoMapper(logger);
    builder.Services.ConfigureSwagger(logger);
    builder.Services.ConfigureJwtBlacklist(logger);
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Show detailed exception page in Development to aid debugging of 500 errors
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TodoList API V1");
    });
    // Development-only minimal endpoint to quickly verify frontend<->backend connectivity
    app.MapGet("/api/dev/minimal/todos", () =>
    {
        var items = new List<TodoList.Core.Models.TodoItem>
        {
            new TodoList.Core.Models.TodoItem { Id = Guid.NewGuid(), Title = "Dev Todo 1", Content = "Sample", Done = false, DueTo = NodaTime.Instant.FromDateTimeUtc(DateTime.UtcNow.AddDays(1)) },
            new TodoList.Core.Models.TodoItem { Id = Guid.NewGuid(), Title = "Dev Todo 2", Content = "Sample 2", Done = true, DueTo = NodaTime.Instant.FromDateTimeUtc(DateTime.UtcNow.AddDays(2)) }
        };
        return Results.Ok(items);
    });
    // Development-only login that issues a JWT for local testing (no Identity checks)
    app.MapPost("/api/dev/minimal/login", (Microsoft.Extensions.Configuration.IConfiguration config, TodoList.API.Models.UserLoginDto login) =>
    {
        var key = config["Authentication:JWT:SecurityKey"] ?? throw new InvalidOperationException("JWT SecurityKey not configured");
        var issuer = config["Authentication:JWT:Issuer"] ?? "TodoListAPI";
        var audience = config["Authentication:JWT:Audience"] ?? "TodoListClient";

        var securityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(key));
        var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(securityKey, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

        var claims = new[] {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, login.Email ?? "dev@local"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, login.Email ?? "dev@local")
        };

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials
        );

        var tokenString = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
        return Results.Ok(new { token = tokenString });
    });
    // Development-only protected endpoint to verify JWT authorization
    app.MapGet("/api/dev/minimal/protected", (System.Security.Claims.ClaimsPrincipal user) =>
    {
        var name = user.Identity?.Name ?? user.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "unknown";
        return Results.Ok(new { message = $"Hello {name}, your token is valid." });
    }).RequireAuthorization();
}

// Only use HTTPS redirection in non-development environments. During local development
// the frontend often runs on http://localhost:3000 and forcing an HTTP->HTTPS redirect
// can cause CORS/preflight requests to fail (redirect responses may not include the
// Access-Control-Allow-Origin header). Restrict HTTPS redirection to production.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// If a client build exists (e.g. React production build), serve it as static files so the API can
// also host the SPA in production. This looks for the build in ../TodoList.Web/ClientApp/build
// relative to the API project's working directory.
var clientBuildPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "TodoList.Web", "ClientApp", "build");
if (Directory.Exists(clientBuildPath))
{
    var staticFileOptions = new Microsoft.AspNetCore.Builder.StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(clientBuildPath),
        RequestPath = ""
    };
    app.UseDefaultFiles(new Microsoft.AspNetCore.Builder.DefaultFilesOptions
    {
        FileProvider = staticFileOptions.FileProvider
    });
    app.UseStaticFiles(staticFileOptions);
}

app.UseRouting();

app.UseCors("AllowLocalDev");

// Development-only: if there is no authenticated user, inject a dev user so frontend can bypass auth during local testing
if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        if (context.User?.Identity == null || !context.User.Identity.IsAuthenticated)
        {
            // Use the seeded test user email so UserManager can resolve an ApplicationUser in Development
            var claims = new[] {
                    new Claim(ClaimTypes.NameIdentifier, "test@local"),
                    new Claim(ClaimTypes.Email, "test@local"),
                    new Claim(ClaimTypes.Name, "Dev User")
                };
            var identity = new ClaimsIdentity(claims, "DevAuth");
            context.User = new ClaimsPrincipal(identity);
        }
        await next();
    });
}

app.UseAuthentication();
app.UseAuthorization();

// Map controller routes
app.MapControllers();

// SPA fallback: if the request path does not start with /api and there is an index.html in the
// client build, return it so client-side routing (React Router) works when hosted by the API.
if (Directory.Exists(clientBuildPath))
{
    app.MapWhen(ctx => !ctx.Request.Path.StartsWithSegments("/api") &&
                       (ctx.Request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase)), branch =>
    {
        branch.Run(async context =>
        {
            var indexPath = Path.Combine(clientBuildPath, "index.html");
            context.Response.ContentType = "text/html";
            await context.Response.SendFileAsync(indexPath);
        });
    });
}

// Seed a test user in Development for E2E testing
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    try
    {
        // Only attempt seeding if UserManager is registered (we may skip EF/Identity in dev)
        var userManager = services.GetService<Microsoft.AspNetCore.Identity.UserManager<TodoList.Core.Models.ApplicationUser>>();
        if (userManager != null)
        {
            var email = "test@local";
            var existing = userManager.FindByEmailAsync(email).Result;
            if (existing == null)
            {
                var user = new TodoList.Core.Models.ApplicationUser { UserName = email, Email = email };
                var result = userManager.CreateAsync(user, "P@ssword1!").Result;
                if (result.Succeeded)
                {
                    // optional: add roles/claims
                }
            }
        }
        else
        {
            logger.LogWarning("UserManager not registered; skipping seeding test user.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to seed test user");
    }
}

app.Run();
