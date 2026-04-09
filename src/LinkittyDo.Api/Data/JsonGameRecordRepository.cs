using LinkittyDo.Api.Models;

namespace LinkittyDo.Api.Data;

/// <summary>
/// JSON file-based implementation of IGameRecordRepository.
/// GameRecords are currently embedded in User JSON files under the Games property.
/// This adapter delegates to IUserRepository for persistence.
/// </summary>
public class JsonGameRecordRepository : IGameRecordRepository
{
    private readonly IUserRepository _userRepository;

    public JsonGameRecordRepository(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<GameRecord?> GetByGameIdAsync(string gameId)
    {
        var users = await _userRepository.GetAllAsync();
        foreach (var user in users)
        {
            var record = user.Games.FirstOrDefault(g => g.GameId == gameId);
            if (record != null)
            {
                record.UserId = user.UniqueId;
                return record;
            }
        }
        return null;
    }

    public async Task<IEnumerable<GameRecord>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 20)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return Enumerable.Empty<GameRecord>();

        return user.Games
            .OrderByDescending(g => g.PlayedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(g => { g.UserId = userId; return g; });
    }

    public async Task<GameRecord> CreateAsync(GameRecord record)
    {
        var user = await _userRepository.GetByIdAsync(record.UserId);
        if (user == null) throw new InvalidOperationException("USER_NOT_FOUND");

        user.Games.Add(record);
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);
        return record;
    }

    public async Task<GameRecord?> UpdateAsync(GameRecord record)
    {
        var user = await _userRepository.GetByIdAsync(record.UserId);
        if (user == null) return null;

        var index = user.Games.FindIndex(g => g.GameId == record.GameId);
        if (index < 0) return null;

        user.Games[index] = record;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);
        return record;
    }

    public async Task<int> GetCountByUserIdAsync(string userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        return user?.Games.Count ?? 0;
    }
}
