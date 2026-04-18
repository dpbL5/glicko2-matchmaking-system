using System.ComponentModel.DataAnnotations;

namespace Entity.MatchService.Api;

public sealed class MatchCreateRequestDto
{
    [Required]
    [MinLength(2)]
    public List<Guid> PlayerIds { get; init; } = [];

    [StringLength(64)]
    public string? QueueId { get; init; }
}