namespace Entity.PlayerService.Api;

using System;

public record class PlayerDto
{
    public Guid Id { get; init; } = Guid.Empty;

    public string Name { get; init; } = string.Empty;
}
