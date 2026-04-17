using Microsoft.EntityFrameworkCore;

namespace Entity.RatingService.Infrastructure;

public sealed class RatingDatabaseInitializer
{
    private readonly RatingDbContext dbContext;

    public RatingDatabaseInitializer(RatingDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
    }
}
