using Entity.RatingService.Domain;
using Microsoft.EntityFrameworkCore;

namespace Entity.RatingService.Infrastructure;

public sealed class RatingDbContext : DbContext
{
    public RatingDbContext(DbContextOptions<RatingDbContext> options)
        : base(options)
    {
    }

    public DbSet<PlayerRating> Ratings => Set<PlayerRating>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var rating = modelBuilder.Entity<PlayerRating>();
        rating.ToTable("ratings");
        rating.HasKey(entity => entity.PlayerId);

        rating.Property(entity => entity.PlayerId)
            .HasColumnName("player_id")
            .HasMaxLength(36)
            .HasConversion(
                id => id.ToString(),
                str => Guid.Parse(str))
            .IsRequired();

        rating.Property(entity => entity.Rating)
            .HasColumnName("rating")
            .HasPrecision(10, 2)
            .IsRequired();

        rating.Property(entity => entity.Rd)
            .HasColumnName("rd")
            .HasPrecision(10, 2)
            .IsRequired();

        rating.Property(entity => entity.Volatility)
            .HasColumnName("volatility")
            .HasPrecision(10, 5)
            .IsRequired();
    }
}
