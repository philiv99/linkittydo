using LinkittyDo.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LinkittyDo.Api.Services;

public interface IRoleService
{
    Task<IList<string>> GetUserRolesAsync(string userId);
    Task AssignRoleAsync(string userId, string roleName);
    Task RemoveRoleAsync(string userId, string roleName);
}

public class RoleService : IRoleService
{
    private readonly Data.LinkittyDoDbContext _dbContext;

    public RoleService(Data.LinkittyDoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IList<string>> GetUserRolesAsync(string userId)
    {
        return await _dbContext.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(_dbContext.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
            .ToListAsync();
    }

    public async Task AssignRoleAsync(string userId, string roleName)
    {
        var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
        if (role == null) return;

        var exists = await _dbContext.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == role.Id);
        if (exists) return;

        _dbContext.UserRoles.Add(new UserRole
        {
            UserId = userId,
            RoleId = role.Id,
            AssignedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();
    }

    public async Task RemoveRoleAsync(string userId, string roleName)
    {
        var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
        if (role == null) return;

        var userRole = await _dbContext.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == role.Id);
        if (userRole == null) return;

        _dbContext.UserRoles.Remove(userRole);
        await _dbContext.SaveChangesAsync();
    }
}
