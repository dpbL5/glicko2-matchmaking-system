namespace MatchmakingProcessService.Application.Messaging;

public sealed class RatingUpdatedEvent
{
    public Guid MatchId { get; init; }

    public DateTimeOffset OccurredAt { get; init; }
}
