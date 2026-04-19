using System.ComponentModel.DataAnnotations;

namespace QueueProcessService.Domain;

public sealed class QueueTicket
{
    [Required]
    public Guid PlayerId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Sr { get; set; }

    [Required]
    [MaxLength(32)]
    public string Status { get; set; } = QueueStatus.Waiting;

    [Required]
    public DateTimeOffset QueuedAt { get; set; }

    public DateTimeOffset? MatchedAt { get; set; }
}