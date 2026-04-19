namespace QueueProcessService.Application;

public sealed class MatchInitializationResult
{
    public Guid MatchId { get; init; }

    public IReadOnlyList<Guid> PlayerIds { get; init; } = [];

    public IReadOnlyList<Guid> DequeuedPlayerIds { get; init; } = [];
}