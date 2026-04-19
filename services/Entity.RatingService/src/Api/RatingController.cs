using Entity.RatingService.Application;
using Entity.RatingService.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Entity.RatingService.Api;

[ApiController]
[Route("ratings")]
public sealed class RatingController : ControllerBase
{
    private readonly IRatingRepository repository;

    public RatingController(IRatingRepository repository)
    {
        this.repository = repository;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PlayerRatingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
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
            var rating = await repository.GetByPlayerIdAsync(id, cancellationToken);
            if (rating is null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Rating not found.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return Ok(ToDto(rating));
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

    [HttpPost("{id:guid}")]
    [ProducesResponseType(typeof(PlayerRatingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> UpdateById(Guid id, [FromBody] RatingUpdateRequestDto request, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["id"] = new[] { "Player id is required." }
            }));
        }

        if (request is null)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["request"] = new[] { "Request body is required." }
            }));
        }

        if (request.Rating < 0 || request.Rd < 0 || request.Volatility < 0)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["request"] = new[] { "Rating values must be non-negative." }
            }));
        }

        try
        {
            var updated = await repository.UpdateAsync(new PlayerRating
            {
                PlayerId = id,
                Rating = request.Rating,
                Rd = request.Rd,
                Volatility = request.Volatility
            }, cancellationToken);

            if (updated is null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Rating not found.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return Ok(ToDto(updated));
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

    [HttpPost("{id:guid}/recalculate")]
    [ProducesResponseType(typeof(PlayerRatingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> RecalculateById(Guid id, [FromBody] RatingRecalculateRequestDto request, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["id"] = new[] { "Player id is required." }
            }));
        }

        if (request is null)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["request"] = new[] { "Request body is required." }
            }));
        }

        Glicko2MatchResult matchResult;
        try
        {
            matchResult = Glicko2Calculator.ParseMatchResult(request.MatchResult);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["matchResult"] = new[] { exception.Message }
            }));
        }

        if (request.OpponentRatings.Any(value => value < 0))
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["opponentRatings"] = new[] { "Opponent ratings must be non-negative." }
            }));
        }

        try
        {
            var current = await repository.GetByPlayerIdAsync(id, cancellationToken);
            if (current is null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Rating not found.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            var recalculated = Glicko2Calculator.Calculate(current, matchResult, request.OpponentRatings);
            var updated = await repository.UpdateAsync(recalculated, cancellationToken);

            if (updated is null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Rating not found.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return Ok(ToDto(updated));
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

    private static PlayerRatingDto ToDto(PlayerRating rating)
    {
        return new PlayerRatingDto
        {
            PlayerId = rating.PlayerId,
            Rating = rating.Rating,
            Rd = rating.Rd,
            Volatility = rating.Volatility
        };
    }
}
