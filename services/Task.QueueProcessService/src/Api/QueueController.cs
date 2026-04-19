using System.Text.Json;
using System.Collections.Concurrent;
using QueueProcessService.Application;
using QueueProcessService.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace QueueProcessService.Api;

[ApiController]
[Route("queue")]
public sealed class QueueController : ControllerBase
{
    private const int StreamPollIntervalMs = 1000;
    private const decimal MaxSrDelta = 100m;
    private static readonly ConcurrentDictionary<Guid, Guid> PlayerMatchMap = new();

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

            PlayerMatchMap.TryRemove(request.PlayerId, out _);

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

    [HttpGet("players")]
    public async Task<IActionResult> GetQueuedPlayers(CancellationToken cancellationToken)
    {
        try
        {
            var tickets = await repository.GetQueuedPlayersAsync(cancellationToken);
            var response = tickets.Select(ToDto).ToList();

            return Ok(response);
        }
        catch (DbUpdateException exception)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
            {
                Title = "Database is unavailable.",
                Status = StatusCodes.Status503ServiceUnavailable,
                Detail = exception.Message
            });
        }
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

        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Append("X-Accel-Buffering", "no");

        await Response.WriteAsync("event: stream-open\n", cancellationToken);
        await Response.WriteAsync($"data: {{\"playerId\":\"{playerId}\",\"connected\":true}}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);

        QueueTicket? lastKnownTicket = null;

        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(StreamPollIntervalMs, cancellationToken);

            var current = await repository.GetByPlayerIdAsync(playerId, cancellationToken);
            if (current is null)
            {
                continue;
            }

            if (lastKnownTicket is null
                || current.Status != lastKnownTicket.Status
                || current.MatchedAt != lastKnownTicket.MatchedAt
                || current.QueuedAt != lastKnownTicket.QueuedAt
                || current.Sr != lastKnownTicket.Sr)
            {
                await WriteSseEventAsync(ToDto(current), cancellationToken);
                lastKnownTicket = current;
            }

            if (current.Status == QueueStatus.Waiting)
            {
                var opponent = (await repository.GetQueuedPlayersAsync(cancellationToken))
                    .Where(ticket => ticket.PlayerId != playerId)
                    .Where(ticket => Math.Abs(ticket.Sr - current.Sr) <= MaxSrDelta)
                    .OrderBy(ticket => ticket.QueuedAt)
                    .FirstOrDefault();

                if (opponent is not null)
                {
                    var initialized = await upstreamClient.InitializeMatchmakingAsync(
                        new Dictionary<Guid, decimal>
                        {
                            [playerId] = current.Sr,
                            [opponent.PlayerId] = opponent.Sr
                        },
                        cancellationToken);

                    if (initialized)
                    {
                        continue;
                    }
                }
            }

            if (current.Status != QueueStatus.Waiting)
            {
                break;
            }
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
        PlayerMatchMap.TryGetValue(ticket.PlayerId, out var matchId);

        return new QueueTicketDto
        {
            PlayerId = ticket.PlayerId,
            MatchId = matchId == Guid.Empty ? null : matchId,
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