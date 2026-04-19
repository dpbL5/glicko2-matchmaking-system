namespace QueueProcessService.Application;

public interface IQueueUpstreamClient
{
    Task<bool> PlayerExistsAsync(Guid playerId, CancellationToken cancellationToken);

    Task<decimal?> GetCurrentRatingAsync(Guid playerId, CancellationToken cancellationToken);

    Task<MatchInitializationResult?> InitializeMatchmakingAsync(IReadOnlyDictionary<Guid, decimal> playerRatings, CancellationToken cancellationToken);
}