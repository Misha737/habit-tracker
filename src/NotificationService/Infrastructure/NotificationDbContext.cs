using Microsoft.EntityFrameworkCore;
using NotificationService.Domain;

namespace NotificationService.Infrastructure;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }

    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        m.Entity<Notification>(e =>
        {
            e.ToTable("notifications");
            e.HasKey(n => n.EventId);
            e.Property(n => n.EventId).HasColumnName("event_id");
            e.Property(n => n.CorrelationId).HasColumnName("correlation_id").HasMaxLength(100);
            e.Property(n => n.CoreItemId).HasColumnName("core_item_id").IsRequired();
            e.Property(n => n.OwnerUserId).HasColumnName("owner_user_id").IsRequired();
            e.Property(n => n.Summary).HasColumnName("summary").HasMaxLength(500);
            e.Property(n => n.Payload).HasColumnName("payload").IsRequired();
            e.Property(n => n.CreatedAt).HasColumnName("created_at").IsRequired();
        });
    }
}
