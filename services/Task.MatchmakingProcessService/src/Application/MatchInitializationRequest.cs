namespace MatchmakingProcessService.Application;

public sealed class MatchInitializationRequest
{
    public IReadOnlyList<Guid> PlayerIds { get; init; } = [];

    public string? QueueId { get; init; }
}
