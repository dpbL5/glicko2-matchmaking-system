using Microsoft.EntityFrameworkCore;
using QueueProcessService.Application;
using QueueProcessService.Domain;

namespace QueueProcessService.Infrastructure;

public sealed class QueueRepository : IQueueRepository
{
    private readonly QueueDbContext dbContext;

    public QueueRepository(QueueDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<QueueTicket> UpsertAsync(QueueTicket ticket, CancellationToken cancellationToken)
    {
        var existing = await dbContext.QueueTickets
            .FirstOrDefaultAsync(entity => entity.PlayerId == ticket.PlayerId, cancellationToken);

        if (existing is null)
        {
            dbContext.QueueTickets.Add(ticket);
            await dbContext.SaveChangesAsync(cancellationToken);
            return ticket;
        }

        existing.Sr = ticket.Sr;
        existing.QueuedAt = ticket.QueuedAt;

        await dbContext.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task<QueueTicket?> GetByPlayerIdAsync(Guid playerId, CancellationToken cancellationToken)
    {
        return await dbContext.QueueTickets
            .AsNoTracking()
            .FirstOrDefaultAsync(ticket => ticket.PlayerId == playerId, cancellationToken);
    }

    public async Task<IReadOnlyList<QueueTicket>> GetQueuedPlayersAsync(CancellationToken cancellationToken)
    {
        return await dbContext.QueueTickets
            .AsNoTracking()
            .OrderBy(ticket => ticket.QueuedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> MarkMatchedAsync(Guid playerId, CancellationToken cancellationToken)
    {
        var existing = await dbContext.QueueTickets
            .FirstOrDefaultAsync(ticket => ticket.PlayerId == playerId, cancellationToken);

        if (existing is null)
        {
            return false;
        }

        dbContext.QueueTickets.Remove(existing);

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RemoveAsync(Guid playerId, CancellationToken cancellationToken)
    {
        var existing = await dbContext.QueueTickets
            .FirstOrDefaultAsync(ticket => ticket.PlayerId == playerId, cancellationToken);

        if (existing is null)
        {
            return false;
        }

        dbContext.QueueTickets.Remove(existing);

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
