using MassTransit;
using MatchmakingProcessService.Application.Messaging;

namespace MatchmakingProcessService.Infrastructure.Messaging;

public sealed class RatingUpdatedConsumer : IConsumer<RatingUpdatedEvent>
{
    private readonly ILogger<RatingUpdatedConsumer> logger;

    public RatingUpdatedConsumer(ILogger<RatingUpdatedConsumer> logger)
    {
        this.logger = logger;
    }

    public Task Consume(ConsumeContext<RatingUpdatedEvent> context)
    {
        var message = context.Message;

        logger.LogInformation(
            "Received RatingUpdated event for match {MatchId} at {OccurredAt}. {Count} ratings updated.",
            message.MatchId,
            message.OccurredAt,
            message.UpdatedRatings.Count);

        foreach (var update in message.UpdatedRatings)
        {
            logger.LogInformation(
                "  Player {PlayerId}: {OldRating} → {NewRating} (RD: {NewRd}, Vol: {NewVol})",
                update.PlayerId,
                update.OldRating,
                update.NewRating,
                update.NewRd,
                update.NewVolatility);
        }

        return Task.CompletedTask;
    }
}
