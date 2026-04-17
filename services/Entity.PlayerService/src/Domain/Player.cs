using System.ComponentModel.DataAnnotations;

namespace Entity.PlayerService.Domain;

public sealed class Player
{
    [Required]
    public Guid Id { get; set; }

    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;
}