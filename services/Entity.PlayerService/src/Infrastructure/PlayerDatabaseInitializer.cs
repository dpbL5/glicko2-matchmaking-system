using Microsoft.EntityFrameworkCore;
using Entity.PlayerService.Domain;

namespace Entity.PlayerService.Infrastructure;

public sealed class PlayerDatabaseInitializer
{
    private readonly PlayerDbContext dbContext;

    public PlayerDatabaseInitializer(PlayerDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        if (await dbContext.Players.AnyAsync(cancellationToken))
        {
            return;
        }

        var seedPlayers = new[]
        {
            new Player { Id = Guid.NewGuid(), Name = "Player 001" },
            new Player { Id = Guid.NewGuid(), Name = "Player 002" },
            new Player { Id = Guid.NewGuid(), Name = "Player 003" },
            new Player { Id = Guid.NewGuid(), Name = "Player 004" },
            new Player { Id = Guid.NewGuid(), Name = "Player 005" },
            new Player { Id = Guid.NewGuid(), Name = "Player 006" },
            new Player { Id = Guid.NewGuid(), Name = "Player 007" },
            new Player { Id = Guid.NewGuid(), Name = "Player 008" },
            new Player { Id = Guid.NewGuid(), Name = "Player 009" },
            new Player { Id = Guid.NewGuid(), Name = "Player 010" },
            new Player { Id = Guid.NewGuid(), Name = "Player 011" },
            new Player { Id = Guid.NewGuid(), Name = "Player 012" },
            new Player { Id = Guid.NewGuid(), Name = "Player 013" },
            new Player { Id = Guid.NewGuid(), Name = "Player 014" },
            new Player { Id = Guid.NewGuid(), Name = "Player 015" },
            new Player { Id = Guid.NewGuid(), Name = "Player 016" },
            new Player { Id = Guid.NewGuid(), Name = "Player 017" },
            new Player { Id = Guid.NewGuid(), Name = "Player 018" },
            new Player { Id = Guid.NewGuid(), Name = "Player 019" },
            new Player { Id = Guid.NewGuid(), Name = "Player 020" },
            new Player { Id = Guid.NewGuid(), Name = "Player 021" },
            new Player { Id = Guid.NewGuid(), Name = "Player 022" },
            new Player { Id = Guid.NewGuid(), Name = "Player 023" },
            new Player { Id = Guid.NewGuid(), Name = "Player 024" },
            new Player { Id = Guid.NewGuid(), Name = "Player 025" },
            new Player { Id = Guid.NewGuid(), Name = "Player 026" },
            new Player { Id = Guid.NewGuid(), Name = "Player 027" },
            new Player { Id = Guid.NewGuid(), Name = "Player 028" },
            new Player { Id = Guid.NewGuid(), Name = "Player 029" },
            new Player { Id = Guid.NewGuid(), Name = "Player 030" },
            new Player { Id = Guid.NewGuid(), Name = "Player 031" },
            new Player { Id = Guid.NewGuid(), Name = "Player 032" },
            new Player { Id = Guid.NewGuid(), Name = "Player 033" }
        };

        await dbContext.Players.AddRangeAsync(seedPlayers, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}