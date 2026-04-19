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
        logger.LogInformation(
            "Received RatingUpdated event for match {MatchId} at {OccurredAt}",
            context.Message.MatchId,
            context.Message.OccurredAt);

        return Task.CompletedTask;
    }
}
