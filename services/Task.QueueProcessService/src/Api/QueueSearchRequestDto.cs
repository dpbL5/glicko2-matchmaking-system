using System.ComponentModel.DataAnnotations;

namespace QueueProcessService.Api;

public sealed class QueueSearchRequestDto
{
    [Required]
    public Guid PlayerId { get; init; }

    [Range(2, int.MaxValue)]
    public int? MinPlayers { get; init; }

    [Range(0.01, double.MaxValue)]
    public decimal? MaxSrDelta { get; init; }
}