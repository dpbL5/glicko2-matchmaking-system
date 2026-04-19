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
        existing.Status = QueueStatus.Waiting; // Reset to waiting on re-enqueue
        existing.QueuedAt = ticket.QueuedAt;
        existing.MatchedAt = null;

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
            .Where(ticket => ticket.Status == QueueStatus.Waiting)
            .OrderBy(ticket => ticket.QueuedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> RemoveAsync(Guid playerId, CancellationToken cancellationToken)
    {
        var existing = await dbContext.QueueTickets
            .FirstOrDefaultAsync(ticket => ticket.PlayerId == playerId, cancellationToken);

        if (existing is null || (existing.Status != QueueStatus.Waiting && existing.Status != QueueStatus.Matched))
        {
            return false;
        }

        existing.Status = QueueStatus.Removed;
        existing.MatchedAt = null;

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
