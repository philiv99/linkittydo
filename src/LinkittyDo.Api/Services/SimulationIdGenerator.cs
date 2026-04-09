namespace LinkittyDo.Api.Services;

public static class SimulationIdGenerator
{
    private const string SimUserPrefix = "SIM-USR-";
    private const string SimGamePrefix = "SIM-GAME-";

    public static string GenerateSimUserId()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var random = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        return $"{SimUserPrefix}{timestamp}-{random}";
    }

    public static string GenerateSimGameId()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var random = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        return $"{SimGamePrefix}{timestamp}-{random}";
    }

    public static bool IsSimulatedUser(string uniqueId) => uniqueId.StartsWith(SimUserPrefix, StringComparison.OrdinalIgnoreCase);

    public static bool IsSimulatedGame(string gameId) => gameId.StartsWith(SimGamePrefix, StringComparison.OrdinalIgnoreCase);
}
