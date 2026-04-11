using LinkittyDo.Api.Models;

namespace LinkittyDo.Api.Services;

public interface IAdminService
{
    Task<DashboardStats> GetDashboardStatsAsync();
    Task<IReadOnlyList<User>> GetUsersAsync(int page = 1, int pageSize = 20, bool? isSimulated = null, string? search = null);
    Task<int> GetUserCountAsync(bool? isSimulated = null, string? search = null);
    Task<bool> SetUserActiveStatusAsync(string uniqueId, bool isActive);
    Task<bool> HardDeleteUserAsync(string uniqueId);
    Task<PlayerStats?> GetPlayerAnalyticsAsync(string userId);
    Task<IList<string>> GetUserRolesAsync(string userId);
    Task<bool> AssignRoleAsync(string userId, string roleName);
    Task<bool> RemoveRoleAsync(string userId, string roleName);
}
