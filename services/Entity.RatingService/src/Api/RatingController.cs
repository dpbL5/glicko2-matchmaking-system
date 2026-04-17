using Entity.RatingService.Application;
using Entity.RatingService.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Entity.RatingService.Api;

[ApiController]
[Route("rating")]
public sealed class RatingController : ControllerBase
{
    private readonly IRatingRepository repository;

    public RatingController(IRatingRepository repository)
    {
        this.repository = repository;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["id"] = new[] { "Player id is required." }
            }));
        }

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

    [HttpPost("{id:guid}")]
    public async Task<IActionResult> UpdateById(Guid id, [FromBody] RatingUpdateRequestDto request, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["id"] = new[] { "Player id is required." }
            }));
        }

        if (request.Rating < 0 || request.Rd < 0 || request.Volatility < 0)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["request"] = new[] { "Rating values must be non-negative." }
            }));
        }

        var updated = await repository.UpsertAsync(new PlayerRating
        {
            PlayerId = id,
            Rating = request.Rating,
            Rd = request.Rd,
            Volatility = request.Volatility
        }, cancellationToken);

        return Ok(ToDto(updated));
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
