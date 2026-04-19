namespace QueueProcessService.Api;

public sealed class QueueTicketDto
{
    public Guid PlayerId { get; init; }

    public decimal Sr { get; init; }

    public DateTimeOffset QueuedAt { get; init; }
}