namespace MatchmakingProcessService.Api;

public sealed class MatchInitRequestDto
{
    public List<Guid> PlayerIds { get; init; } = [];

    public string? QueueId { get; init; }
}
