using Entity.MatchService.Domain;
using Microsoft.EntityFrameworkCore;

namespace Entity.MatchService.Infrastructure;

public sealed class MatchDbContext : DbContext
{
    public MatchDbContext(DbContextOptions<MatchDbContext> options)
        : base(options)
    {
    }

    public DbSet<Match> Matches => Set<Match>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var match = modelBuilder.Entity<Match>();
        match.ToTable("matches");
        match.HasKey(entity => entity.Id);

        match.Property(entity => entity.Id)
            .HasColumnName("id")
            .HasMaxLength(36)
            .HasConversion(
                id => id.ToString(),
                str => Guid.Parse(str))
            .IsRequired();

        match.Property(entity => entity.Status)
            .HasColumnName("status")
            .HasMaxLength(32)
            .IsRequired();

        match.Property(entity => entity.PlayerIdsJson)
            .HasColumnName("player_ids_json")
            .HasColumnType("json")
            .IsRequired();

        match.Property(entity => entity.QueueId)
            .HasColumnName("queue_id")
            .HasMaxLength(64);

        match.Property(entity => entity.Winner)
            .HasColumnName("winner")
            .HasMaxLength(255);

        match.Property(entity => entity.Result)
            .HasColumnName("result")
            .HasMaxLength(255);
    }
}