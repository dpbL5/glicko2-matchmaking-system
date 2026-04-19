using System.Text.Json;
using Entity.MatchService.Application;
using Entity.MatchService.Domain;
using MassTransit;
using MatchmakingProcessService.Application.Messaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Entity.MatchService.Api;

[ApiController]
[Route("matches")]
public sealed class MatchController : ControllerBase
{
    private readonly IMatchRepository repository;
    private readonly IPublishEndpoint publishEndpoint;

    public MatchController(IMatchRepository repository, IPublishEndpoint publishEndpoint)
    {
        this.repository = repository;
        this.publishEndpoint = publishEndpoint;
    }

    [HttpPost]
    [ProducesResponseType(typeof(MatchDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> CreateMatch([FromBody] MatchCreateRequestDto request, CancellationToken cancellationToken)
    {
        var validationErrors = ValidateCreateRequest(request);
        if (validationErrors.Count > 0)
        {
            return BadRequest(new ValidationProblemDetails(validationErrors));
        }

        try
        {
            var created = await repository.CreateAsync(new Match
            {
                Id = Guid.NewGuid(),
                Status = MatchStatus.Pending,
                PlayerIdsJson = JsonSerializer.Serialize(request.PlayerIds),
            }, cancellationToken);

            return CreatedAtAction(nameof(GetMatchById), new { id = created.Id }, ToDto(created));
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
    [ProducesResponseType(typeof(MatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetMatchById(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["id"] = new[] { "Match id is required." }
            }));
        }

        try
        {
            var match = await repository.GetByIdAsync(id, cancellationToken);
            if (match is null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Match not found.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return Ok(ToDto(match));
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

    /// <summary>
    /// Receives match result from GameServer.
    /// Per design §2.8: GameServer → Broker: MatchEnded.
    /// This endpoint validates the request, looks up the match to build PlayerOutcomes,
    /// then publishes MatchEndedEvent. The actual DB update is handled by MatchEndedConsumer.
    /// </summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(MatchResultResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> UpdateResult(Guid id, [FromBody] MatchResultRequestDto request, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["id"] = new[] { "Match id is required." }
            }));
        }

        var validationErrors = ValidateResultRequest(request);
        if (validationErrors.Count > 0)
        {
            return BadRequest(new ValidationProblemDetails(validationErrors));
        }

        try
        {
            // Look up the match to validate it exists and to get player IDs
            var match = await repository.GetByIdAsync(id, cancellationToken);
            if (match is null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Match not found.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            // Build PlayerOutcomes from match data for Glicko-2 recalculation
            var playerIds = DeserializePlayerIds(match.PlayerIdsJson);
            var winnerPlayerId = request.Winner;

            if (!playerIds.Contains(winnerPlayerId))
            {
                return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    ["winner"] = new[] { "Winner must be a player in the match." }
                }));
            }

            var playerOutcomes = playerIds.Select(pid =>
            {
                return new MatchPlayerOutcome
                {
                    PlayerId = pid,
                    MatchResult = pid == winnerPlayerId ? "win" : "loss",
                    OpponentRatings = [] // RatingService will look up from its own DB
                };
            }).ToList();

            // Publish MatchEndedEvent — triggers:
            // - RatingService.MatchEndedConsumer → Glicko-2 recalculation → RatingUpdated
            // - MatchService.MatchEndedConsumer → Update match result     → MatchUpdated
            await publishEndpoint.Publish(new MatchEndedEvent
            {
                MatchId = match.Id,
                Winner = winnerPlayerId,
                Result = request.Result.Trim(),
                PlayerOutcomes = playerOutcomes,
                OccurredAt = DateTimeOffset.UtcNow
            }, cancellationToken);

            return Ok(new MatchResultResponseDto
            {
                MatchId = match.Id,
                Status = match.Status
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

    private static MatchDto ToDto(Match match)
    {
        return new MatchDto
        {
            Id = match.Id,
            Status = match.Status,
            PlayerIds = DeserializePlayerIds(match.PlayerIdsJson)
        };
    }

    private static List<Guid> DeserializePlayerIds(string playerIdsJson)
    {
        return JsonSerializer.Deserialize<List<Guid>>(playerIdsJson) ?? [];
    }

    private static Dictionary<string, string[]> ValidateCreateRequest(MatchCreateRequestDto? request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request is null)
        {
            errors["request"] = new[] { "Request body is required." };
            return errors;
        }

        if (request.PlayerIds is null || request.PlayerIds.Count < 2)
        {
            errors["playerIds"] = new[] { "At least two player ids are required." };
        }
        else
        {
            if (request.PlayerIds.Any(playerId => playerId == Guid.Empty))
            {
                errors["playerIds"] = new[] { "Player ids must be valid non-empty GUIDs." };
            }
            else if (request.PlayerIds.Distinct().Count() != request.PlayerIds.Count)
            {
                errors["playerIds"] = new[] { "Player ids must be unique." };
            }
        }

        if (request.QueueId is not null && string.IsNullOrWhiteSpace(request.QueueId))
        {
            errors["queueId"] = new[] { "Queue id cannot be blank." };
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateResultRequest(MatchResultRequestDto? request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request is null)
        {
            errors["request"] = new[] { "Request body is required." };
            return errors;
        }

        if (request.Winner == Guid.Empty)
        {
            errors["winner"] = new[] { "Winner player id is required." };
        }

        if (string.IsNullOrWhiteSpace(request.Result))
        {
            errors["result"] = new[] { "Result is required." };
        }

        return errors;
    }
}