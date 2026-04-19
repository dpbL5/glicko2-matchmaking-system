namespace MatchmakingProcessService.Application;

public sealed class MatchInitializationResult
{
    public Guid MatchId { get; init; }

    public string Status { get; init; } = string.Empty;

    public IReadOnlyList<Guid> PlayerIds { get; init; } = [];

    public IReadOnlyList<Guid> DequeuedPlayerIds { get; init; } = [];

    public IReadOnlyList<Guid> FailedToDequeuePlayerIds { get; init; } = [];
}
