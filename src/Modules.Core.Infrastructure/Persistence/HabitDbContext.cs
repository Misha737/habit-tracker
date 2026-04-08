using Microsoft.EntityFrameworkCore;
using Modules.Core.Domain;

namespace Modules.Core.Infrastructure.Persistence;

public class HabitDbContext : DbContext
{
    public HabitDbContext(DbContextOptions<HabitDbContext> options) : base(options) { }

    public DbSet<Habit> Habits => Set<Habit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Habit>(entity =>
        {
            entity.ToTable("habits");
            entity.HasKey(h => h.Id);
            entity.Property(h => h.Id).HasColumnName("id");
            entity.Property(h => h.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.Property(h => h.Description).HasColumnName("description").HasMaxLength(1000);
            entity.Property(h => h.FrequencyPerWeek).HasColumnName("frequency_per_week").IsRequired();
            entity.Property(h => h.Status)
                  .HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(h => h.OwnerUserId)
                  .HasColumnName("owner_user_id").IsRequired();
            entity.Property(h => h.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(h => h.UpdatedAt).HasColumnName("updated_at").IsRequired();
            entity.HasIndex(h => new { h.OwnerUserId, h.Name })
                  .HasDatabaseName("ix_habits_owner_name");
        });
    }
}
