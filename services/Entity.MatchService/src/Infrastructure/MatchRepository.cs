using Entity.MatchService.Application;
using Entity.MatchService.Domain;
using Microsoft.EntityFrameworkCore;

namespace Entity.MatchService.Infrastructure;

public sealed class MatchRepository : IMatchRepository
{
    private readonly MatchDbContext dbContext;

    public MatchRepository(MatchDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<Match> CreateAsync(Match match, CancellationToken cancellationToken)
    {
        dbContext.Matches.Add(match);
        await dbContext.SaveChangesAsync(cancellationToken);
        return match;
    }

    public async Task<Match?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Matches
            .AsNoTracking()
            .FirstOrDefaultAsync(match => match.Id == id, cancellationToken);
    }

    public async Task<Match?> UpdateResultAsync(Guid id, string winner, string result, CancellationToken cancellationToken)
    {
        var existing = await dbContext.Matches
            .FirstOrDefaultAsync(match => match.Id == id, cancellationToken);

        if (existing is null)
        {
            return null;
        }

        existing.Status = MatchStatus.Finished;
        existing.Winner = winner;
        existing.Result = result;

        await dbContext.SaveChangesAsync(cancellationToken);
        return existing;
    }
}