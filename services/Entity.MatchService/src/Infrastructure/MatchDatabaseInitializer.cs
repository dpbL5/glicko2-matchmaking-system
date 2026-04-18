using Microsoft.EntityFrameworkCore;

namespace Entity.MatchService.Infrastructure;

public sealed class MatchDatabaseInitializer
{
    private readonly MatchDbContext dbContext;

    public MatchDatabaseInitializer(MatchDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
    }
}