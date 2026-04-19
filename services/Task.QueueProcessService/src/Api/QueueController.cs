using QueueProcessService.Application;
using QueueProcessService.Domain;
using Microsoft.AspNetCore.Mvc;

namespace QueueProcessService.Api;

[ApiController]
[Route("queue")]
public sealed class QueueController : ControllerBase
{
    private const int DefaultMinPlayers = 2;
    private const decimal DefaultMaxSrDelta = 50m;

    private readonly IQueueRepository repository;
    private readonly IQueueUpstreamClient upstreamClient;

    public QueueController(IQueueRepository repository, IQueueUpstreamClient upstreamClient)
    {
        this.repository = repository;
        this.upstreamClient = upstreamClient;
    }

    [HttpPost]
    public async global::System.Threading.Tasks.Task<IActionResult> Enqueue([FromBody] QueueEnqueueRequestDto request, CancellationToken cancellationToken)
    {
        var validationErrors = ValidateEnqueueRequest(request);
        if (validationErrors.Count > 0)
        {
            return BadRequest(new ValidationProblemDetails(validationErrors));
        }

        try
        {
            var playerExists = await upstreamClient.PlayerExistsAsync(request.PlayerId, cancellationToken);
            if (!playerExists)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Player not found.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            var rating = await upstreamClient.GetCurrentRatingAsync(request.PlayerId, cancellationToken);
            if (rating is null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Rating not found.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            var ticket = await repository.UpsertAsync(new QueueTicket
            {
                PlayerId = request.PlayerId,
                Sr = rating.Value,
                Status = QueueStatus.Waiting,
                QueuedAt = DateTimeOffset.UtcNow,
                MatchedAt = null
            }, cancellationToken);

            return CreatedAtAction(nameof(GetQueueStatus), new { playerId = ticket.PlayerId }, ToDto(ticket));
        }
        catch (HttpRequestException exception)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
            {
                Title = "Upstream service unavailable.",
                Status = StatusCodes.Status503ServiceUnavailable,
                Detail = exception.Message
            });
        }
    }

    [HttpGet("{playerId:guid}")]
    public async global::System.Threading.Tasks.Task<IActionResult> GetQueueStatus(Guid playerId, CancellationToken cancellationToken)
    {
        if (playerId == Guid.Empty)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["playerId"] = new[] { "Player id is required." }
            }));
        }

        var ticket = await repository.GetByPlayerIdAsync(playerId, cancellationToken);
        if (ticket is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Queue ticket not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        if (ticket.Status == QueueStatus.Waiting)
        {
            await repository.SearchAndMatchAsync(playerId, DefaultMinPlayers, DefaultMaxSrDelta, cancellationToken);
            ticket = await repository.GetByPlayerIdAsync(playerId, cancellationToken);
            if (ticket is null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Queue ticket not found.",
                    Status = StatusCodes.Status404NotFound
                });
            }
        }

        return Ok(ToDto(ticket));
    }

    [HttpDelete("{playerId:guid}")]
    public async global::System.Threading.Tasks.Task<IActionResult> Dequeue(Guid playerId, CancellationToken cancellationToken)
    {
        if (playerId == Guid.Empty)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["playerId"] = new[] { "Player id is required." }
            }));
        }

        var removed = await repository.RemoveAsync(playerId, cancellationToken);
        if (!removed)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Queue ticket not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return NoContent();
    }

    [HttpPost("search")]
    public async global::System.Threading.Tasks.Task<IActionResult> Search([FromBody] QueueSearchRequestDto request, CancellationToken cancellationToken)
    {
        var validationErrors = ValidateSearchRequest(request);
        if (validationErrors.Count > 0)
        {
            return BadRequest(new ValidationProblemDetails(validationErrors));
        }

        var minPlayers = request.MinPlayers ?? DefaultMinPlayers;
        var maxSrDelta = request.MaxSrDelta ?? DefaultMaxSrDelta;

        var ticket = await repository.GetByPlayerIdAsync(request.PlayerId, cancellationToken);
        if (ticket is null || ticket.Status != QueueStatus.Waiting)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Queue ticket not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var matchedTickets = await repository.SearchAndMatchAsync(request.PlayerId, minPlayers, maxSrDelta, cancellationToken);
        var matched = matchedTickets.Count >= minPlayers;

        return Ok(new QueueSearchResponseDto
        {
            Matched = matched,
            PlayerIds = matched ? matchedTickets.Select(item => item.PlayerId).ToList() : []
        });
    }

    private static QueueTicketDto ToDto(QueueTicket ticket)
    {
        return new QueueTicketDto
        {
            PlayerId = ticket.PlayerId,
            Sr = ticket.Sr,
            Status = ticket.Status,
            QueuedAt = ticket.QueuedAt
        };
    }

    private static Dictionary<string, string[]> ValidateEnqueueRequest(QueueEnqueueRequestDto? request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request is null)
        {
            errors["request"] = new[] { "Request body is required." };
            return errors;
        }

        if (request.PlayerId == Guid.Empty)
        {
            errors["playerId"] = new[] { "Player id is required." };
        }

        if (request.Sr <= 0)
        {
            errors["sr"] = new[] { "SR must be greater than zero." };
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateSearchRequest(QueueSearchRequestDto? request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request is null)
        {
            errors["request"] = new[] { "Request body is required." };
            return errors;
        }

        if (request.PlayerId == Guid.Empty)
        {
            errors["playerId"] = new[] { "Player id is required." };
        }

        if (request.MinPlayers is not null && request.MinPlayers.Value < 2)
        {
            errors["minPlayers"] = new[] { "Minimum players must be at least 2." };
        }

        if (request.MaxSrDelta is not null && request.MaxSrDelta.Value <= 0)
        {
            errors["maxSrDelta"] = new[] { "Maximum SR delta must be greater than zero." };
        }

        return errors;
    }
}