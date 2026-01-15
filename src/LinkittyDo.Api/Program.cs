using DotNetEnv;
using LinkittyDo.Api.Data;
using LinkittyDo.Api.Services;

// Load .env file if it exists
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register data repositories
builder.Services.AddSingleton<IUserRepository, JsonUserRepository>();
builder.Services.AddSingleton<IGamePhraseRepository, JsonGamePhraseRepository>();

// Register application services
builder.Services.AddSingleton<IGamePhraseService, GamePhraseService>();
builder.Services.AddSingleton<IGameService, GameService>();
builder.Services.AddSingleton<IUserService, UserService>();
builder.Services.AddHttpClient<IClueService, ClueService>();
builder.Services.AddHttpClient<ILlmService, OpenAiLlmService>();

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
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Fallback to Swagger for any unmatched routes
app.MapFallback(() => Results.Redirect("/swagger"));

app.UseHttpsRedirection();

// Use AllowAll CORS policy for Azure deployment
var env = app.Environment;
app.UseCors(env.IsDevelopment() ? "AllowReactApp" : "AllowAll");

app.MapControllers();

app.Run();
