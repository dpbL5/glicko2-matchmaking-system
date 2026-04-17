using System.ComponentModel.DataAnnotations;

namespace Entity.RatingService.Api;

public sealed class RatingUpdateRequestDto
{
    [Range(0, double.MaxValue)]
    public decimal Rating { get; init; }

    [Range(0, double.MaxValue)]
    public decimal Rd { get; init; }

    [Range(0, double.MaxValue)]
    public decimal Volatility { get; init; }
}
