using System.Net;
using MassTransit;
using MatchmakingProcessService.Application.Messaging;
using Microsoft.AspNetCore.Mvc;

namespace MatchmakingProcessService.Api;

[ApiController]
[Route("mm")]
public sealed class MatchmakingController : ControllerBase
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly IPublishEndpoint publishEndpoint;

    public MatchmakingController(IHttpClientFactory httpClientFactory, IPublishEndpoint publishEndpoint)
    {
        this.httpClientFactory = httpClientFactory;
        this.publishEndpoint = publishEndpoint;
    }

    [HttpPost]
    [ProducesResponseType(typeof(MatchInitResultDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> InitializeMatchmaking([FromBody] MatchInitRequestDto request, CancellationToken cancellationToken)
    {
        var validationErrors = ValidateRequest(request);
        if (validationErrors.Count > 0)
        {
            return BadRequest(new ValidationProblemDetails(validationErrors));
        }

        var playerIds = request.PlayerRatings.Keys.ToList();

        try
        {
            var createdMatch = await CreateMatchAsync(playerIds, cancellationToken);

            await PublishMatchReadyAsync(createdMatch.Id, playerIds, cancellationToken);

            return Accepted(new MatchInitResultDto
            {
                MatchId = createdMatch.Id,
                Status = createdMatch.Status,
                PlayerIds = playerIds,
                DequeuedPlayerIds = []
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

    private async Task<MatchResponseDto> CreateMatchAsync(IReadOnlyList<Guid> playerIds, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("MatchService");

        using var response = await client.PostAsJsonAsync("/matches", new MatchCreateRequestDto
        {
            PlayerIds = playerIds.ToList()
        }, cancellationToken);

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new HttpRequestException("Match service rejected the match initialization payload.", null, response.StatusCode);
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Match service returned {(int)response.StatusCode} ({response.ReasonPhrase}).",
                null,
                response.StatusCode);
        }

        var payload = await response.Content.ReadFromJsonAsync<MatchResponseDto>(cancellationToken: cancellationToken);
        if (payload is null || payload.Id == Guid.Empty)
        {
            throw new HttpRequestException("Match service returned an invalid match payload.");
        }

        return payload;
    }

    private async Task PublishMatchReadyAsync(Guid matchId, IReadOnlyList<Guid> playerIds, CancellationToken cancellationToken)
    {
        await publishEndpoint.Publish(new MatchReadyEvent
        {
            MatchId = matchId,
            PlayerIds = playerIds,
            OccurredAt = DateTimeOffset.UtcNow
        }, cancellationToken);
    }

    private sealed class MatchCreateRequestDto
    {
        public List<Guid> PlayerIds { get; init; } = [];
    }

    private sealed class MatchResponseDto
    {
        public Guid Id { get; init; }

        public string Status { get; init; } = string.Empty;
    }

    private static Dictionary<string, string[]> ValidateRequest(MatchInitRequestDto? request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request is null)
        {
            errors["request"] = new[] { "Request body is required." };
            return errors;
        }

        if (request.PlayerRatings is null || request.PlayerRatings.Count < 2)
        {
            errors["playerRatings"] = new[] { "At least two player ratings are required." };
        }
        else
        {
            if (request.PlayerRatings.Keys.Any(playerId => playerId == Guid.Empty))
            {
                errors["playerRatings"] = new[] { "Player ids must be valid non-empty GUIDs." };
            }
            else if (request.PlayerRatings.Values.Any(rating => rating < 0))
            {
                errors["playerRatings"] = new[] { "Rating values must be non-negative." };
            }
        }

        return errors;
    }
}
