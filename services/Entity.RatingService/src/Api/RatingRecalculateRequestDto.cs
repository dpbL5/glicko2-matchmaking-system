using System.ComponentModel.DataAnnotations;

namespace Entity.RatingService.Api;

public sealed class RatingRecalculateRequestDto
{
    [Required]
    public string MatchResult { get; init; } = string.Empty;

    public IReadOnlyList<decimal> OpponentRatings { get; init; } = Array.Empty<decimal>();
}
