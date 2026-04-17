namespace Entity.PlayerService.Api;

using Entity.PlayerService.Application;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("player")]
public sealed class PlayerController : ControllerBase
{
    private readonly IPlayerRepository repository;

    public PlayerController(IPlayerRepository repository)
    {
        this.repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> GetPlayers(CancellationToken cancellationToken)
    {
        var players = await repository.GetAllAsync(cancellationToken);
        var response = players
            .Select(player => new PlayerDto
            {
                Id = player.Id,
                Name = player.Name
            })
            .ToList();

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPlayerById(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["id"] = new[] { "Player id is required." }
            }));
        }

        var player = await repository.GetByIdAsync(id, cancellationToken);
        if (player is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Player not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(new PlayerDto
        {
            Id = player.Id,
            Name = player.Name
        });
    }
}
