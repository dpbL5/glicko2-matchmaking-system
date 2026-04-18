using System.Text.Json;
using Entity.MatchService.Application;
using Entity.MatchService.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Entity.MatchService.Api;

[ApiController]
[Route("match")]
public sealed class MatchController : ControllerBase
{
    private readonly IMatchRepository repository;

    public MatchController(IMatchRepository repository)
    {
        this.repository = repository;
    }

    [HttpPost]
    public async Task<IActionResult> CreateMatch([FromBody] MatchCreateRequestDto request, CancellationToken cancellationToken)
    {
        var validationErrors = ValidateCreateRequest(request);
        if (validationErrors.Count > 0)
        {
            return BadRequest(new ValidationProblemDetails(validationErrors));
        }

        var created = await repository.CreateAsync(new Match
        {
            Id = Guid.NewGuid(),
            Status = MatchStatus.Pending,
            PlayerIdsJson = JsonSerializer.Serialize(request.PlayerIds),
            QueueId = string.IsNullOrWhiteSpace(request.QueueId) ? null : request.QueueId.Trim()
        }, cancellationToken);

        return CreatedAtAction(nameof(GetMatchById), new { id = created.Id }, ToDto(created));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetMatchById(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["id"] = new[] { "Match id is required." }
            }));
        }

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

    [HttpPost("{id:guid}/result")]
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

        var updated = await repository.UpdateResultAsync(id, request.Winner.Trim(), request.Result.Trim(), cancellationToken);
        if (updated is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Match not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(new MatchResultResponseDto
        {
            MatchId = updated.Id,
            Status = updated.Status
        });
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

        if (string.IsNullOrWhiteSpace(request.Winner))
        {
            errors["winner"] = new[] { "Winner is required." };
        }

        if (string.IsNullOrWhiteSpace(request.Result))
        {
            errors["result"] = new[] { "Result is required." };
        }

        return errors;
    }
}