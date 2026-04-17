namespace Entity.RatingService.Api;

public sealed class PlayerRatingDto
{
    public Guid PlayerId { get; init; } = Guid.Empty;

    public decimal Rating { get; init; }

    public decimal Rd { get; init; }

    public decimal Volatility { get; init; }
}
