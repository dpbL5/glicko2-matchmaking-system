namespace QueueProcessService.Api;

public sealed class QueueTicketDto
{
    public Guid PlayerId { get; init; }

    public decimal Sr { get; init; }

    public string Status { get; init; } = string.Empty;

    public DateTimeOffset QueuedAt { get; init; }
}