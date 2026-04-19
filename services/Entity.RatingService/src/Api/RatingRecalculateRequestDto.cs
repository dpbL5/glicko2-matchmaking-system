using System.ComponentModel.DataAnnotations;

namespace Entity.RatingService.Api;

public sealed class RatingRecalculateRequestDto
{
    public Guid? MatchId { get; init; }

    [Required]
    public string MatchResult { get; init; } = string.Empty;

    public IReadOnlyList<decimal> OpponentRatings { get; init; } = Array.Empty<decimal>();
}
