using Microsoft.EntityFrameworkCore;
using QueueProcessService.Domain;

namespace QueueProcessService.Infrastructure;

public sealed class QueueDbContext : DbContext
{
    public QueueDbContext(DbContextOptions<QueueDbContext> options)
        : base(options)
    {
    }

    public DbSet<QueueTicket> QueueTickets => Set<QueueTicket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var ticket = modelBuilder.Entity<QueueTicket>();
        ticket.ToTable("queue_tickets");
        ticket.HasKey(entity => entity.PlayerId);

        ticket.Property(entity => entity.PlayerId)
            .HasColumnName("player_id")
            .HasMaxLength(36)
            .HasConversion(
                id => id.ToString(),
                str => Guid.Parse(str))
            .IsRequired();

        ticket.Property(entity => entity.Sr)
            .HasColumnName("sr")
            .HasPrecision(10, 2)
            .IsRequired();

        ticket.Property(entity => entity.QueuedAt)
            .HasColumnName("queued_at")
            .HasColumnType("datetime(6)")
            .IsRequired();
    }
}