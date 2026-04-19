namespace Entity.PlayerService.Api;

using Entity.PlayerService.Application;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("players")]
public sealed class PlayerController : ControllerBase
{
    private readonly IPlayerRepository repository;

    public PlayerController(IPlayerRepository repository)
    {
        this.repository = repository;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PlayerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPlayers(CancellationToken cancellationToken)
    {
        try
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
        catch (DbUpdateException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
            {
                Title = "Database is unavailable.",
                Detail = ex.Message,
                Status = StatusCodes.Status503ServiceUnavailable
            });
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PlayerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetPlayerById(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["id"] = new[] { "Player id is required." }
            }));
        }

        try
        {
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
        catch (DbUpdateException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
            {
                Title = "Database is unavailable.",
                Detail = ex.Message,
                Status = StatusCodes.Status503ServiceUnavailable
            });
        }
    }
}
