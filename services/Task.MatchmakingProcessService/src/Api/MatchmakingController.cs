using MatchmakingProcessService.Application;
using MatchmakingProcessService.Domain;
using Microsoft.AspNetCore.Mvc;

namespace MatchmakingProcessService.Api;

[ApiController]
[Route("mm")]
public sealed class MatchmakingController : ControllerBase
{
    private readonly IMatchmakingOrchestrator orchestrator;

    public MatchmakingController(IMatchmakingOrchestrator orchestrator)
    {
        this.orchestrator = orchestrator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(MatchInitResultDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> InitializeMatchmaking([FromBody] MatchInitRequestDto request, CancellationToken cancellationToken)
    {
        var validationErrors = ValidateRequest(request);
        if (validationErrors.Count > 0)
        {
            return BadRequest(new ValidationProblemDetails(validationErrors));
        }

        var command = new MatchInitializationRequest
        {
            PlayerIds = request.PlayerIds,
            QueueId = request.QueueId
        };

        try
        {
            var result = await orchestrator.InitializeAsync(command, cancellationToken);
            return Accepted(ToDto(result));
        }
        catch (MatchmakingConflictException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Matchmaking initialization conflict.",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
        catch (HttpRequestException exception)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
            {
                Title = "Dependent service unavailable.",
                Detail = exception.Message,
                Status = StatusCodes.Status503ServiceUnavailable
            });
        }
    }

    private static MatchInitResultDto ToDto(MatchInitializationResult result)
    {
        return new MatchInitResultDto
        {
            MatchId = result.MatchId,
            Status = result.Status,
            PlayerIds = result.PlayerIds,
            DequeuedPlayerIds = result.DequeuedPlayerIds,
            FailedToDequeuePlayerIds = result.FailedToDequeuePlayerIds
        };
    }

    private static Dictionary<string, string[]> ValidateRequest(MatchInitRequestDto? request)
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
}
