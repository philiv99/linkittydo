using System.Text;
using System.Threading.RateLimiting;
using DotNetEnv;
using LinkittyDo.Api;
using LinkittyDo.Api.Data;
using LinkittyDo.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

// Load .env file if it exists
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key not configured");
builder.Services.AddAuthentication(options =>
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
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});
builder.Services.AddAuthorization();

// Data provider feature flag: "Json" (default) or "MySql"
var dataProvider = builder.Configuration.GetValue<string>("DataProvider") ?? "Json";

if (dataProvider.Equals("MySql", StringComparison.OrdinalIgnoreCase))
{
    // EF Core + MySQL provider
    var connectionString = builder.Configuration.GetConnectionString("MySql")
        ?? throw new InvalidOperationException("MySql connection string not configured");
    builder.Services.AddDbContext<LinkittyDoDbContext>(options =>
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

    builder.Services.AddScoped<IUserRepository, EfUserRepository>();
    builder.Services.AddScoped<IGamePhraseRepository, EfGamePhraseRepository>();
    builder.Services.AddScoped<IGameRecordRepository, EfGameRecordRepository>();
    builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();

    // Services must be Scoped when repositories are Scoped
    builder.Services.AddScoped<IGamePhraseService, GamePhraseService>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
}
else
{
    // JSON file provider (default)
    builder.Services.AddSingleton<IUserRepository, JsonUserRepository>();
    builder.Services.AddSingleton<IGamePhraseRepository, JsonGamePhraseRepository>();
    builder.Services.AddSingleton<IGameRecordRepository, JsonGameRecordRepository>();

    builder.Services.AddSingleton<IGamePhraseService, GamePhraseService>();
    builder.Services.AddSingleton<IUserService, UserService>();
    builder.Services.AddSingleton<IAuthService, AuthService>();
}

// Session store is always Singleton (survives across Scoped lifetimes)
builder.Services.AddSingleton<ISessionStore, InMemorySessionStore>();

// GameService uses ISessionStore + repositories
if (dataProvider.Equals("MySql", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IGameService, GameService>();
}
else
{
    builder.Services.AddSingleton<IGameService, GameService>();
}

builder.Services.AddHttpClient<IClueService, ClueService>();
builder.Services.AddHttpClient<ILlmService, OpenAiLlmService>();

// Register background services
builder.Services.AddHostedService<SessionCleanupService>();

// Configure rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Game start: 10 requests per minute per IP
    options.AddFixedWindowLimiter("game-start", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });

    // Clue requests: 30 requests per minute per IP
    options.AddFixedWindowLimiter("clue", opt =>
    {
        opt.PermitLimit = 30;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });

    // User endpoints: 60 requests per minute per IP
    options.AddFixedWindowLimiter("user", opt =>
    {
        opt.PermitLimit = 60;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });

    // Global fallback: 100 requests per minute per IP
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// Configure CORS for React frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173", 
                "http://localhost:5174", 
                "http://localhost:3000")
              .SetIsOriginAllowedToAllowWildcardSubdomains()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
    
    // Allow any origin in production (Azure) - you can restrict this later
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
// Enable Swagger in all environments for API documentation
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "LinkittyDo API v1");
    c.RoutePrefix = "swagger"; // Swagger UI at /swagger
});

// Add a root endpoint that redirects to Swagger
app.MapGet("/", () => Results.Redirect("/swagger"));

// Add a health check endpoint
app.MapGet("/health", (IGameService gameService, IConfiguration configuration) =>
{
    var dataDir = configuration.GetValue<string>("DataDirectory") ?? "Data";
    var dataDirectoryExists = Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), dataDir));
    var startTime = System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime();
    var uptime = DateTime.UtcNow - startTime;

    return Results.Ok(new
    {
        status = "healthy",
        timestamp = DateTime.UtcNow,
        uptime = $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m",
        activeSessions = gameService.ActiveSessionCount,
        dependencies = new
        {
            dataDirectory = dataDirectoryExists ? "ok" : "missing",
        }
    });
});

// Fallback to Swagger for any unmatched routes
app.MapFallback(() => Results.Redirect("/swagger"));

app.UseHttpsRedirection();

// Use AllowAll CORS policy for Azure deployment
var env = app.Environment;
app.UseCors(env.IsDevelopment() ? "AllowReactApp" : "AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();

app.Run();
