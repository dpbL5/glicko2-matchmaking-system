namespace MatchmakingProcessService.Application.Messaging;

public sealed class MatchEndedEvent
{
    public Guid MatchId { get; init; }

    public Guid Winner { get; init; }

    public string Result { get; init; } = string.Empty;

    public IReadOnlyList<MatchPlayerOutcome> PlayerOutcomes { get; init; } = [];

    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed class MatchPlayerOutcome
{
    public Guid PlayerId { get; init; }

    public string MatchResult { get; init; } = string.Empty;

    public IReadOnlyList<decimal> OpponentRatings { get; init; } = [];
}
