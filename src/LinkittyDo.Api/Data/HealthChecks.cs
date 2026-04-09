using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

namespace LinkittyDo.Api.Data;

/// <summary>
/// Health check for MySQL connectivity via EF Core DbContext.
/// Executes a lightweight query to verify the database is reachable.
/// </summary>
public class MySqlHealthCheck : IHealthCheck
{
    private readonly LinkittyDoDbContext _dbContext;

    public MySqlHealthCheck(LinkittyDoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbContext.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
            return HealthCheckResult.Healthy("MySQL is reachable");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("MySQL is unreachable", ex);
        }
    }
}

/// <summary>
/// Health check for JSON file storage provider.
/// Verifies that data directories exist and are accessible.
/// </summary>
public class JsonStorageHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;

    public JsonStorageHealthCheck(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var baseDir = _configuration.GetValue<string>("DataDirectory") ?? "Data";
        var usersDir = Path.Combine(baseDir, "Users");
        var phrasesDir = Path.Combine(baseDir, "Phrases");
        var gameRecordsDir = Path.Combine(baseDir, "GameRecords");

        var issues = new List<string>();

        if (!Directory.Exists(baseDir))
            issues.Add($"Base data directory missing: {baseDir}");
        if (!Directory.Exists(usersDir))
            issues.Add($"Users directory missing: {usersDir}");
        if (!Directory.Exists(phrasesDir))
            issues.Add($"Phrases directory missing: {phrasesDir}");
        if (!Directory.Exists(gameRecordsDir))
            issues.Add($"GameRecords directory missing: {gameRecordsDir}");

        if (issues.Count > 0)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                "Some data directories are missing",
                data: new Dictionary<string, object> { { "issues", issues } }));
        }

        return Task.FromResult(HealthCheckResult.Healthy("All data directories exist"));
    }
}
