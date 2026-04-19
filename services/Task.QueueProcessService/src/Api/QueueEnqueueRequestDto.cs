using System.ComponentModel.DataAnnotations;

namespace QueueProcessService.Api;

public sealed class QueueEnqueueRequestDto
{
    [Required]
    public Guid PlayerId { get; init; }
}