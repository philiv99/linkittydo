using LinkittyDo.Api.Models;

namespace LinkittyDo.Api.Services;

public interface IAuditService
{
    Task LogAsync(string action, string? userId = null, string? entityType = null,
        string? entityId = null, string? details = null, string? ipAddress = null);
}

public class AuditService : IAuditService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AuditService> _logger;

    public AuditService(IServiceProvider serviceProvider, ILogger<AuditService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task LogAsync(string action, string? userId = null, string? entityType = null,
        string? entityId = null, string? details = null, string? ipAddress = null)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<Data.LinkittyDoDbContext>();

            dbContext.AuditLog.Add(new AuditLogEntry
            {
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = details,
                IpAddress = ipAddress,
                Timestamp = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write audit log entry: {Action}", action);
        }
    }
}

/// <summary>
/// No-op audit service for JSON data provider mode (no database available).
/// </summary>
public class NoOpAuditService : IAuditService
{
    public Task LogAsync(string action, string? userId = null, string? entityType = null,
        string? entityId = null, string? details = null, string? ipAddress = null)
    {
        return Task.CompletedTask;
    }
}
