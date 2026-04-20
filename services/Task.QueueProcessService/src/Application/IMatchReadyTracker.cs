namespace QueueProcessService.Application;

public interface IMatchReadyTracker
{
    void Store(Guid matchId, IReadOnlyList<Guid> playerIds, DateTimeOffset occurredAt);

    bool TryTake(Guid playerId, Guid? expectedMatchId, out MatchReadyConfirmation confirmation);
}

public sealed class MatchReadyConfirmation
{
    public Guid MatchId { get; init; }

    public IReadOnlyList<Guid> PlayerIds { get; init; } = [];

    public DateTimeOffset OccurredAt { get; init; }
}