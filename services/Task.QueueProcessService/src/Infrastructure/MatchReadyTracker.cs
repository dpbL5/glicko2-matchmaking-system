using System.Collections.Concurrent;
using QueueProcessService.Application;

namespace QueueProcessService.Infrastructure;

public sealed class MatchReadyTracker : IMatchReadyTracker
{
    private readonly ConcurrentDictionary<Guid, MatchReadyConfirmation> eventsByPlayerId = new();

    public void Store(Guid matchId, IReadOnlyList<Guid> playerIds, DateTimeOffset occurredAt)
    {
        var confirmation = new MatchReadyConfirmation
        {
            MatchId = matchId,
            PlayerIds = playerIds,
            OccurredAt = occurredAt
        };

        foreach (var playerId in playerIds)
        {
            eventsByPlayerId[playerId] = confirmation;
        }
    }

    public bool TryTake(Guid playerId, Guid? expectedMatchId, out MatchReadyConfirmation confirmation)
    {
        if (!eventsByPlayerId.TryGetValue(playerId, out confirmation!))
        {
            confirmation = new MatchReadyConfirmation();
            return false;
        }

        if (expectedMatchId is not null && confirmation.MatchId != expectedMatchId.Value)
        {
            confirmation = new MatchReadyConfirmation();
            return false;
        }

        eventsByPlayerId.TryRemove(playerId, out _);
        return true;
    }
}