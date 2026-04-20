using MassTransit;
using MatchmakingProcessService.Application.Messaging;
using QueueProcessService.Application;

namespace QueueProcessService.Infrastructure.Messaging;

public sealed class MatchReadyEventConsumer : IConsumer<MatchReadyEvent>
{
    private readonly IMatchReadyTracker matchReadyTracker;

    public MatchReadyEventConsumer(IMatchReadyTracker matchReadyTracker)
    {
        this.matchReadyTracker = matchReadyTracker;
    }

    public Task Consume(ConsumeContext<MatchReadyEvent> context)
    {
        var message = context.Message;
        if (message.MatchId == Guid.Empty || message.PlayerIds.Count == 0)
        {
            return Task.CompletedTask;
        }

        matchReadyTracker.Store(message.MatchId, message.PlayerIds, message.OccurredAt);
        return Task.CompletedTask;
    }
}