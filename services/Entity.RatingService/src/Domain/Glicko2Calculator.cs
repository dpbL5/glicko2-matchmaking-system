namespace Entity.RatingService.Domain;

public static class Glicko2Calculator
{
    private const decimal RatingStep = 32m;
    private const decimal MinimumRd = 30m;

    public static PlayerRating Calculate(PlayerRating current, Glicko2MatchResult matchResult, IReadOnlyList<decimal> opponentRatings)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(opponentRatings);

        var score = MapScore(matchResult);
        var averageOpponentRating = opponentRatings.Count == 0
            ? current.Rating
            : opponentRatings.Average();

        var expected = 1m / (1m + (decimal)Math.Pow(10d, (double)((averageOpponentRating - current.Rating) / 400m)));
        var nextRating = decimal.Round(current.Rating + RatingStep * (score - expected), 2, MidpointRounding.AwayFromZero);
        var nextRd = decimal.Round(Math.Max(MinimumRd, current.Rd * 0.95m), 2, MidpointRounding.AwayFromZero);
        var nextVolatility = decimal.Round(current.Volatility, 5, MidpointRounding.AwayFromZero);

        return new PlayerRating
        {
            PlayerId = current.PlayerId,
            Rating = nextRating,
            Rd = nextRd,
            Volatility = nextVolatility
        };
    }

    public static Glicko2MatchResult ParseMatchResult(string matchResult)
    {
        if (string.IsNullOrWhiteSpace(matchResult))
        {
            throw new ArgumentException("Match result is required.", nameof(matchResult));
        }

        return matchResult.Trim().ToLowerInvariant() switch
        {
            "win" => Glicko2MatchResult.Win,
            "draw" => Glicko2MatchResult.Draw,
            "loss" => Glicko2MatchResult.Loss,
            _ => throw new ArgumentException("Match result must be win, draw, or loss.", nameof(matchResult))
        };
    }

    private static decimal MapScore(Glicko2MatchResult matchResult)
    {
        return matchResult switch
        {
            Glicko2MatchResult.Win => 1m,
            Glicko2MatchResult.Draw => 0.5m,
            Glicko2MatchResult.Loss => 0m,
            _ => throw new ArgumentOutOfRangeException(nameof(matchResult), matchResult, "Unsupported match result.")
        };
    }
}
