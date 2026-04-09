namespace LinkittyDo.Api.Models;

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class UserRole
{
    public string UserId { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
    public Role? Role { get; set; }
}
