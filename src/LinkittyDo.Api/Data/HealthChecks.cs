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
