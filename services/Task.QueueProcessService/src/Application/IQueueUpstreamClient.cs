namespace QueueProcessService.Application;

public interface IQueueUpstreamClient
{
    global::System.Threading.Tasks.Task<bool> PlayerExistsAsync(Guid playerId, CancellationToken cancellationToken);

    global::System.Threading.Tasks.Task<decimal?> GetCurrentRatingAsync(Guid playerId, CancellationToken cancellationToken);
}