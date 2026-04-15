using Microsoft.EntityFrameworkCore;
using WorkflowService.Domain;

namespace WorkflowService.Infrastructure;

public class WorkflowDbContext : DbContext
{
    public WorkflowDbContext(DbContextOptions<WorkflowDbContext> options) : base(options) { }

    public DbSet<WorkflowInstance> WorkflowInstances => Set<WorkflowInstance>();
    public DbSet<HabitJoining> HabitJoinings => Set<HabitJoining>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkflowInstance>(entity =>
        {
            entity.ToTable("workflow_instances");
            entity.HasKey(w => w.WorkflowId);
            entity.Property(w => w.WorkflowId).HasColumnName("workflow_id");
            entity.Property(w => w.Type)
                  .HasColumnName("type")
                  .HasConversion<string>()
                  .HasMaxLength(50);
            entity.Property(w => w.State)
                  .HasColumnName("state")
                  .HasConversion<string>()
                  .HasMaxLength(50);
            entity.Property(w => w.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(w => w.UpdatedAt).HasColumnName("updated_at").IsRequired();
            entity.Property(w => w.LastError).HasColumnName("last_error");
            entity.Property(w => w.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(w => w.HabitId).HasColumnName("habit_id").IsRequired();
            entity.Property(w => w.JoiningId).HasColumnName("joining_id");
        });

        modelBuilder.Entity<HabitJoining>(entity =>
        {
            entity.ToTable("habit_joinings");
            entity.HasKey(h => h.Id);
            entity.Property(h => h.Id).HasColumnName("id");
            entity.Property(h => h.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(h => h.HabitId).HasColumnName("habit_id").IsRequired();
            entity.Property(h => h.Status)
                  .HasColumnName("status")
                  .HasConversion<string>()
                  .HasMaxLength(50);
            entity.Property(h => h.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(h => h.CancelledAt).HasColumnName("cancelled_at");
        });
    }
}
