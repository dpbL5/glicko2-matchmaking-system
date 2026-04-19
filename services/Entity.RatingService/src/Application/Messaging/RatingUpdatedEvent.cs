namespace MatchmakingProcessService.Application.Messaging;

public sealed class RatingUpdatedEvent
{
    public Guid MatchId { get; init; }

    public IReadOnlyList<PlayerRatingUpdate> UpdatedRatings { get; init; } = [];

    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed class PlayerRatingUpdate
{
    public Guid PlayerId { get; init; }

    public decimal OldRating { get; init; }

    public decimal NewRating { get; init; }

    public decimal NewRd { get; init; }

    public decimal NewVolatility { get; init; }
}
