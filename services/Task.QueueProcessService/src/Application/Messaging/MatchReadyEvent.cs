namespace MatchmakingProcessService.Application.Messaging;

public sealed class MatchReadyEvent
{
    public Guid MatchId { get; init; }

    public IReadOnlyList<Guid> PlayerIds { get; init; } = [];

    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}