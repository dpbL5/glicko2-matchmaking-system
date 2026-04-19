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
        var message = context.Message;

        logger.LogInformation(
            "Received MatchUpdated event for match {MatchId}. Status: {Status}, Winner: {Winner}, Result: {Result}. OccurredAt: {OccurredAt}",
            message.MatchId,
            message.Status,
            message.Winner,
            message.Result,
            message.OccurredAt);

        return Task.CompletedTask;
    }
}
