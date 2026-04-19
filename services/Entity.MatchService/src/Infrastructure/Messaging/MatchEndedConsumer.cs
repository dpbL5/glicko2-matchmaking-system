using Entity.MatchService.Application;
using Entity.MatchService.Domain;
using MassTransit;
using MatchmakingProcessService.Application.Messaging;

namespace Entity.MatchService.Infrastructure.Messaging;

/// <summary>
/// Consumes MatchEnded events from the message broker.
/// Per design §2.8: Broker → MatchService: MatchEnded → Update match result → Publish MatchUpdated.
/// </summary>
public sealed class MatchEndedConsumer : IConsumer<MatchEndedEvent>
{
    private readonly IMatchRepository repository;
    private readonly IPublishEndpoint publishEndpoint;
    private readonly ILogger<MatchEndedConsumer> logger;

    public MatchEndedConsumer(
        IMatchRepository repository,
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
            "Received MatchEnded event for match {MatchId}. Winner: {Winner}. OccurredAt: {OccurredAt}",
            message.MatchId,
            message.Winner,
            message.OccurredAt);

        var updated = await repository.UpdateResultAsync(
            message.MatchId,
            message.Winner,
            message.Result,
            context.CancellationToken);

        if (updated is null)
        {
            logger.LogWarning("Match {MatchId} not found when processing MatchEnded event.", message.MatchId);
            return;
        }

        logger.LogInformation("Match {MatchId} updated to status {Status}.", updated.Id, updated.Status);

        await publishEndpoint.Publish(new MatchUpdatedEvent
        {
            MatchId = updated.Id,
            Status = updated.Status,
            Winner = message.Winner,
            Result = message.Result,
            OccurredAt = DateTimeOffset.UtcNow
        }, context.CancellationToken);

        logger.LogInformation("Published MatchUpdated event for match {MatchId}.", updated.Id);
    }
}
