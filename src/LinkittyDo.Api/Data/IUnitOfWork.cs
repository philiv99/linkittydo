namespace LinkittyDo.Api.Data;

/// <summary>
/// Unit of Work interface for transactional consistency across repositories.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IGamePhraseRepository GamePhrases { get; }
    IGameRecordRepository GameRecords { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
