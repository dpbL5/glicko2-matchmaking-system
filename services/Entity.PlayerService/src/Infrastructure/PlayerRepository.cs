namespace Entity.PlayerService.Infrastructure;

using Entity.PlayerService.Application;
using Entity.PlayerService.Domain;
using Microsoft.EntityFrameworkCore;

public sealed class PlayerRepository : IPlayerRepository
{
    private readonly PlayerDbContext dbContext;

    public PlayerRepository(PlayerDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Player>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Players
            .AsNoTracking()
            .OrderBy(player => player.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<Player?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Players
            .AsNoTracking()
            .FirstOrDefaultAsync(player => player.Id == id, cancellationToken);
    }
}