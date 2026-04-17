using Entity.RatingService.Application;
using Entity.RatingService.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Entity.RatingService.Api;

[ApiController]
[Route("glicko2")]
public sealed class Glicko2Controller : ControllerBase
{
    private readonly IRatingRepository repository;

    public Glicko2Controller(IRatingRepository repository)
    {
        this.repository = repository;
    }

    [HttpPost]
    public async Task<IActionResult> Calculate([FromBody] Glicko2RequestDto request, CancellationToken cancellationToken)
    {
        if (request.PlayerId == Guid.Empty)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["playerId"] = new[] { "Player id is required." }
            }));
        }

        if (!TryMapScore(request.MatchResult, out var score))
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["matchResult"] = new[] { "Match result must be win, loss, or draw." }
            }));
        }

        var current = await repository.GetByPlayerIdAsync(request.PlayerId, cancellationToken)
            ?? new PlayerRating
            {
                PlayerId = request.PlayerId,
                Rating = 1500m,
                Rd = 350m,
                Volatility = 0.06m
            };

        var averageOpponentRating = request.OpponentRatings.Count == 0
            ? current.Rating
            : request.OpponentRatings.Average();

        var expected = (decimal)(1.0 / (1.0 + Math.Pow(10.0, (double)((averageOpponentRating - current.Rating) / 400m))));
        var nextRating = decimal.Round(current.Rating + 32m * (score - expected), 2, MidpointRounding.AwayFromZero);
        var nextRd = decimal.Round(Math.Max(30m, current.Rd * 0.95m), 2, MidpointRounding.AwayFromZero);
        var nextVolatility = decimal.Round(current.Volatility, 5, MidpointRounding.AwayFromZero);

        var updated = await repository.UpsertAsync(new PlayerRating
        {
            PlayerId = request.PlayerId,
            Rating = nextRating,
            Rd = nextRd,
            Volatility = nextVolatility
        }, cancellationToken);

        return Ok(new Glicko2ResponseDto
        {
            PlayerId = updated.PlayerId,
            Rating = updated.Rating,
            Rd = updated.Rd,
            Volatility = updated.Volatility
        });
    }

    private static bool TryMapScore(string matchResult, out decimal score)
    {
        switch (matchResult.Trim().ToLowerInvariant())
        {
            case "win":
                score = 1m;
                return true;
            case "draw":
                score = 0.5m;
                return true;
            case "loss":
                score = 0m;
                return true;
            default:
                score = 0m;
                return false;
        }
    }
}
