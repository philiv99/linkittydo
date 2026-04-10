using LinkittyDo.Api.Data;
using LinkittyDo.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LinkittyDo.Api.Services;

public class PhraseAdminService : IPhraseAdminService
{
    private readonly LinkittyDoDbContext _context;

    public PhraseAdminService(LinkittyDoDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<GamePhrase>> GetPhrasesAsync(int page = 1, int pageSize = 20, bool? isActive = null)
    {
        var query = _context.GamePhrases.AsQueryable();
        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetPhraseCountAsync(bool? isActive = null)
    {
        var query = _context.GamePhrases.AsQueryable();
        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);
        return await query.CountAsync();
    }

    public async Task<GamePhrase> CreatePhraseAsync(string text, int difficulty = 0)
    {
        var phrase = new GamePhrase
        {
            UniqueId = GamePhrase.GenerateUniqueId(),
            Text = text.Trim(),
            WordCount = text.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
            Difficulty = difficulty,
            GeneratedByLlm = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.GamePhrases.Add(phrase);
        await _context.SaveChangesAsync();
        return phrase;
    }

    public async Task<GamePhrase?> UpdatePhraseAsync(string uniqueId, string text, int difficulty)
    {
        var phrase = await _context.GamePhrases.FindAsync(uniqueId);
        if (phrase == null) return null;

        phrase.Text = text.Trim();
        phrase.WordCount = text.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        phrase.Difficulty = difficulty;
        await _context.SaveChangesAsync();
        return phrase;
    }

    public async Task<bool> SetPhraseActiveStatusAsync(string uniqueId, bool isActive)
    {
        var phrase = await _context.GamePhrases.FindAsync(uniqueId);
        if (phrase == null) return false;

        phrase.IsActive = isActive;
        await _context.SaveChangesAsync();
        return true;
    }
}
