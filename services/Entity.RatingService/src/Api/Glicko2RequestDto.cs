using System.ComponentModel.DataAnnotations;

namespace Entity.RatingService.Api;

public sealed class Glicko2RequestDto
{
    [Required]
    public Guid PlayerId { get; init; } = Guid.Empty;

    [Required]
    public string MatchResult { get; init; } = string.Empty;

    public IReadOnlyList<decimal> OpponentRatings { get; init; } = Array.Empty<decimal>();
}
