namespace Entity.RatingService.Api;

public sealed class Glicko2ResponseDto
{
    public Guid PlayerId { get; init; } = Guid.Empty;

    public decimal Rating { get; init; }

    public decimal Rd { get; init; }

    public decimal Volatility { get; init; }
}
