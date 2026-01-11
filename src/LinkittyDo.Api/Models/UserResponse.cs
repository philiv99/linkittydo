namespace LinkittyDo.Api.Models;

public class UserResponse
{
    public string UniqueId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int LifetimePoints { get; set; }
    public int PreferredDifficulty { get; set; }
    public int GamesPlayed { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DifficultyResponse
{
    public string UniqueId { get; set; } = string.Empty;
    public int PreferredDifficulty { get; set; }
}

public class PointsResponse
{
    public string UniqueId { get; set; } = string.Empty;
    public int LifetimePoints { get; set; }
    public int PointsAdded { get; set; }
}

public class AvailabilityResponse
{
    public bool Available { get; set; }
}

public class ErrorResponse
{
    public ErrorDetail Error { get; set; } = new();
}

public class ErrorDetail
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
