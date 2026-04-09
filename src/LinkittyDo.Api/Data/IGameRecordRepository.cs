using LinkittyDo.Api.Models;

namespace LinkittyDo.Api.Data;

/// <summary>
/// Repository interface for GameRecord data access.
/// GameRecords are a separate aggregate from Users, accessed independently.
/// </summary>
public interface IGameRecordRepository
{
    Task<GameRecord?> GetByGameIdAsync(string gameId);
    Task<IEnumerable<GameRecord>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 20);
    Task<GameRecord> CreateAsync(GameRecord record);
    Task<GameRecord?> UpdateAsync(GameRecord record);
    Task<int> GetCountByUserIdAsync(string userId);
}
