using LinkittyDo.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LinkittyDo.Api.Data;

/// <summary>
/// EF Core implementation of IUserRepository.
/// </summary>
public class EfUserRepository : IUserRepository
{
    private readonly LinkittyDoDbContext _context;

    public EfUserRepository(LinkittyDoDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _context.Users.Where(u => u.IsActive).ToListAsync();
    }

    public async Task<User?> GetByIdAsync(string uniqueId)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.UniqueId == uniqueId && u.IsActive);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
    }

    public async Task<User?> GetByNameAsync(string name)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Name == name && u.IsActive);
    }

    public async Task<User> CreateAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<User?> UpdateAsync(User user)
    {
        var existing = await _context.Users.FindAsync(user.UniqueId);
        if (existing == null) return null;

        _context.Entry(existing).CurrentValues.SetValues(user);
        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(string uniqueId)
    {
        var user = await _context.Users.FindAsync(uniqueId);
        if (user == null) return false;

        // Soft delete
        user.IsActive = false;
        user.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> IsNameAvailableAsync(string name, string? excludeUserId = null)
    {
        var query = _context.Users.Where(u => u.Name == name && u.IsActive);
        if (excludeUserId != null)
            query = query.Where(u => u.UniqueId != excludeUserId);
        return !await query.AnyAsync();
    }

    public async Task<bool> IsEmailAvailableAsync(string email, string? excludeUserId = null)
    {
        var query = _context.Users.Where(u => u.Email == email && u.IsActive);
        if (excludeUserId != null)
            query = query.Where(u => u.UniqueId != excludeUserId);
        return !await query.AnyAsync();
    }
}
