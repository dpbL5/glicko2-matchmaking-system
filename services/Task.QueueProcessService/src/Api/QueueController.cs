using System.Text.Json;
using QueueProcessService.Application;
using QueueProcessService.Domain;
using Microsoft.AspNetCore.Mvc;

namespace QueueProcessService.Api;

[ApiController]
[Route("queue")]
public sealed class QueueController : ControllerBase
{
    private const int StreamPollIntervalMs = 1000;

    private readonly IQueueRepository repository;
    private readonly IQueueUpstreamClient upstreamClient;

    public QueueController(IQueueRepository repository, IQueueUpstreamClient upstreamClient)
    {
        this.repository = repository;
        this.upstreamClient = upstreamClient;
    }

    [HttpPost]
    public async Task<IActionResult> Enqueue([FromBody] QueueEnqueueRequestDto request, CancellationToken cancellationToken)
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
    public async Task<IActionResult> GetQueueStatus(Guid playerId, CancellationToken cancellationToken)
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

        return Ok(ToDto(ticket));
    }

    [HttpGet("{playerId:guid}/stream")]
    public async Task<IActionResult> StreamQueueStatus(Guid playerId, CancellationToken cancellationToken)
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

        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Append("X-Accel-Buffering", "no");

        await WriteSseEventAsync(ToDto(ticket), cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            if (ticket.Status != QueueStatus.Waiting)
            {
                break;
            }

            await Task.Delay(StreamPollIntervalMs, cancellationToken);

            var current = await repository.GetByPlayerIdAsync(playerId, cancellationToken);
            if (current is null)
            {
                break;
            }

            if (current.Status != ticket.Status || current.MatchedAt != ticket.MatchedAt)
            {
                await WriteSseEventAsync(ToDto(current), cancellationToken);
            }

            ticket = current;
        }

        return new EmptyResult();
    }

    [HttpDelete("{playerId:guid}")]
    public async Task<IActionResult> Dequeue(Guid playerId, CancellationToken cancellationToken)
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

    private static QueueTicketDto ToDto(QueueTicket ticket)
    {
        return new QueueTicketDto
        {
            PlayerId = ticket.PlayerId,
            Sr = ticket.Sr,
            Status = ticket.Status,
            QueuedAt = ticket.QueuedAt,
            MatchedAt = ticket.MatchedAt
        };
    }

    private async Task WriteSseEventAsync(QueueTicketDto payload, CancellationToken cancellationToken)
    {
        await Response.WriteAsync("event: queue-status\n", cancellationToken);
        await Response.WriteAsync($"data: {JsonSerializer.Serialize(payload)}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
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

        return errors;
    }
}