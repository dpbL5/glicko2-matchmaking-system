using QueueProcessService.Domain;

namespace QueueProcessService.Application;

public interface IQueueRepository
{
    Task<QueueTicket> UpsertAsync(QueueTicket ticket, CancellationToken cancellationToken);

    Task<QueueTicket?> GetByPlayerIdAsync(Guid playerId, CancellationToken cancellationToken);

    Task<IReadOnlyList<QueueTicket>> GetQueuedPlayersAsync(CancellationToken cancellationToken);

    // Removes ticket when player is matched.
    Task<bool> MarkMatchedAsync(Guid playerId, CancellationToken cancellationToken);

    Task<bool> RemoveAsync(Guid playerId, CancellationToken cancellationToken);
}