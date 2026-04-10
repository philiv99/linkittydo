using LinkittyDo.Api.Services;

namespace LinkittyDo.Api;

public class SessionCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SessionCleanupService> _logger;
    private readonly TimeSpan _sessionTtl;
    private readonly TimeSpan _cleanupInterval;

    public SessionCleanupService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<SessionCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        var ttlHours = configuration.GetValue("SessionManagement:TtlHours", 24);
        _sessionTtl = TimeSpan.FromHours(ttlHours);

        var intervalMinutes = configuration.GetValue("SessionManagement:CleanupIntervalMinutes", 30);
        _cleanupInterval = TimeSpan.FromMinutes(intervalMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Session cleanup service started (TTL: {Ttl}, Interval: {Interval})",
            _sessionTtl, _cleanupInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_cleanupInterval, stoppingToken);
            using var scope = _serviceProvider.CreateScope();
            var gameService = scope.ServiceProvider.GetRequiredService<IGameService>();
            gameService.RemoveExpiredSessions(_sessionTtl);
        }
    }
}
