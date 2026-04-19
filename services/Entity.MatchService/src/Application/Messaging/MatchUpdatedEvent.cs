namespace MatchmakingProcessService.Application.Messaging;

public sealed class MatchUpdatedEvent
{
    public Guid MatchId { get; init; }

    public string Status { get; init; } = string.Empty;

    public Guid Winner { get; init; }

    public string Result { get; init; } = string.Empty;

    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
