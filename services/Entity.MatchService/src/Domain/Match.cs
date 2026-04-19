using System.ComponentModel.DataAnnotations;

namespace Entity.MatchService.Domain;

public sealed class Match
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(32)]
    public string Status { get; set; } = MatchStatus.Pending;

    [Required]
    public string PlayerIdsJson { get; set; } = "[]";


    [MaxLength(64)]
    public string? QueueId { get; set; }

    public Guid? Winner { get; set; }

    [MaxLength(255)]
    public string? Result { get; set; }
}