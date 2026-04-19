namespace QueueProcessService.Api;

public sealed class QueueSearchResponseDto
{
    public bool Matched { get; init; }

    public IReadOnlyList<Guid> PlayerIds { get; init; } = [];
}