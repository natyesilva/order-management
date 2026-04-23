using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(b =>
        {
            b.ToTable("orders");
            b.HasKey(x => x.Id);

            b.Property(x => x.Customer).HasMaxLength(200).IsRequired();
            b.Property(x => x.Product).HasMaxLength(200).IsRequired();
            b.Property(x => x.Value).HasColumnType("numeric(18,2)").IsRequired();

            b.Property(x => x.Status).IsRequired();
            b.Property(x => x.CreatedAt).IsRequired();
            b.Property(x => x.UpdatedAt);

            b.HasMany(x => x.StatusHistory)
                .WithOne(x => x.Order!)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderStatusHistory>(b =>
        {
            b.ToTable("order_status_history");
            b.HasKey(x => x.Id);

            b.Property(x => x.Source).HasMaxLength(50).IsRequired();
            b.Property(x => x.ChangedAt).IsRequired();

            b.HasIndex(x => new { x.OrderId, x.ChangedAt });
        });

        modelBuilder.Entity<ProcessedMessage>(b =>
        {
            b.ToTable("processed_messages");
            b.HasKey(x => x.Id);

            b.Property(x => x.MessageId).HasMaxLength(200).IsRequired();
            b.Property(x => x.CorrelationId).HasMaxLength(200).IsRequired();
            b.Property(x => x.EventType).HasMaxLength(100).IsRequired();
            b.Property(x => x.ProcessedAt).IsRequired();

            b.HasIndex(x => x.MessageId).IsUnique();
        });

        modelBuilder.Entity<OutboxMessage>(b =>
        {
            b.ToTable("outbox_messages");
            b.HasKey(x => x.Id);

            b.Property(x => x.MessageId).HasMaxLength(200).IsRequired();
            b.Property(x => x.CorrelationId).HasMaxLength(200).IsRequired();
            b.Property(x => x.EventType).HasMaxLength(100).IsRequired();
            b.Property(x => x.Payload).HasColumnType("jsonb").IsRequired();
            b.Property(x => x.CreatedAt).IsRequired();
            b.Property(x => x.ProcessedAt);

            b.HasIndex(x => x.MessageId).IsUnique();
            b.HasIndex(x => new { x.EventType, x.ProcessedAt, x.CreatedAt });
        });
    }
}
