using Microsoft.EntityFrameworkCore;

namespace QueueProcessService.Infrastructure;

public sealed class QueueDatabaseInitializer
{
    private readonly QueueDbContext dbContext;

    public QueueDatabaseInitializer(QueueDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async global::System.Threading.Tasks.Task InitializeAsync(CancellationToken cancellationToken)
    {
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
    }
}