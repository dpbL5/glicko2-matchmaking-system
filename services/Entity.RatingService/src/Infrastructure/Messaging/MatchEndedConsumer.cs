using Entity.RatingService.Application;
using Entity.RatingService.Domain;
using MassTransit;
using MatchmakingProcessService.Application.Messaging;

namespace Entity.RatingService.Infrastructure.Messaging;

/// <summary>
/// Consumes MatchEnded events from the message broker.
/// Per design §2.8: Broker → RatingService: MatchEnded → Recalculate rating → Publish RatingUpdated.
/// </summary>
public sealed class MatchEndedConsumer : IConsumer<MatchEndedEvent>
{
    private readonly IRatingRepository repository;
    private readonly IPublishEndpoint publishEndpoint;
    private readonly ILogger<MatchEndedConsumer> logger;

    public MatchEndedConsumer(
        IRatingRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<MatchEndedConsumer> logger)
    {
        this.repository = repository;
        this.publishEndpoint = publishEndpoint;
        this.logger = logger;
    }

    public async Task Consume(ConsumeContext<MatchEndedEvent> context)
    {
        var message = context.Message;

        logger.LogInformation(
            "Received MatchEnded event for match {MatchId}. Processing {Count} player outcomes.",
            message.MatchId,
            message.PlayerOutcomes.Count);

        // Pre-fetch all player ratings so we can derive opponent ratings
        var playerRatings = new Dictionary<Guid, PlayerRating>();
        foreach (var outcome in message.PlayerOutcomes)
        {
            var rating = await repository.GetByPlayerIdAsync(outcome.PlayerId, context.CancellationToken);
            if (rating is not null)
            {
                playerRatings[outcome.PlayerId] = rating;
            }
            else
            {
                logger.LogWarning(
                    "Rating not found for player {PlayerId} in match {MatchId}. Skipping.",
                    outcome.PlayerId,
                    message.MatchId);
            }
        }

        var updatedRatings = new List<PlayerRatingUpdate>();

        foreach (var outcome in message.PlayerOutcomes)
        {
            if (!playerRatings.ContainsKey(outcome.PlayerId))
            {
                continue;
            }

            // Use provided opponent ratings, or derive from fetched player ratings
            var opponentRatings = outcome.OpponentRatings.Count > 0
                ? outcome.OpponentRatings
                : playerRatings
                    .Where(kv => kv.Key != outcome.PlayerId)
                    .Select(kv => kv.Value.Rating)
                    .ToList();

            var ratingUpdate = await RecalculatePlayerRatingAsync(
                message.MatchId, outcome, opponentRatings, playerRatings[outcome.PlayerId], context.CancellationToken);

            if (ratingUpdate is not null)
            {
                updatedRatings.Add(ratingUpdate);
            }
        }

        await publishEndpoint.Publish(new RatingUpdatedEvent
        {
            MatchId = message.MatchId,
            UpdatedRatings = updatedRatings,
            OccurredAt = DateTimeOffset.UtcNow
        }, context.CancellationToken);

        logger.LogInformation("Published RatingUpdated event for match {MatchId} with {Count} rating updates.", message.MatchId, updatedRatings.Count);
    }

    private async Task<PlayerRatingUpdate?> RecalculatePlayerRatingAsync(
        Guid matchId,
        MatchPlayerOutcome outcome,
        IReadOnlyList<decimal> opponentRatings,
        PlayerRating current,
        CancellationToken cancellationToken)
    {
        Glicko2MatchResult matchResult;
        try
        {
            matchResult = Glicko2Calculator.ParseMatchResult(outcome.MatchResult);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(
                "Invalid match result '{MatchResult}' for player {PlayerId} in match {MatchId}: {Error}",
                outcome.MatchResult,
                outcome.PlayerId,
                matchId,
                ex.Message);
            return null;
        }

        var recalculated = Glicko2Calculator.Calculate(current, matchResult, opponentRatings);
        var updated = await repository.UpdateAsync(recalculated, cancellationToken);

        if (updated is not null)
        {
            logger.LogInformation(
                "Rating updated for player {PlayerId}: {OldRating} → {NewRating}",
                outcome.PlayerId,
                current.Rating,
                updated.Rating);

            return new PlayerRatingUpdate
            {
                PlayerId = outcome.PlayerId,
                OldRating = current.Rating,
                NewRating = updated.Rating,
                NewRd = updated.Rd,
                NewVolatility = updated.Volatility
            };
        }

        return null;
    }
}
