using System.ComponentModel.DataAnnotations;

namespace Entity.RatingService.Domain;

public sealed class PlayerRating
{
    [Required]
    public Guid PlayerId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Rating { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Rd { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Volatility { get; set; }
}
