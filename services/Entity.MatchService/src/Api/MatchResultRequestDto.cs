using System.ComponentModel.DataAnnotations;

namespace Entity.MatchService.Api;

public sealed class MatchResultRequestDto
{
    [Required]
    public Guid Winner { get; init; }

    [Required]
    [StringLength(255)]
    public string Result { get; init; } = string.Empty;
}