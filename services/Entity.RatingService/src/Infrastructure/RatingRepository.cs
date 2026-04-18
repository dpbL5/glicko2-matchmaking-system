using Entity.RatingService.Application;
using Entity.RatingService.Domain;
using Microsoft.EntityFrameworkCore;

namespace Entity.RatingService.Infrastructure;

public sealed class RatingRepository : IRatingRepository
{
    private readonly RatingDbContext dbContext;

    public RatingRepository(RatingDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<PlayerRating?> GetByPlayerIdAsync(Guid playerId, CancellationToken cancellationToken)
    {
        return await dbContext.Ratings
            .AsNoTracking()
            .FirstOrDefaultAsync(rating => rating.PlayerId == playerId, cancellationToken);
    }

    public async Task<PlayerRating?> UpdateAsync(PlayerRating rating, CancellationToken cancellationToken)
    {
        var existing = await dbContext.Ratings
            .FirstOrDefaultAsync(entity => entity.PlayerId == rating.PlayerId, cancellationToken);

        if (existing is null)
        {
            return null;
        }

        existing.Rating = rating.Rating;
        existing.Rd = rating.Rd;
        existing.Volatility = rating.Volatility;

        await dbContext.SaveChangesAsync(cancellationToken);
        return existing;
    }
}
