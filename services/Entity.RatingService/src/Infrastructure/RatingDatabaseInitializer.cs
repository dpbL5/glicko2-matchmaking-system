using Microsoft.EntityFrameworkCore;
using Entity.RatingService.Domain;

namespace Entity.RatingService.Infrastructure;

public sealed class RatingDatabaseInitializer
{
    private static readonly (Guid Id, string Name)[] SeedPlayers =
    [
        (Guid.Parse("00000000-0000-0000-0000-000000000001"), "Player 001"),
        (Guid.Parse("00000000-0000-0000-0000-000000000002"), "Player 002"),
        (Guid.Parse("00000000-0000-0000-0000-000000000003"), "Player 003"),
        (Guid.Parse("00000000-0000-0000-0000-000000000004"), "Player 004"),
        (Guid.Parse("00000000-0000-0000-0000-000000000005"), "Player 005"),
        (Guid.Parse("00000000-0000-0000-0000-000000000006"), "Player 006"),
        (Guid.Parse("00000000-0000-0000-0000-000000000007"), "Player 007"),
        (Guid.Parse("00000000-0000-0000-0000-000000000008"), "Player 008"),
        (Guid.Parse("00000000-0000-0000-0000-000000000009"), "Player 009"),
        (Guid.Parse("00000000-0000-0000-0000-00000000000a"), "Player 010"),
        (Guid.Parse("00000000-0000-0000-0000-00000000000b"), "Player 011"),
        (Guid.Parse("00000000-0000-0000-0000-00000000000c"), "Player 012"),
        (Guid.Parse("00000000-0000-0000-0000-00000000000d"), "Player 013"),
        (Guid.Parse("00000000-0000-0000-0000-00000000000e"), "Player 014"),
        (Guid.Parse("00000000-0000-0000-0000-00000000000f"), "Player 015"),
        (Guid.Parse("00000000-0000-0000-0000-000000000010"), "Player 016"),
        (Guid.Parse("00000000-0000-0000-0000-000000000011"), "Player 017"),
        (Guid.Parse("00000000-0000-0000-0000-000000000012"), "Player 018"),
        (Guid.Parse("00000000-0000-0000-0000-000000000013"), "Player 019"),
        (Guid.Parse("00000000-0000-0000-0000-000000000014"), "Player 020"),
        (Guid.Parse("00000000-0000-0000-0000-000000000015"), "Player 021"),
        (Guid.Parse("00000000-0000-0000-0000-000000000016"), "Player 022"),
        (Guid.Parse("00000000-0000-0000-0000-000000000017"), "Player 023"),
        (Guid.Parse("00000000-0000-0000-0000-000000000018"), "Player 024"),
        (Guid.Parse("00000000-0000-0000-0000-000000000019"), "Player 025"),
        (Guid.Parse("00000000-0000-0000-0000-00000000001a"), "Player 026"),
        (Guid.Parse("00000000-0000-0000-0000-00000000001b"), "Player 027"),
        (Guid.Parse("00000000-0000-0000-0000-00000000001c"), "Player 028"),
        (Guid.Parse("00000000-0000-0000-0000-00000000001d"), "Player 029"),
        (Guid.Parse("00000000-0000-0000-0000-00000000001e"), "Player 030"),
        (Guid.Parse("00000000-0000-0000-0000-00000000001f"), "Player 031"),
        (Guid.Parse("00000000-0000-0000-0000-000000000020"), "Player 032"),
        (Guid.Parse("00000000-0000-0000-0000-000000000021"), "Player 033")
    ];

    private readonly RatingDbContext dbContext;

    public RatingDatabaseInitializer(RatingDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        if (await dbContext.Ratings.AnyAsync(cancellationToken))
        {
            return;
        }

        var seedRatings = SeedPlayers.Select(player => new PlayerRating
        {
            PlayerId = player.Id,
            Rating = 1500m,
            Rd = 350m,
            Volatility = 0.06m
        });

        await dbContext.Ratings.AddRangeAsync(seedRatings, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
