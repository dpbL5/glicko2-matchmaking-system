using Microsoft.EntityFrameworkCore;
using QueueProcessService.Application;
using QueueProcessService.Domain;

namespace QueueProcessService.Infrastructure;

public sealed class BackgroundQueueRepository : IQueueRepository
{
    private readonly QueueDbContext dbContext;

    public BackgroundQueueRepository(QueueDbContext dbContext)
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

    public async Task<IReadOnlyList<IReadOnlyList<Guid>>> FindMatchCandidateGroupsAsync(int minPlayers, decimal maxSrDelta, CancellationToken cancellationToken)
    {
        var waitingTickets = await dbContext.QueueTickets
            .AsNoTracking()
            .Where(ticket => ticket.Status == QueueStatus.Waiting)
            .OrderBy(ticket => ticket.QueuedAt)
            .ToListAsync(cancellationToken);

        if (waitingTickets.Count < minPlayers)
        {
            return [];
        }

        var groups = new List<IReadOnlyList<Guid>>();
        var processedPlayerIds = new HashSet<Guid>();

        foreach (var source in waitingTickets)
        {
            if (processedPlayerIds.Contains(source.PlayerId)) continue;

            var candidates = waitingTickets
                .Where(t => t.PlayerId != source.PlayerId && !processedPlayerIds.Contains(t.PlayerId))
                .Select(t => new
                {
                    Ticket = t,
                    Delta = Math.Abs(t.Sr - source.Sr)
                })
                .Where(c => c.Delta <= maxSrDelta)
                .OrderBy(c => c.Delta)
                .ThenBy(c => c.Ticket.QueuedAt)
                .Take(minPlayers - 1)
                .ToList();

            if (candidates.Count == minPlayers - 1)
            {
                var matchGroup = new List<QueueTicket> { source };
                matchGroup.AddRange(candidates.Select(c => c.Ticket));

                groups.Add(matchGroup.Select(ticket => ticket.PlayerId).ToList());

                foreach (var ticket in matchGroup)
                {
                    processedPlayerIds.Add(ticket.PlayerId);
                }
            }
        }

        return groups;
    }
}
