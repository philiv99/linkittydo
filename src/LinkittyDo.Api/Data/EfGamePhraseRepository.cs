using LinkittyDo.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LinkittyDo.Api.Data;

/// <summary>
/// EF Core implementation of IGamePhraseRepository.
/// </summary>
public class EfGamePhraseRepository : IGamePhraseRepository
{
    private readonly LinkittyDoDbContext _context;

    public EfGamePhraseRepository(LinkittyDoDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<GamePhrase>> GetAllAsync()
    {
        return await _context.GamePhrases.Where(p => p.IsActive).ToListAsync();
    }

    public async Task<GamePhrase?> GetByIdAsync(string uniqueId)
    {
        return await _context.GamePhrases.FirstOrDefaultAsync(p => p.UniqueId == uniqueId && p.IsActive);
    }

    public async Task<GamePhrase?> GetByTextAsync(string text)
    {
        return await _context.GamePhrases.FirstOrDefaultAsync(
            p => p.Text.ToLower() == text.ToLower() && p.IsActive);
    }

    public async Task<GamePhrase> CreateAsync(GamePhrase phrase)
    {
        _context.GamePhrases.Add(phrase);
        await _context.SaveChangesAsync();
        return phrase;
    }

    public async Task<IEnumerable<GamePhrase>> CreateManyAsync(IEnumerable<GamePhrase> phrases)
    {
        var list = phrases.ToList();
        _context.GamePhrases.AddRange(list);
        await _context.SaveChangesAsync();
        return list;
    }

    public async Task<bool> DeleteAsync(string uniqueId)
    {
        var phrase = await _context.GamePhrases.FindAsync(uniqueId);
        if (phrase == null) return false;

        // Soft delete
        phrase.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsByTextAsync(string text)
    {
        return await _context.GamePhrases.AnyAsync(
            p => p.Text.ToLower() == text.ToLower() && p.IsActive);
    }

    public async Task<int> GetCountAsync()
    {
        return await _context.GamePhrases.CountAsync(p => p.IsActive);
    }
}
