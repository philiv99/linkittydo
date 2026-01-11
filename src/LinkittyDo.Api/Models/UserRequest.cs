using System.ComponentModel.DataAnnotations;

namespace LinkittyDo.Api.Models;

public class CreateUserRequest
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 50 characters")]
    [RegularExpression(@"^[a-zA-Z0-9\s_-]+$", ErrorMessage = "Name can only contain letters, numbers, spaces, underscores, and hyphens")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;
}

public class UpdateUserRequest
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 50 characters")]
    [RegularExpression(@"^[a-zA-Z0-9\s_-]+$", ErrorMessage = "Name can only contain letters, numbers, spaces, underscores, and hyphens")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;
}

public class UpdateDifficultyRequest
{
    [Required(ErrorMessage = "Difficulty is required")]
    [Range(0, 100, ErrorMessage = "Difficulty must be between 0 and 100")]
    public int Difficulty { get; set; }
}

public class AddPointsRequest
{
    [Required(ErrorMessage = "Points is required")]
    [Range(0, int.MaxValue, ErrorMessage = "Points must be a non-negative value")]
    public int Points { get; set; }
}
