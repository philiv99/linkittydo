using LinkittyDo.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LinkittyDo.Api.Data;

/// <summary>
/// EF Core implementation of IGameRecordRepository.
/// </summary>
public class EfGameRecordRepository : IGameRecordRepository
{
    private readonly LinkittyDoDbContext _context;

    public EfGameRecordRepository(LinkittyDoDbContext context)
    {
        _context = context;
    }

    public async Task<GameRecord?> GetByGameIdAsync(string gameId)
    {
        return await _context.GameRecords.FirstOrDefaultAsync(r => r.GameId == gameId);
    }

    public async Task<GameRecord?> GetByGameIdWithEventsAsync(string gameId)
    {
        var record = await _context.GameRecords.FirstOrDefaultAsync(r => r.GameId == gameId);
        if (record == null) return null;

        record.Events = await _context.GameEvents
            .Where(e => e.GameId == gameId)
            .OrderBy(e => e.SequenceNumber)
            .ToListAsync();

        return record;
    }

    public async Task<IEnumerable<GameRecord>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 20)
    {
        return await _context.GameRecords
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.PlayedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<GameRecord>> GetByUserIdWithEventsAsync(string userId, int page = 1, int pageSize = 20)
    {
        var records = await _context.GameRecords
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.PlayedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        if (records.Any())
        {
            var gameIds = records.Select(r => r.GameId).ToList();
            var events = await _context.GameEvents
                .Where(e => gameIds.Contains(e.GameId))
                .OrderBy(e => e.SequenceNumber)
                .ToListAsync();

            foreach (var record in records)
            {
                record.Events = events.Where(e => e.GameId == record.GameId).ToList();
            }
        }

        return records;
    }

    public async Task<GameRecord> CreateAsync(GameRecord record)
    {
        _context.GameRecords.Add(record);
        await _context.SaveChangesAsync();
        return record;
    }

    public async Task<GameRecord?> UpdateAsync(GameRecord record)
    {
        var existing = await _context.GameRecords.FindAsync(record.GameId);
        if (existing == null) return null;

        _context.Entry(existing).CurrentValues.SetValues(record);
        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<int> GetCountByUserIdAsync(string userId)
    {
        return await _context.GameRecords.CountAsync(r => r.UserId == userId);
    }
}
