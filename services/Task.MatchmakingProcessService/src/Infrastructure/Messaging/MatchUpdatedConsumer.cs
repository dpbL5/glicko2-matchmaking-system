using MassTransit;
using MatchmakingProcessService.Application.Messaging;

namespace MatchmakingProcessService.Infrastructure.Messaging;

public sealed class MatchUpdatedConsumer : IConsumer<MatchUpdatedEvent>
{
    private readonly ILogger<MatchUpdatedConsumer> logger;

    public MatchUpdatedConsumer(ILogger<MatchUpdatedConsumer> logger)
    {
        this.logger = logger;
    }

    public Task Consume(ConsumeContext<MatchUpdatedEvent> context)
    {
        logger.LogInformation(
            "Received MatchUpdated event for match {MatchId}. Status: {Status}. OccurredAt: {OccurredAt}",
            context.Message.MatchId,
            context.Message.Status,
            context.Message.OccurredAt);

        return Task.CompletedTask;
    }
}
