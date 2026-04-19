using System.ComponentModel.DataAnnotations;

namespace QueueProcessService.Domain;

public sealed class QueueTicket
{
    [Required]
    public Guid PlayerId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Sr { get; set; }

    [Required]
    public DateTimeOffset QueuedAt { get; set; }
}