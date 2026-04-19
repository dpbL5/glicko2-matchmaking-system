using QueueProcessService.Domain;

namespace QueueProcessService.Application;

public interface IQueueRepository
{
    Task<QueueTicket> UpsertAsync(QueueTicket ticket, CancellationToken cancellationToken);

    Task<QueueTicket?> GetByPlayerIdAsync(Guid playerId, CancellationToken cancellationToken);

    Task<bool> RemoveAsync(Guid playerId, CancellationToken cancellationToken);

    Task<IReadOnlyList<IReadOnlyList<Guid>>> FindMatchCandidateGroupsAsync(int minPlayers, decimal maxSrDelta, CancellationToken cancellationToken);
}