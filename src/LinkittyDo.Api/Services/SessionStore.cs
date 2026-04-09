using LinkittyDo.Api.Models;
using System.Collections.Concurrent;

namespace LinkittyDo.Api.Services;

/// <summary>
/// Interface for in-memory game session storage.
/// This must be Singleton to survive across Scoped service lifetimes.
/// </summary>
public interface ISessionStore
{
    GameSession? Get(Guid sessionId);
    void Set(Guid sessionId, GameSession session);
    bool Remove(Guid sessionId);
    IEnumerable<KeyValuePair<Guid, GameSession>> GetAll();
    int Count { get; }
}

/// <summary>
/// Thread-safe in-memory session store using ConcurrentDictionary.
/// Registered as Singleton so sessions persist across Scoped service lifetimes.
/// </summary>
public class InMemorySessionStore : ISessionStore
{
    private readonly ConcurrentDictionary<Guid, GameSession> _sessions = new();

    public GameSession? Get(Guid sessionId)
    {
        return _sessions.GetValueOrDefault(sessionId);
    }

    public void Set(Guid sessionId, GameSession session)
    {
        _sessions[sessionId] = session;
    }

    public bool Remove(Guid sessionId)
    {
        return _sessions.TryRemove(sessionId, out _);
    }

    public IEnumerable<KeyValuePair<Guid, GameSession>> GetAll()
    {
        return _sessions;
    }

    public int Count => _sessions.Count;
}
