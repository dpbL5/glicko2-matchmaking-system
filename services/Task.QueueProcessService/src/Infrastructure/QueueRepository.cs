using System.Data;
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
        existing.Status = ticket.Status;
        existing.QueuedAt = ticket.QueuedAt;
        existing.MatchedAt = ticket.MatchedAt;

        await dbContext.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task<QueueTicket?> GetByPlayerIdAsync(Guid playerId, CancellationToken cancellationToken)
    {
        return await dbContext.QueueTickets
            .AsNoTracking()
            .FirstOrDefaultAsync(ticket => ticket.PlayerId == playerId, cancellationToken);
    }

    public async Task<bool> RemoveAsync(Guid playerId, CancellationToken cancellationToken)
    {
        var existing = await dbContext.QueueTickets
            .FirstOrDefaultAsync(ticket => ticket.PlayerId == playerId, cancellationToken);

        if (existing is null || existing.Status != QueueStatus.Waiting)
        {
            return false;
        }

        existing.Status = QueueStatus.Removed;
        existing.MatchedAt = null;

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<QueueTicket>> SearchAndMatchAsync(Guid playerId, int minPlayers, decimal maxSrDelta, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var waitingTickets = await dbContext.QueueTickets
            .Where(ticket => ticket.Status == QueueStatus.Waiting)
            .OrderBy(ticket => ticket.QueuedAt)
            .ToListAsync(cancellationToken);

        var source = waitingTickets.FirstOrDefault(ticket => ticket.PlayerId == playerId);
        if (source is null)
        {
            return [];
        }

        var candidates = waitingTickets
            .Where(ticket => ticket.PlayerId != playerId)
            .Select(ticket => new
            {
                Ticket = ticket,
                Delta = Math.Abs(ticket.Sr - source.Sr)
            })
            .Where(candidate => candidate.Delta <= maxSrDelta)
            .OrderBy(candidate => candidate.Delta)
            .ThenBy(candidate => candidate.Ticket.QueuedAt)
            .Take(Math.Max(0, minPlayers - 1))
            .Select(candidate => candidate.Ticket)
            .ToList();

        var selected = new List<QueueTicket> { source };
        selected.AddRange(candidates);

        if (selected.Count < minPlayers)
        {
            return selected;
        }

        var matchedAt = DateTimeOffset.UtcNow;
        foreach (var ticket in selected)
        {
            ticket.Status = QueueStatus.Matched;
            ticket.MatchedAt = matchedAt;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return selected;
    }
}