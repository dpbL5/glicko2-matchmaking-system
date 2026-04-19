namespace Entity.MatchService.Api;

/// <summary>
/// Returned when MatchEnded event is accepted for async processing.
/// Status reflects the match's current state (before the consumer updates it).
/// </summary>
public sealed class MatchResultResponseDto
{
    public Guid MatchId { get; init; }

    public string Status { get; init; } = string.Empty;
}