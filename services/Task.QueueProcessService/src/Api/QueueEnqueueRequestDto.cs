using System.ComponentModel.DataAnnotations;

namespace QueueProcessService.Api;

public sealed class QueueEnqueueRequestDto
{
    [Required]
    public Guid PlayerId { get; init; }

    [Range(0.01, double.MaxValue)]
    public decimal Sr { get; init; }
}