using Microsoft.EntityFrameworkCore;
using Entity.PlayerService.Domain;

namespace Entity.PlayerService.Infrastructure;

public sealed class PlayerDbContext : DbContext
{
    public PlayerDbContext(DbContextOptions<PlayerDbContext> options)
        : base(options)
    {
    }

    public DbSet<Player> Players => Set<Player>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var player = modelBuilder.Entity<Player>();
        player.ToTable("players");
        player.HasKey(entity => entity.Id);

        player.Property(entity => entity.Id)
            .HasColumnName("id")
            .HasMaxLength(36)
            .HasConversion(
                id => id.ToString(),
                str => Guid.Parse(str))
            .IsRequired();

        player.Property(entity => entity.Name)
            .HasColumnName("name")
            .HasMaxLength(255)
            .IsRequired();
    }
}
