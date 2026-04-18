using System.ComponentModel.DataAnnotations;

namespace Entity.MatchService.Api;

public sealed class MatchResultRequestDto
{
    [Required]
    [StringLength(255)]
    public string Winner { get; init; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string Result { get; init; } = string.Empty;
}